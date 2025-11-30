using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Commands;

public record CreateJobCommand
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
    // Backwards-compatible properties expected by integration tests / frontend
    public string AssemblyName { get => ExecutionAssembly; init => ExecutionAssembly = value; }
    public string ClassName { get => ExecutionTypeName; init => ExecutionTypeName = value; }
    public string? MethodName { get => ExecutionCommand; init => ExecutionCommand = value; }
    public bool AllowManualTrigger { get; init; } = true;

    public OwnerInfo? Owner { get; init; }

    public record OwnerInfo
    {
        public string UserId { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
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
