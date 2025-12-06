using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobs.Worker.Worker;

public class SchedulerWorker : BackgroundService
{
    private readonly ILogger<SchedulerWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _hostInstance;

    public SchedulerWorker(
        ILogger<SchedulerWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostInstance = Environment.MachineName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Scheduler Worker starting at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSchedulesAsync(stoppingToken);
                await ProcessQueuedExecutionsAsync(stoppingToken);
                await ProcessRetryExecutionsAsync(stoppingToken);

                // Check every 10 seconds
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduler worker main loop");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Job Scheduler Worker stopping at: {time}", DateTimeOffset.Now);
    }

    private async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<IJobScheduleRepository>();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
        var scheduleCalculator = scope.ServiceProvider.GetRequiredService<IScheduleCalculator>();

        var dueSchedules = await scheduleRepo.GetDueSchedulesAsync(DateTime.UtcNow, cancellationToken);

        foreach (var schedule in dueSchedules)
        {
            var lockKey = $"schedule:{schedule.Id}";

            if (await lockService.TryAcquireLockAsync(lockKey, _hostInstance, TimeSpan.FromMinutes(5), cancellationToken))
            {
                try
                {
                    _logger.LogInformation("Processing due schedule {ScheduleId} for job {JobName}",
                        schedule.Id, schedule.JobDefinition.Name);

                    // Check concurrency
                    var activeCount = await executionRepo.GetActiveExecutionCountForJobAsync(
                        schedule.JobDefinitionId, cancellationToken);

                    if (activeCount >= schedule.JobDefinition.MaxConcurrentExecutions)
                    {
                        _logger.LogWarning("Job {JobName} has reached max concurrency ({Max}). Skipping execution.",
                            schedule.JobDefinition.Name, schedule.JobDefinition.MaxConcurrentExecutions);
                        continue;
                    }

                    // Create execution
                    var context = Jobs.Worker.Domain.ValueObjects.ExecutionContext.Create(_hostInstance);
                    var execution = new JobExecution(
                        schedule.JobDefinitionId,
                        schedule.Id,
                        context,
                        null,
                        "Scheduler",
                        false,
                        schedule.JobDefinition.RetryPolicy.MaxRetries
                    );

                    await executionRepo.AddAsync(execution, cancellationToken);

                    // Update schedule
                    schedule.RecordExecution(DateTime.UtcNow);
                    var nextExecution = scheduleCalculator.CalculateNextExecution(schedule, DateTime.UtcNow);
                    if (nextExecution.HasValue)
                    {
                        schedule.SetNextExecution(nextExecution.Value);
                    }
                    await scheduleRepo.UpdateAsync(schedule, cancellationToken);

                    _logger.LogInformation("Created execution {ExecutionId} for job {JobName}",
                        execution.Id, schedule.JobDefinition.Name);
                }
                finally
                {
                    await lockService.ReleaseLockAsync(lockKey, _hostInstance, cancellationToken);
                }
            }
        }
    }

    private async Task ProcessQueuedExecutionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();
        var lockService = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();

        var queuedExecutions = await executionRepo.GetExecutionsByStatusAsync(
            Domain.Enums.ExecutionStatus.Queued, cancellationToken);

        foreach (var execution in queuedExecutions.Take(10)) // Process up to 10 at a time
        {
            var lockKey = $"execution:{execution.Id}";

            if (await lockService.TryAcquireLockAsync(lockKey, _hostInstance, TimeSpan.FromMinutes(60), cancellationToken))
            {
                // Start execution in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteJobAsync(execution.Id, cancellationToken);
                    }
                    finally
                    {
                        await lockService.ReleaseLockAsync(lockKey, _hostInstance, default);
                    }
                }, cancellationToken);
            }
        }
    }

    private async Task ProcessRetryExecutionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();

        var retryingExecutions = await executionRepo.GetExecutionsByStatusAsync(
            Domain.Enums.ExecutionStatus.Retrying, cancellationToken);

        foreach (var execution in retryingExecutions.Where(e => e.NextRetryAtUtc <= DateTime.UtcNow))
        {
            execution.Start();
            await executionRepo.UpdateAsync(execution, cancellationToken);

            _ = Task.Run(() => ExecuteJobAsync(execution.Id, cancellationToken), cancellationToken);
        }
    }

    private async Task ExecuteJobAsync(Guid executionId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var execution = await executionRepo.GetByIdAsync(executionId, cancellationToken);
        if (execution == null)
        {
            _logger.LogError("Execution {ExecutionId} not found", executionId);
            return;
        }

        var job = await jobRepo.GetByIdAsync(execution.JobDefinitionId, cancellationToken);
        if (job == null)
        {
            _logger.LogError("Job {JobId} not found for execution {ExecutionId}",
                execution.JobDefinitionId, executionId);
            return;
        }

        try
        {
            _logger.LogInformation("Starting execution {ExecutionId} for job {JobName}",
                executionId, job.Name);

            execution.Start();
            await executionRepo.UpdateAsync(execution, cancellationToken);

            // Simulate job execution - in production, this would:
            // 1. Load the job implementation from the assembly
            // 2. Execute it with timeout
            // 3. Capture output
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(job.TimeoutSeconds));

            await SimulateJobWorkAsync(job, execution, cts.Token);

            execution.Complete($"{{\"result\": \"success\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}");
            await executionRepo.UpdateAsync(execution, cancellationToken);

            _logger.LogInformation("Completed execution {ExecutionId} for job {JobName} in {Duration}s",
                executionId, job.Name, execution.DurationSeconds);
        }
        catch (OperationCanceledException)
        {
            execution.Timeout();
            await executionRepo.UpdateAsync(execution, cancellationToken);

            _logger.LogWarning("Execution {ExecutionId} for job {JobName} timed out",
                executionId, job.Name);

            // Handle retry
            if (execution.CanRetry())
            {
                var delay = job.RetryPolicy.CalculateDelay(execution.RetryAttempt + 1);
                execution.ScheduleRetry(DateTime.UtcNow.AddSeconds(delay), delay);
                await executionRepo.UpdateAsync(execution, cancellationToken);

                _logger.LogInformation("Scheduled retry {Attempt}/{Max} for execution {ExecutionId} in {Delay}s",
                    execution.RetryAttempt, execution.MaxRetryAttempts, executionId, delay);
            }
        }
        catch (Exception ex)
        {
            execution.Fail(ex.Message, ex.StackTrace);
            await executionRepo.UpdateAsync(execution, cancellationToken);

            _logger.LogError(ex, "Execution {ExecutionId} for job {JobName} failed",
                executionId, job.Name);

            // Handle retry
            if (execution.CanRetry())
            {
                var delay = job.RetryPolicy.CalculateDelay(execution.RetryAttempt + 1);
                execution.ScheduleRetry(DateTime.UtcNow.AddSeconds(delay), delay);
                await executionRepo.UpdateAsync(execution, cancellationToken);

                _logger.LogInformation("Scheduled retry {Attempt}/{Max} for execution {ExecutionId} in {Delay}s",
                    execution.RetryAttempt, execution.MaxRetryAttempts, executionId, delay);
            }
        }
    }

    private async Task SimulateJobWorkAsync(JobDefinition job, JobExecution execution, CancellationToken cancellationToken)
    {
        // This simulates actual job work
        // In production, this would:
        // 1. Load the type from job.ExecutionAssembly and job.ExecutionTypeName
        // 2. Create an instance
        // 3. Execute the job with parameters
        // 4. Capture and return the result

        _logger.LogInformation("Executing job {JobName} (simulated)", job.Name);
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(2, 10)), cancellationToken);
        _logger.LogInformation("Job {JobName} execution completed (simulated)", job.Name);
    }
}
