using Jobs.Worker.Domain.Enums;
using MediatR;

namespace Jobs.Worker.Application.Commands;

public record CreateJobCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public DeploymentEnvironment AllowedEnvironments { get; init; }
    public ExecutionMode ExecutionMode { get; init; }
    public string ExecutionAssembly { get; init; } = string.Empty;
    public string ExecutionTypeName { get; init; } = string.Empty;
    public string? ExecutionCommand { get; init; }
    public string? ContainerImage { get; init; }
    public int TimeoutSeconds { get; init; }
    public int MaxRetries { get; init; }
    public RetryStrategy RetryStrategy { get; init; }
    public int BaseDelaySeconds { get; init; }
    public int MaxConcurrentExecutions { get; init; }
    public string OwnerName { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
}
