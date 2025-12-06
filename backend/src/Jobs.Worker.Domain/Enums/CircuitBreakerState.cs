namespace Jobs.Worker.Domain.Enums;

public enum CircuitBreakerState
{
    Closed = 1,      // Normal operation
    Open = 2,        // Circuit breaker tripped, job disabled
    HalfOpen = 3     // Testing if job has recovered
}
