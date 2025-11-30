using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.Entities;

public class JobFeatureFlag
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public string FlagName { get; private set; } = string.Empty;
    public string FlagValue { get; private set; } = string.Empty;
    public DeploymentEnvironment Environments { get; private set; }
    public bool IsEnabled { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public virtual JobDefinition JobDefinition { get; private set; } = null!;

    private JobFeatureFlag() { }

    public JobFeatureFlag(
        Guid jobDefinitionId,
        string flagName,
        string flagValue,
        DeploymentEnvironment environments,
        string createdBy)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        FlagName = flagName;
        FlagValue = flagValue;
        Environments = environments;
        IsEnabled = true;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateValue(string flagValue, string updatedBy)
    {
        FlagValue = flagValue;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Toggle()
    {
        IsEnabled = !IsEnabled;
    }

    public bool IsEnabledForEnvironment(DeploymentEnvironment environment)
    {
        return IsEnabled && Environments.HasFlag(environment);
    }
}
