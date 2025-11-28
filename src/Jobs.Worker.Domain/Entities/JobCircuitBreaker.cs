using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.Entities;

public class JobCircuitBreaker
{
    public Guid Id { get; private set; }
    public Guid JobDefinitionId { get; private set; }
    public CircuitBreakerState State { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public DateTime? LastFailureAtUtc { get; private set; }
    public DateTime? OpenedAtUtc { get; private set; }
    public DateTime? LastStateChangeAtUtc { get; private set; }
    public int HalfOpenAttempts { get; private set; }
    public string? OpenReason { get; private set; }
    public string? OpenedBy { get; private set; }

    // Navigation properties
    public virtual JobDefinition JobDefinition { get; private set; } = null!;

    private JobCircuitBreaker() { }

    public JobCircuitBreaker(Guid jobDefinitionId)
    {
        Id = Guid.NewGuid();
        JobDefinitionId = jobDefinitionId;
        State = CircuitBreakerState.Closed;
        ConsecutiveFailures = 0;
        LastStateChangeAtUtc = DateTime.UtcNow;
        HalfOpenAttempts = 0;
    }

    public void RecordFailure()
    {
        ConsecutiveFailures++;
        LastFailureAtUtc = DateTime.UtcNow;
    }

    public void RecordSuccess()
    {
        ConsecutiveFailures = 0;
        LastFailureAtUtc = null;

        if (State == CircuitBreakerState.HalfOpen)
        {
            Close();
        }
    }

    public void Open(string reason, string openedBy)
    {
        if (State == CircuitBreakerState.Open)
            return;

        State = CircuitBreakerState.Open;
        OpenedAtUtc = DateTime.UtcNow;
        OpenReason = reason;
        OpenedBy = openedBy;
        LastStateChangeAtUtc = DateTime.UtcNow;
        HalfOpenAttempts = 0;
    }

    public void MoveToHalfOpen()
    {
        if (State != CircuitBreakerState.Open)
            throw new InvalidOperationException("Can only move to HalfOpen from Open state");

        State = CircuitBreakerState.HalfOpen;
        LastStateChangeAtUtc = DateTime.UtcNow;
        HalfOpenAttempts = 0;
    }

    public void Close()
    {
        State = CircuitBreakerState.Closed;
        ConsecutiveFailures = 0;
        LastFailureAtUtc = null;
        OpenedAtUtc = null;
        OpenReason = null;
        OpenedBy = null;
        LastStateChangeAtUtc = DateTime.UtcNow;
        HalfOpenAttempts = 0;
    }

    public void IncrementHalfOpenAttempts()
    {
        if (State != CircuitBreakerState.HalfOpen)
            throw new InvalidOperationException("Can only increment attempts in HalfOpen state");

        HalfOpenAttempts++;
    }

    public bool ShouldTransitionToHalfOpen(int openDurationSeconds)
    {
        if (State != CircuitBreakerState.Open || !OpenedAtUtc.HasValue)
            return false;

        var elapsed = DateTime.UtcNow - OpenedAtUtc.Value;
        return elapsed.TotalSeconds >= openDurationSeconds;
    }

    public bool HasExceededHalfOpenAttempts(int maxAttempts)
    {
        return State == CircuitBreakerState.HalfOpen && HalfOpenAttempts >= maxAttempts;
    }
}
