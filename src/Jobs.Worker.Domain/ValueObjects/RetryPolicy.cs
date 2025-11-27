using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.ValueObjects;

public class RetryPolicy
{
    public int MaxRetries { get; private set; }
    public RetryStrategy Strategy { get; private set; }
    public int BaseDelaySeconds { get; private set; }
    public int MaxDelaySeconds { get; private set; }

    private RetryPolicy() { }

    public RetryPolicy(int maxRetries, RetryStrategy strategy, int baseDelaySeconds, int maxDelaySeconds = 3600)
    {
        if (maxRetries < 0 || maxRetries > 10)
            throw new ArgumentException("Max retries must be between 0 and 10", nameof(maxRetries));

        if (baseDelaySeconds < 1)
            throw new ArgumentException("Base delay must be at least 1 second", nameof(baseDelaySeconds));

        MaxRetries = maxRetries;
        Strategy = strategy;
        BaseDelaySeconds = baseDelaySeconds;
        MaxDelaySeconds = maxDelaySeconds;
    }

    public int CalculateDelay(int attemptNumber)
    {
        return Strategy switch
        {
            RetryStrategy.Linear => Math.Min(BaseDelaySeconds * attemptNumber, MaxDelaySeconds),
            RetryStrategy.Exponential => Math.Min((int)Math.Pow(2, attemptNumber - 1) * BaseDelaySeconds, MaxDelaySeconds),
            RetryStrategy.ExponentialWithJitter => Math.Min((int)(Math.Pow(2, attemptNumber - 1) * BaseDelaySeconds * (0.5 + Random.Shared.NextDouble() * 0.5)), MaxDelaySeconds),
            _ => 0
        };
    }

    public static RetryPolicy NoRetry() => new(0, RetryStrategy.None, 0);
    public static RetryPolicy Default() => new(3, RetryStrategy.ExponentialWithJitter, 30, 300);
}
