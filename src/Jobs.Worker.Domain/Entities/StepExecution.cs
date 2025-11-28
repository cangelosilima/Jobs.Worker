using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.Entities;

public class StepExecution
{
    public Guid Id { get; private set; }
    public Guid JobExecutionId { get; private set; }
    public Guid JobStepId { get; private set; }
    public StepStatus Status { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }
    public int RetryAttempt { get; private set; }
    public string? InputPayload { get; private set; }
    public string? OutputPayload { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? StackTrace { get; private set; }

    // Navigation properties
    public virtual JobExecution JobExecution { get; private set; } = null!;
    public virtual JobStep JobStep { get; private set; } = null!;

    private StepExecution() { }

    public StepExecution(
        Guid jobExecutionId,
        Guid jobStepId,
        string? inputPayload)
    {
        Id = Guid.NewGuid();
        JobExecutionId = jobExecutionId;
        JobStepId = jobStepId;
        Status = StepStatus.Pending;
        InputPayload = inputPayload;
        RetryAttempt = 0;
    }

    public void Start()
    {
        Status = StepStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Complete(string? outputPayload = null)
    {
        Status = StepStatus.Succeeded;
        CompletedAtUtc = DateTime.UtcNow;
        OutputPayload = outputPayload;

        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public void Fail(string errorMessage, string? stackTrace = null)
    {
        Status = StepStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        StackTrace = stackTrace;

        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public void Skip(string reason)
    {
        Status = StepStatus.Skipped;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = $"Skipped: {reason}";
    }

    public void ScheduleRetry()
    {
        RetryAttempt++;
        Status = StepStatus.Retrying;
    }
}
