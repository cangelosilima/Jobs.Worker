using System;

namespace Jobs.Worker.Domain.Entities;

public class JobParameter
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public string ParameterName { get; private set; } = string.Empty;
    public string ParameterValue { get; private set; } = string.Empty;
    public string ParameterType { get; private set; } = "String";
    public bool IsEncrypted { get; private set; }
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
    public string? Description { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;

    private JobParameter() { }

    public JobParameter(
        Guid jobDefinitionId,
        string name,
        string value,
        string type,
        bool isRequired,
        bool isEncrypted,
        string createdBy,
        string? description = null,
        string? defaultValue = null)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        ParameterName = name;
        ParameterValue = value;
        ParameterType = type;
        IsRequired = isRequired;
        IsEncrypted = isEncrypted;
        Description = description;
        DefaultValue = defaultValue;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateValue(string newValue, string updatedBy)
    {
        ParameterValue = newValue;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
