using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;

namespace Jobs.Worker.Domain.Entities;

public class JobExecution
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public Guid? JobScheduleId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string TraceId { get; private set; } = string.Empty;
    public ExecutionStatus Status { get; private set; }
    public DateTime QueuedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }
    public int RetryAttempt { get; private set; }
    public int MaxRetryAttempts { get; private set; }
    public string HostInstance { get; private set; } = string.Empty;
    public string? InputPayload { get; private set; }
    public string? OutputPayload { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? StackTrace { get; private set; }
    public string? TriggeredBy { get; private set; }
    public bool IsManualTrigger { get; private set; }
    public DateTime? NextRetryAtUtc { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;
    public virtual JobSchedule? JobSchedule { get; private set; }
    public virtual ICollection<JobExecutionLog> Logs { get; private set; } = new List<JobExecutionLog>();
    public virtual ICollection<StepExecution> StepExecutions { get; private set; } = new List<StepExecution>();
    public virtual ICollection<JobCheckpoint> Checkpoints { get; private set; } = new List<JobCheckpoint>();
    public virtual ICollection<TaskMapping> TaskMappings { get; private set; } = new List<TaskMapping>();

    private JobExecution() { }

    public JobExecution(
        Guid jobDefinitionId,
        Guid? scheduleId,
        ExecutionContext context,
        string? inputPayload,
        string? triggeredBy,
        bool isManual,
        int maxRetryAttempts)
    {
        Id = context.ExecutionId;
        JobDefinitionId = jobDefinitionId;
        JobScheduleId = scheduleId;
        CorrelationId = context.CorrelationId;
        TraceId = context.TraceId;
        Status = ExecutionStatus.Queued;
        QueuedAtUtc = DateTime.UtcNow;
        HostInstance = context.HostInstance;
        InputPayload = inputPayload;
        TriggeredBy = triggeredBy;
        IsManualTrigger = isManual;
        RetryAttempt = 0;
        MaxRetryAttempts = maxRetryAttempts;
    }

    public void Start()
    {
        Status = ExecutionStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Complete(string? outputPayload = null)
    {
        Status = ExecutionStatus.Succeeded;
        CompletedAtUtc = DateTime.UtcNow;
        OutputPayload = outputPayload;

        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public void Fail(string errorMessage, string? stackTrace = null)
    {
        Status = ExecutionStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        StackTrace = stackTrace;

        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public void Timeout()
    {
        Status = ExecutionStatus.TimedOut;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = "Execution timed out";

        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public void Cancel(string reason)
    {
        Status = ExecutionStatus.Cancelled;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = $"Cancelled: {reason}";

        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public void Skip(string reason)
    {
        Status = ExecutionStatus.Skipped;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = $"Skipped: {reason}";
    }

    public void ScheduleRetry(DateTime nextRetryTime, int delaySeconds)
    {
        RetryAttempt++;
        Status = ExecutionStatus.Retrying;
        NextRetryAtUtc = nextRetryTime;

        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(DateTime.UtcNow - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public bool CanRetry()
    {
        return RetryAttempt < MaxRetryAttempts &&
               (Status == ExecutionStatus.Failed || Status == ExecutionStatus.TimedOut);
    }

    public bool IsCompleted()
    {
        return Status == ExecutionStatus.Succeeded ||
               Status == ExecutionStatus.Failed ||
               Status == ExecutionStatus.TimedOut ||
               Status == ExecutionStatus.Cancelled ||
               Status == ExecutionStatus.Skipped;
    }
}
