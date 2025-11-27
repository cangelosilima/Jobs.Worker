using Jobs.Worker.Domain.ValueObjects;

namespace Jobs.Worker.Domain.Entities;

public class JobSchedule
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public ScheduleRule Rule { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime? StartDateUtc { get; private set; }
    public DateTime? EndDateUtc { get; private set; }
    public DateTime? LastExecutionUtc { get; private set; }
    public DateTime? NextExecutionUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;

    private JobSchedule() { }

    public JobSchedule(Guid jobDefinitionId, ScheduleRule rule, string createdBy, DateTime? startDate = null, DateTime? endDate = null)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        Rule = rule;
        IsActive = true;
        StartDateUtc = startDate;
        EndDateUtc = endDate;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateRule(ScheduleRule newRule, string updatedBy)
    {
        Rule = newRule;
        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
        NextExecutionUtc = null; // Will be recalculated
    }

    public void SetNextExecution(DateTime nextExecution)
    {
        NextExecutionUtc = nextExecution;
    }

    public void RecordExecution(DateTime executionTime)
    {
        LastExecutionUtc = executionTime;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool IsWithinActiveWindow(DateTime currentTime)
    {
        if (StartDateUtc.HasValue && currentTime < StartDateUtc.Value)
            return false;

        if (EndDateUtc.HasValue && currentTime > EndDateUtc.Value)
            return false;

        return true;
    }

    public bool ShouldExecute(DateTime currentTime)
    {
        return IsActive &&
               IsWithinActiveWindow(currentTime) &&
               NextExecutionUtc.HasValue &&
               NextExecutionUtc.Value <= currentTime;
    }
}
