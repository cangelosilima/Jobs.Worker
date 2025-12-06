using System;

namespace Jobs.Worker.Domain.Entities;

public class JobExecutionLog
{
    public Guid Id { get; private set; }
    public Guid JobExecutionId { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string LogLevel { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? Exception { get; private set; }
    public string? Properties { get; private set; }

    // Navigation properties
    public virtual JobExecution JobExecution { get; private set; } = null!;

    private JobExecutionLog() { }

    public JobExecutionLog(Guid executionId, string logLevel, string message, string? exception = null, string? properties = null)
    {
        Id = Guid.NewGuid();
        JobExecutionId = executionId;
        TimestampUtc = DateTime.UtcNow;
        LogLevel = logLevel;
        Message = message;
        Exception = exception;
        Properties = properties;
    }
}
