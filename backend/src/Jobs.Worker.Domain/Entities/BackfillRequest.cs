using System;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.Entities;

public class BackfillRequest
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public int TotalExecutions { get; private set; }
    public int CompletedExecutions { get; private set; }
    public int FailedExecutions { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public virtual JobDefinition JobDefinition { get; private set; } = null!;

    private BackfillRequest() { }

    public BackfillRequest(
        Guid jobDefinitionId,
        DateTime startDate,
        DateTime endDate,
        string requestedBy)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        StartDate = startDate;
        EndDate = endDate;
        Status = ExecutionStatus.Queued;
        RequestedBy = requestedBy;
        RequestedAtUtc = DateTime.UtcNow;

        // Calculate total executions needed (one per day)
        TotalExecutions = (int)(endDate - startDate).TotalDays + 1;
    }

    public void IncrementCompleted()
    {
        CompletedExecutions++;
        if (CompletedExecutions + FailedExecutions >= TotalExecutions)
        {
            Status = ExecutionStatus.Succeeded;
            CompletedAtUtc = DateTime.UtcNow;
        }
    }

    public void IncrementFailed()
    {
        FailedExecutions++;
        if (CompletedExecutions + FailedExecutions >= TotalExecutions)
        {
            Status = FailedExecutions == TotalExecutions ? ExecutionStatus.Failed : ExecutionStatus.Succeeded;
            CompletedAtUtc = DateTime.UtcNow;
        }
    }
}
