using Jobs.Worker.Domain.Enums;
using System.Text.Json.Serialization;

namespace Jobs.Worker.Application.Commands;

public class CreateJobCommand
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = "General";

    [JsonPropertyName("allowedEnvironments")]
    public DeploymentEnvironment AllowedEnvironments { get; set; }

    [JsonPropertyName("executionMode")]
    public ExecutionMode ExecutionMode { get; set; }

    [JsonPropertyName("executionAssembly")]
    public string ExecutionAssembly { get; set; } = string.Empty;

    [JsonPropertyName("executionTypeName")]
    public string ExecutionTypeName { get; set; } = string.Empty;

    [JsonPropertyName("executionCommand")]
    public string? ExecutionCommand { get; set; }

    [JsonPropertyName("containerImage")]
    public string? ContainerImage { get; set; }

    // Backwards-compatible properties
    [JsonPropertyName("assemblyName")]
    public string? AssemblyName { get; set; }

    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    [JsonPropertyName("methodName")]
    public string? MethodName { get; set; }

    [JsonPropertyName("allowManualTrigger")]
    public bool AllowManualTrigger { get; set; } = true;

    [JsonPropertyName("owner")]
    public OwnerInfo? Owner { get; set; }

    public class OwnerInfo
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;

    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; }

    [JsonPropertyName("retryStrategy")]
    public RetryStrategy RetryStrategy { get; set; }

    [JsonPropertyName("baseDelaySeconds")]
    public int BaseDelaySeconds { get; set; }

    [JsonPropertyName("maxConcurrentExecutions")]
    public int MaxConcurrentExecutions { get; set; } = 1;

    [JsonPropertyName("ownerName")]
    public string OwnerName { get; set; } = string.Empty;

    [JsonPropertyName("ownerEmail")]
    public string OwnerEmail { get; set; } = string.Empty;

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = "system";
}
