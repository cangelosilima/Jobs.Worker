namespace Jobs.Worker.Domain.Enums;

public enum ExecutionStatus
{
    Queued = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    TimedOut = 5,
    Cancelled = 6,
    Skipped = 7,
    Retrying = 8
}
