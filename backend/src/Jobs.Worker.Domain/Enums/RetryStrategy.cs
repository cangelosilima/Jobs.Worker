namespace Jobs.Worker.Domain.Enums;

public enum RetryStrategy
{
    None = 0,
    Linear = 1,
    Exponential = 2,
    ExponentialWithJitter = 3
}
