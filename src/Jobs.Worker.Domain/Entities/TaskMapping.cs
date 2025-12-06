using System;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.Entities;

public class TaskMapping
{
    public Guid Id { get; private set; }
    public Guid JobExecutionId { get; private set; }
    public string TaskIdentifier { get; private set; } = string.Empty; // Item ID or key
    public string TaskPayload { get; private set; } = "{}"; // JSON payload for this task
    public ExecutionStatus Status { get; private set; }
    public DateTime QueuedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }
    public string? ErrorMessage { get; private set; }

    public virtual JobExecution JobExecution { get; private set; } = null!;

    private TaskMapping() { }

    public TaskMapping(
        Guid jobExecutionId,
        string taskIdentifier,
        string taskPayload)
    {
        Id = Guid.NewGuid();
        JobExecutionId = jobExecutionId;
        TaskIdentifier = taskIdentifier;
        TaskPayload = taskPayload;
        Status = ExecutionStatus.Queued;
        QueuedAtUtc = DateTime.UtcNow;
    }

    public void Start()
    {
        Status = ExecutionStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ExecutionStatus.Succeeded;
        CompletedAtUtc = DateTime.UtcNow;
        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        if (StartedAtUtc.HasValue)
        {
            DurationSeconds = (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalSeconds;
        }
    }
}
