using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobs.Worker.Api.Hubs;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobs.Worker.Api.Services;

public class CircuitBreakerMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<JobsHub> _hubContext;
    private readonly ILogger<CircuitBreakerMonitoringService> _logger;

    public CircuitBreakerMonitoringService(
        IServiceProvider serviceProvider,
        IHubContext<JobsHub> hubContext,
        ILogger<CircuitBreakerMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Circuit Breaker Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorCircuitBreakers(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Circuit Breaker Monitoring Service");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Circuit Breaker Monitoring Service stopped");
    }

    private async Task MonitorCircuitBreakers(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var circuitBreakerRepo = scope.ServiceProvider.GetRequiredService<IJobCircuitBreakerRepository>();
        var executionRepo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();

        try
        {
            // Get all active jobs with circuit breaker enabled
            var allJobs = await jobRepo.GetAllAsync(cancellationToken);
            var jobsWithCircuitBreaker = allJobs.Where(j => j.CircuitBreakerPolicy.IsEnabled).ToList();

            foreach (var job in jobsWithCircuitBreaker)
            {
                await ProcessJobCircuitBreaker(job, circuitBreakerRepo, executionRepo, jobRepo, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring circuit breakers");
        }
    }

    private async Task ProcessJobCircuitBreaker(
        JobDefinition job,
        IJobCircuitBreakerRepository circuitBreakerRepo,
        IJobExecutionRepository executionRepo,
        IJobRepository jobRepo,
        CancellationToken cancellationToken)
    {
        // Get or create circuit breaker
        var circuitBreaker = await circuitBreakerRepo.GetByJobIdAsync(job.Id, cancellationToken);
        if (circuitBreaker == null)
        {
            circuitBreaker = new JobCircuitBreaker(job.Id);
            await circuitBreakerRepo.AddAsync(circuitBreaker, cancellationToken);
        }

        var policy = job.CircuitBreakerPolicy;

        // Check current state
        switch (circuitBreaker.State)
        {
            case CircuitBreakerState.Closed:
                await MonitorClosedCircuit(job, circuitBreaker, policy, executionRepo, circuitBreakerRepo, jobRepo, cancellationToken);
                break;

            case CircuitBreakerState.Open:
                await MonitorOpenCircuit(job, circuitBreaker, policy, circuitBreakerRepo, cancellationToken);
                break;

            case CircuitBreakerState.HalfOpen:
                await MonitorHalfOpenCircuit(job, circuitBreaker, policy, executionRepo, circuitBreakerRepo, jobRepo, cancellationToken);
                break;
        }
    }

    private async Task MonitorClosedCircuit(
        JobDefinition job,
        JobCircuitBreaker circuitBreaker,
        Jobs.Worker.Domain.ValueObjects.CircuitBreakerPolicy policy,
        IJobExecutionRepository executionRepo,
        IJobCircuitBreakerRepository circuitBreakerRepo,
        IJobRepository jobRepo,
        CancellationToken cancellationToken)
    {
        // Get recent executions within the window
        var recentExecutions = job.Executions
            .OrderByDescending(e => e.QueuedAtUtc)
            .Take(policy.ConsecutiveFailuresWindow)
            .ToList();

        if (recentExecutions.Count < policy.ConsecutiveFailuresWindow)
            return;

        // Count consecutive failures from the most recent executions
        int consecutiveFailures = 0;
        foreach (var exec in recentExecutions)
        {
            if (exec.Status == ExecutionStatus.Failed || exec.Status == ExecutionStatus.TimedOut)
            {
                consecutiveFailures++;
            }
            else if (exec.Status == ExecutionStatus.Succeeded)
            {
                break; // Stop counting if we hit a success
            }
        }

        // Update circuit breaker failure count using public method
        for (int i = 0; i < consecutiveFailures; i++)
        {
            circuitBreaker.RecordFailure();
        }
        // Clear failures if needed
        if (circuitBreaker.ConsecutiveFailures > consecutiveFailures)
        {
            circuitBreaker.RecordSuccess();
        }

        // Check if we should open the circuit
        if (consecutiveFailures >= policy.FailureThreshold)
        {
            var reason = $"Circuit breaker opened due to {consecutiveFailures} consecutive failures (threshold: {policy.FailureThreshold})";
            circuitBreaker.Open(reason, "CircuitBreakerMonitor");

            // Auto-disable the job
            job.Disable("CircuitBreakerMonitor", reason);
            await jobRepo.UpdateAsync(job, cancellationToken);
            await circuitBreakerRepo.UpdateAsync(circuitBreaker, cancellationToken);

            _logger.LogWarning("Circuit breaker opened for job {JobId} ({JobName}): {Reason}",
                job.Id, job.Name, reason);

            // Send SignalR notification
            await _hubContext.Clients.All.SendAsync("CircuitBreakerOpened", new
            {
                JobId = job.Id,
                JobName = job.Name,
                Reason = reason,
                ConsecutiveFailures = consecutiveFailures,
                Threshold = policy.FailureThreshold
            }, cancellationToken);
        }
        else if (consecutiveFailures > 0)
        {
            await circuitBreakerRepo.UpdateAsync(circuitBreaker, cancellationToken);
        }
    }

    private async Task MonitorOpenCircuit(
        JobDefinition job,
        JobCircuitBreaker circuitBreaker,
        Jobs.Worker.Domain.ValueObjects.CircuitBreakerPolicy policy,
        IJobCircuitBreakerRepository circuitBreakerRepo,
        CancellationToken cancellationToken)
    {
        if (!policy.AutoRecover)
            return;

        // Check if enough time has passed to transition to half-open
        if (circuitBreaker.ShouldTransitionToHalfOpen(policy.OpenDurationSeconds))
        {
            circuitBreaker.MoveToHalfOpen();
            await circuitBreakerRepo.UpdateAsync(circuitBreaker, cancellationToken);

            _logger.LogInformation("Circuit breaker for job {JobId} ({JobName}) moved to HalfOpen state",
                job.Id, job.Name);

            // Send SignalR notification
            await _hubContext.Clients.All.SendAsync("CircuitBreakerHalfOpened", new
            {
                JobId = job.Id,
                JobName = job.Name,
                MaxAttempts = policy.HalfOpenMaxAttempts
            }, cancellationToken);
        }
    }

    private async Task MonitorHalfOpenCircuit(
        JobDefinition job,
        JobCircuitBreaker circuitBreaker,
        Jobs.Worker.Domain.ValueObjects.CircuitBreakerPolicy policy,
        IJobExecutionRepository executionRepo,
        IJobCircuitBreakerRepository circuitBreakerRepo,
        IJobRepository jobRepo,
        CancellationToken cancellationToken)
    {
        // Check recent executions since entering half-open state
        var executionsSinceHalfOpen = job.Executions
            .Where(e => e.QueuedAtUtc >= circuitBreaker.LastStateChangeAtUtc)
            .OrderByDescending(e => e.QueuedAtUtc)
            .Take(policy.HalfOpenMaxAttempts)
            .ToList();

        if (!executionsSinceHalfOpen.Any())
            return;

        var latestExecution = executionsSinceHalfOpen.First();

        // If latest execution succeeded, close the circuit
        if (latestExecution.Status == ExecutionStatus.Succeeded)
        {
            circuitBreaker.Close();

            // Re-activate the job
            job.Activate("CircuitBreakerMonitor");
            await jobRepo.UpdateAsync(job, cancellationToken);
            await circuitBreakerRepo.UpdateAsync(circuitBreaker, cancellationToken);

            _logger.LogInformation("Circuit breaker for job {JobId} ({JobName}) closed - job recovered",
                job.Id, job.Name);

            // Send SignalR notification
            await _hubContext.Clients.All.SendAsync("CircuitBreakerClosed", new
            {
                JobId = job.Id,
                JobName = job.Name
            }, cancellationToken);
        }
        // If we have too many failed attempts, reopen the circuit
        else if (circuitBreaker.HasExceededHalfOpenAttempts(policy.HalfOpenMaxAttempts))
        {
            var reason = $"Circuit breaker reopened - job failed to recover after {policy.HalfOpenMaxAttempts} attempts";
            circuitBreaker.Open(reason, "CircuitBreakerMonitor");
            await circuitBreakerRepo.UpdateAsync(circuitBreaker, cancellationToken);

            _logger.LogWarning("Circuit breaker for job {JobId} ({JobName}) reopened: {Reason}",
                job.Id, job.Name, reason);

            // Send SignalR notification
            await _hubContext.Clients.All.SendAsync("CircuitBreakerReopened", new
            {
                JobId = job.Id,
                JobName = job.Name,
                Reason = reason
            }, cancellationToken);
        }
        else
        {
            circuitBreaker.IncrementHalfOpenAttempts();
            await circuitBreakerRepo.UpdateAsync(circuitBreaker, cancellationToken);
        }
    }
}
