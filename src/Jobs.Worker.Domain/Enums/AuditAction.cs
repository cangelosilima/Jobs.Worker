namespace Jobs.Worker.Domain.Enums;

public enum AuditAction
{
    JobCreated = 1,
    JobUpdated = 2,
    JobDisabled = 3,
    JobEnabled = 4,
    JobDeleted = 5,
    ScheduleChanged = 6,
    ManualTrigger = 7,
    ExecutionStarted = 8,
    ExecutionCompleted = 9,
    ExecutionFailed = 10,
    ExecutionCancelled = 11,
    RetryAttempted = 12,
    NotificationSent = 13,
    ParameterChanged = 14,
    OwnershipChanged = 15
}
