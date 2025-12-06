using System;

namespace Jobs.Worker.Domain.ValueObjects;

public class CircuitBreakerPolicy
{
    public bool IsEnabled { get; private set; }
    public int FailureThreshold { get; private set; }
    public int ConsecutiveFailuresWindow { get; private set; }
    public int OpenDurationSeconds { get; private set; }
    public bool AutoRecover { get; private set; }
    public int HalfOpenMaxAttempts { get; private set; }

    private CircuitBreakerPolicy() { }

    public CircuitBreakerPolicy(
        bool isEnabled,
        int failureThreshold,
        int consecutiveFailuresWindow,
        int openDurationSeconds,
        bool autoRecover = true,
        int halfOpenMaxAttempts = 3)
    {
        if (failureThreshold < 1)
            throw new ArgumentException("Failure threshold must be at least 1", nameof(failureThreshold));

        if (consecutiveFailuresWindow < 1)
            throw new ArgumentException("Consecutive failures window must be at least 1", nameof(consecutiveFailuresWindow));

        if (openDurationSeconds < 1)
            throw new ArgumentException("Open duration must be at least 1 second", nameof(openDurationSeconds));

        if (halfOpenMaxAttempts < 1)
            throw new ArgumentException("Half-open max attempts must be at least 1", nameof(halfOpenMaxAttempts));

        IsEnabled = isEnabled;
        FailureThreshold = failureThreshold;
        ConsecutiveFailuresWindow = consecutiveFailuresWindow;
        OpenDurationSeconds = openDurationSeconds;
        AutoRecover = autoRecover;
        HalfOpenMaxAttempts = halfOpenMaxAttempts;
    }

    public static CircuitBreakerPolicy Disabled()
    {
        return new CircuitBreakerPolicy(
            isEnabled: false,
            failureThreshold: 5,
            consecutiveFailuresWindow: 10,
            openDurationSeconds: 300,
            autoRecover: true,
            halfOpenMaxAttempts: 3
        );
    }

    public static CircuitBreakerPolicy Default()
    {
        return new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 5,
            consecutiveFailuresWindow: 10,
            openDurationSeconds: 300,
            autoRecover: true,
            halfOpenMaxAttempts: 3
        );
    }

    public static CircuitBreakerPolicy Aggressive()
    {
        return new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 3,
            consecutiveFailuresWindow: 5,
            openDurationSeconds: 600,
            autoRecover: true,
            halfOpenMaxAttempts: 2
        );
    }

    public static CircuitBreakerPolicy Lenient()
    {
        return new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 10,
            consecutiveFailuresWindow: 20,
            openDurationSeconds: 180,
            autoRecover: true,
            halfOpenMaxAttempts: 5
        );
    }
}
