using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.Entities;

public class JobAudit
{
    public Guid Id { get; private set; }
    public Guid? JobDefinitionId { get; private set; }
    public Guid? JobExecutionId { get; private set; }
    public AuditAction Action { get; private set; }
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime PerformedAtUtc { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? AdditionalData { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;

    private JobAudit() { }

    public JobAudit(
        Guid? jobDefinitionId,
        Guid? executionId,
        AuditAction action,
        string performedBy,
        string ipAddress,
        string userAgent,
        string? oldValues = null,
        string? newValues = null,
        string? additionalData = null)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        JobExecutionId = executionId;
        Action = action;
        PerformedBy = performedBy;
        PerformedAtUtc = DateTime.UtcNow;
        OldValues = oldValues;
        NewValues = newValues;
        AdditionalData = additionalData;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public static JobAudit CreateJobCreated(Guid jobId, string createdBy, string ipAddress, string userAgent, string jobData)
    {
        return new JobAudit(jobId, null, AuditAction.JobCreated, createdBy, ipAddress, userAgent, null, jobData);
    }

    public static JobAudit CreateJobUpdated(Guid jobId, string updatedBy, string ipAddress, string userAgent, string oldData, string newData)
    {
        return new JobAudit(jobId, null, AuditAction.JobUpdated, updatedBy, ipAddress, userAgent, oldData, newData);
    }

    public static JobAudit CreateExecutionStarted(Guid jobId, Guid executionId, string triggeredBy, string ipAddress, string userAgent)
    {
        return new JobAudit(jobId, executionId, AuditAction.ExecutionStarted, triggeredBy, ipAddress, userAgent);
    }

    public static JobAudit CreateExecutionCompleted(Guid jobId, Guid executionId, string status, string ipAddress, string userAgent)
    {
        return new JobAudit(jobId, executionId, AuditAction.ExecutionCompleted, "System", ipAddress, userAgent, null, status);
    }
}
