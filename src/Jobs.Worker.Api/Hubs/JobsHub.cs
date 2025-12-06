using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Jobs.Worker.Api.Hubs;

public class JobsHub : Hub
{
    private readonly ILogger<JobsHub> _logger;

    public JobsHub(ILogger<JobsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Method to send job execution updates to all clients
    public async Task SendJobExecutionUpdate(JobExecutionUpdateDto update)
    {
        await Clients.All.SendAsync("JobExecutionUpdated", update);
    }

    // Method to send job started notification
    public async Task SendJobStarted(JobExecutionUpdateDto update)
    {
        await Clients.All.SendAsync("JobStarted", update);
    }

    // Method to send job completed notification
    public async Task SendJobCompleted(JobExecutionUpdateDto update)
    {
        await Clients.All.SendAsync("JobCompleted", update);
    }

    // Method to send job failed notification
    public async Task SendJobFailed(JobExecutionUpdateDto update)
    {
        await Clients.All.SendAsync("JobFailed", update);
    }

    // Method to send metrics update
    public async Task SendMetricsUpdate(MetricsUpdateDto metrics)
    {
        await Clients.All.SendAsync("MetricsUpdated", metrics);
    }

    // Method to send audit log entry
    public async Task SendAuditLogEntry(AuditLogEntryDto auditLog)
    {
        await Clients.All.SendAsync("AuditLogAdded", auditLog);
    }

    // Method to send notification
    public async Task SendNotification(NotificationDto notification)
    {
        await Clients.All.SendAsync("NotificationReceived", notification);
    }
}

// DTOs for SignalR messages
public record JobExecutionUpdateDto(
    string ExecutionId,
    string JobId,
    string JobName,
    string Status,
    string? StartTime,
    string? EndTime,
    string? Output,
    string? ErrorMessage,
    int? Progress
);

public record MetricsUpdateDto(
    int TotalJobs,
    int ActiveJobs,
    int RunningExecutions,
    int FailedToday,
    int SucceededToday,
    double SuccessRatePercentage
);

public record AuditLogEntryDto(
    string Id,
    string Timestamp,
    string UserId,
    string UserName,
    string Action,
    string EntityType,
    string EntityId,
    string Changes
);

public record NotificationDto(
    string Id,
    string Timestamp,
    string Severity,
    string Title,
    string Message,
    string? JobId,
    string? ExecutionId
);
