namespace Jobs.Worker.Domain.ValueObjects;

public class ExecutionContext
{
    public Guid ExecutionId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string TraceId { get; private set; }
    public string HostInstance { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private ExecutionContext()
    {
        Metadata = new Dictionary<string, object>();
        TraceId = string.Empty;
        HostInstance = string.Empty;
    }

    public ExecutionContext(Guid executionId, Guid correlationId, string traceId, string hostInstance)
    {
        ExecutionId = executionId;
        CorrelationId = correlationId;
        TraceId = traceId;
        HostInstance = hostInstance;
        Metadata = new Dictionary<string, object>();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public static ExecutionContext Create(string hostInstance, Guid? correlationId = null)
    {
        return new ExecutionContext(
            Guid.NewGuid(),
            correlationId ?? Guid.NewGuid(),
            Guid.NewGuid().ToString("N"),
            hostInstance
        );
    }
}
