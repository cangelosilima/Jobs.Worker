namespace Jobs.Worker.Domain.Enums;

public enum StepStatus
{
    Pending = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    Skipped = 5,
    Retrying = 6
}
