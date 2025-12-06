using System;
using Jobs.Worker.Domain.ValueObjects;

namespace Jobs.Worker.Domain.Entities;

public class JobNotification
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public NotificationRule Rule { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;

    private JobNotification() { }

    public JobNotification(Guid jobDefinitionId, NotificationRule rule, string createdBy)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        Rule = rule;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateRule(NotificationRule newRule, string updatedBy)
    {
        Rule = newRule;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
