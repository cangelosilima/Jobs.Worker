using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.DTOs;

public record JobExecutionDto
{
    public Guid Id { get; init; }
    public Guid JobDefinitionId { get; init; }
    public string JobName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public ExecutionStatus Status { get; init; }
    public DateTime QueuedAtUtc { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public int DurationSeconds { get; init; }
    public int RetryAttempt { get; init; }
    public int MaxRetryAttempts { get; init; }
    public string HostInstance { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? TriggeredBy { get; init; }
    public bool IsManualTrigger { get; init; }
    public DateTime? NextRetryAtUtc { get; init; }
}
