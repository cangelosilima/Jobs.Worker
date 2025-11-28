using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.Entities;

public class JobTemplate
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string TemplateType { get; private set; } = string.Empty; // ETL, Report, Reconciliation
    public string ConfigurationSchema { get; private set; } = "{}"; // JSON schema
    public string DefaultConfiguration { get; private set; } = "{}"; // JSON defaults
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private JobTemplate() { }

    public JobTemplate(
        string name,
        string description,
        string category,
        string templateType,
        string configurationSchema,
        string defaultConfiguration,
        string createdBy)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Category = category;
        TemplateType = templateType;
        ConfigurationSchema = configurationSchema;
        DefaultConfiguration = defaultConfiguration;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string name, string description, string updatedBy)
    {
        Name = name;
        Description = description;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
