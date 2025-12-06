using System;

namespace Jobs.Worker.Domain.Entities;

public class JobDependency
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public Guid DependsOnJobId { get; private set; }
    public int DelayAfterCompletionSeconds { get; private set; }
    public bool FailIfDependencyFails { get; private set; }
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;
    public virtual JobDefinition DependsOnJob { get; private set; } = null!;

    private JobDependency() { }

    public JobDependency(
        Guid jobDefinitionId,
        Guid dependsOnJobId,
        int delaySeconds,
        bool failIfDependencyFails,
        string createdBy)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        DependsOnJobId = dependsOnJobId;
        DelayAfterCompletionSeconds = delaySeconds;
        FailIfDependencyFails = failIfDependencyFails;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
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
