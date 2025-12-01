using Jobs.Worker.Api.Hubs;
using Jobs.Worker.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Jobs.Worker.Api.Services;

public class SignalRNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<JobsHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IServiceProvider serviceProvider,
        IHubContext<JobsHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SignalR Notification Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendMetricsUpdate(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignalR Notification Service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("SignalR Notification Service stopped");
    }

    private async Task SendMetricsUpdate(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var execRepo = scope.ServiceProvider.GetRequiredService<IJobExecutionRepository>();

        try
        {
            var allJobs = await jobRepo.GetAllAsync();
            var runningExecutions = await execRepo.GetRunningExecutionsAsync();
            var failedToday = await execRepo.GetFailedExecutionsTodayAsync();

            var totalJobs = allJobs.Count();
            var activeJobs = allJobs.Count(j => j.Status == Domain.Enums.JobStatus.Active);
            var succeededToday = allJobs.Sum(j => j.Executions.Count(e =>
                e.Status == Domain.Enums.ExecutionStatus.Succeeded &&
                e.QueuedAtUtc.Date == DateTime.UtcNow.Date));

            var totalToday = succeededToday + failedToday.Count();
            var successRate = totalToday > 0 ? (double)succeededToday / totalToday * 100 : 0;

            var metrics = new MetricsUpdateDto(
                totalJobs,
                activeJobs,
                runningExecutions.Count(),
                failedToday.Count(),
                succeededToday,
                successRate
            );

            await _hubContext.Clients.All.SendAsync("MetricsUpdated", metrics, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending metrics update");
        }
    }
}
