using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jobs.Worker.Client
{
    /// <summary>
    /// Base SignalR client for Jobs.Worker API
    /// </summary>
    public class JobsHubClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly ClientSettings _settings;

        /// <summary>
        /// Gets the connection state
        /// </summary>
        public HubConnectionState State => _connection.State;

        /// <summary>
        /// Initializes a new instance of the JobsHubClient class
        /// </summary>
        /// <param name="settings">Client settings (optional)</param>
        public JobsHubClient(ClientSettings? settings = null)
        {
            _settings = settings ?? new ClientSettings();

            var hubUrl = $"{_settings.BaseUrl.TrimEnd('/')}/hubs/jobs";

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            RegisterHandlers();
        }

        /// <summary>
        /// Registers default event handlers
        /// </summary>
        private void RegisterHandlers()
        {
            _connection.Closed += OnConnectionClosed;
            _connection.Reconnecting += OnReconnecting;
            _connection.Reconnected += OnReconnected;
        }

        /// <summary>
        /// Event raised when the connection is closed
        /// </summary>
        public event Func<Exception?, Task>? ConnectionClosed;

        /// <summary>
        /// Event raised when reconnecting
        /// </summary>
        public event Func<Exception?, Task>? Reconnecting;

        /// <summary>
        /// Event raised when reconnected
        /// </summary>
        public event Func<string?, Task>? Reconnected;

        private Task OnConnectionClosed(Exception? exception)
        {
            return ConnectionClosed?.Invoke(exception) ?? Task.CompletedTask;
        }

        private Task OnReconnecting(Exception? exception)
        {
            return Reconnecting?.Invoke(exception) ?? Task.CompletedTask;
        }

        private Task OnReconnected(string? connectionId)
        {
            return Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Starts the connection to the SignalR hub
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Stops the connection to the SignalR hub
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_connection.State != HubConnectionState.Disconnected)
            {
                await _connection.StopAsync(cancellationToken);
            }
        }

        #region Server-to-Client Events

        /// <summary>
        /// Event raised when a job execution is updated
        /// </summary>
        public event Action<JobExecutionUpdateDto>? OnJobExecutionUpdated;

        /// <summary>
        /// Event raised when a job is started
        /// </summary>
        public event Action<JobExecutionUpdateDto>? OnJobStarted;

        /// <summary>
        /// Event raised when a job is completed
        /// </summary>
        public event Action<JobExecutionUpdateDto>? OnJobCompleted;

        /// <summary>
        /// Event raised when a job fails
        /// </summary>
        public event Action<JobExecutionUpdateDto>? OnJobFailed;

        /// <summary>
        /// Event raised when metrics are updated
        /// </summary>
        public event Action<MetricsUpdateDto>? OnMetricsUpdated;

        /// <summary>
        /// Event raised when an audit log entry is added
        /// </summary>
        public event Action<AuditLogEntryDto>? OnAuditLogAdded;

        /// <summary>
        /// Event raised when a notification is received
        /// </summary>
        public event Action<NotificationDto>? OnNotificationReceived;

        /// <summary>
        /// Subscribes to all hub events
        /// </summary>
        public void SubscribeToEvents()
        {
            _connection.On<JobExecutionUpdateDto>("JobExecutionUpdated", (update) => OnJobExecutionUpdated?.Invoke(update));
            _connection.On<JobExecutionUpdateDto>("JobStarted", (update) => OnJobStarted?.Invoke(update));
            _connection.On<JobExecutionUpdateDto>("JobCompleted", (update) => OnJobCompleted?.Invoke(update));
            _connection.On<JobExecutionUpdateDto>("JobFailed", (update) => OnJobFailed?.Invoke(update));
            _connection.On<MetricsUpdateDto>("MetricsUpdated", (metrics) => OnMetricsUpdated?.Invoke(metrics));
            _connection.On<AuditLogEntryDto>("AuditLogAdded", (auditLog) => OnAuditLogAdded?.Invoke(auditLog));
            _connection.On<NotificationDto>("NotificationReceived", (notification) => OnNotificationReceived?.Invoke(notification));
        }

        #endregion

        #region Client-to-Server Methods

        /// <summary>
        /// Sends a job execution update to the hub
        /// </summary>
        public async Task SendJobExecutionUpdateAsync(JobExecutionUpdateDto update, CancellationToken cancellationToken = default)
        {
            await _connection.InvokeAsync("SendJobExecutionUpdate", update, cancellationToken);
        }

        /// <summary>
        /// Sends a job started notification to the hub
        /// </summary>
        public async Task SendJobStartedAsync(JobExecutionUpdateDto update, CancellationToken cancellationToken = default)
        {
            await _connection.InvokeAsync("SendJobStarted", update, cancellationToken);
        }

        /// <summary>
        /// Sends a job completed notification to the hub
        /// </summary>
        public async Task SendJobCompletedAsync(JobExecutionUpdateDto update, CancellationToken cancellationToken = default)
        {
            await _connection.InvokeAsync("SendJobCompleted", update, cancellationToken);
        }

        /// <summary>
        /// Sends a job failed notification to the hub
        /// </summary>
        public async Task SendJobFailedAsync(JobExecutionUpdateDto update, CancellationToken cancellationToken = default)
        {
            await _connection.InvokeAsync("SendJobFailed", update, cancellationToken);
        }

        /// <summary>
        /// Sends a metrics update to the hub
        /// </summary>
        public async Task SendMetricsUpdateAsync(MetricsUpdateDto metrics, CancellationToken cancellationToken = default)
        {
            await _connection.InvokeAsync("SendMetricsUpdate", metrics, cancellationToken);
        }

        /// <summary>
        /// Sends an audit log entry to the hub
        /// </summary>
        public async Task SendAuditLogEntryAsync(AuditLogEntryDto auditLog, CancellationToken cancellationToken = default)
        {
            await _connection.InvokeAsync("SendAuditLogEntry", auditLog, cancellationToken);
        }

        /// <summary>
        /// Sends a notification to the hub
        /// </summary>
        public async Task SendNotificationAsync(NotificationDto notification, CancellationToken cancellationToken = default)
        {
            await _connection.InvokeAsync("SendNotification", notification, cancellationToken);
        }

        #endregion

        /// <summary>
        /// Disposes the client and releases resources
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    #region DTOs

    /// <summary>
    /// Job execution update DTO
    /// </summary>
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

    /// <summary>
    /// Metrics update DTO
    /// </summary>
    public record MetricsUpdateDto(
        int TotalJobs,
        int ActiveJobs,
        int RunningExecutions,
        int FailedToday,
        int SucceededToday,
        double SuccessRatePercentage
    );

    /// <summary>
    /// Audit log entry DTO
    /// </summary>
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

    /// <summary>
    /// Notification DTO
    /// </summary>
    public record NotificationDto(
        string Id,
        string Timestamp,
        string Severity,
        string Title,
        string Message,
        string? JobId,
        string? ExecutionId
    );

    #endregion
}
