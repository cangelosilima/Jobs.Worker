using System;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Responses;

public record JobDefinitionResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public JobStatus Status { get; init; }
    public DeploymentEnvironment AllowedEnvironments { get; init; }
    public ExecutionMode ExecutionMode { get; init; }
    public int TimeoutSeconds { get; init; }
    public int MaxRetries { get; init; }
    public RetryStrategy RetryStrategy { get; init; }
    public int MaxConcurrentExecutions { get; init; }
    public int? ExpectedDurationSeconds { get; init; }
    public DateTime? LastExecutionUtc { get; init; }
    public ExecutionStatus? LastExecutionStatus { get; init; }
    public string? OwnerName { get; init; }
    public string? OwnerEmail { get; init; }
    public string? TeamName { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
