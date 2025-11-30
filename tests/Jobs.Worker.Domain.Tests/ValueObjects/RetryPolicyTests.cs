using FluentAssertions;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;
using Xunit;

namespace Jobs.Worker.Domain.Tests.ValueObjects;

public class RetryPolicyTests
{
    [Fact]
    public void RetryPolicy_WithLinearStrategy_ShouldCalculateCorrectDelay()
    {
        // Arrange
        var policy = new RetryPolicy(
            maxRetries: 5,
            strategy: RetryStrategy.Linear,
            initialDelaySeconds: 10,
            maxDelaySeconds: null,
            backoffMultiplier: null
        );

        // Act & Assert
        policy.CalculateDelay(1).Should().Be(TimeSpan.FromSeconds(10));
        policy.CalculateDelay(2).Should().Be(TimeSpan.FromSeconds(10));
        policy.CalculateDelay(3).Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void RetryPolicy_WithExponentialStrategy_ShouldCalculateCorrectDelay()
    {
        // Arrange
        var policy = new RetryPolicy(
            maxRetries: 5,
            strategy: RetryStrategy.Exponential,
            initialDelaySeconds: 2,
            maxDelaySeconds: 60,
            backoffMultiplier: 2.0
        );

        // Act & Assert
        policy.CalculateDelay(1).Should().Be(TimeSpan.FromSeconds(2));   // 2^1
        policy.CalculateDelay(2).Should().Be(TimeSpan.FromSeconds(4));   // 2^2
        policy.CalculateDelay(3).Should().Be(TimeSpan.FromSeconds(8));   // 2^3
        policy.CalculateDelay(4).Should().Be(TimeSpan.FromSeconds(16));  // 2^4
        policy.CalculateDelay(5).Should().Be(TimeSpan.FromSeconds(32));  // 2^5
        policy.CalculateDelay(6).Should().Be(TimeSpan.FromSeconds(60));  // Capped at max
    }

    [Fact]
    public void RetryPolicy_WithExponentialJitter_ShouldCalculateDelayWithinRange()
    {
        // Arrange
        var policy = new RetryPolicy(
            maxRetries: 5,
            strategy: RetryStrategy.ExponentialWithJitter,
            initialDelaySeconds: 2,
            maxDelaySeconds: 60,
            backoffMultiplier: 2.0
        );

        // Act
        var delay = policy.CalculateDelay(3);

        // Assert
        // For attempt 3, base delay would be 8 seconds
        // Jitter adds randomness, so it should be between 0 and 8 seconds
        delay.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        delay.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public void RetryPolicy_Constructor_WithNegativeMaxRetries_ShouldThrow()
    {
        // Act
        Action act = () => new RetryPolicy(-1, RetryStrategy.Linear, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*maxRetries*");
    }

    [Fact]
    public void RetryPolicy_Constructor_WithNegativeInitialDelay_ShouldThrow()
    {
        // Act
        Action act = () => new RetryPolicy(3, RetryStrategy.Linear, -5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*initialDelaySeconds*");
    }

    [Fact]
    public void ShouldRetry_WhenAttemptLessThanMax_ShouldReturnTrue()
    {
        // Arrange
        var policy = new RetryPolicy(3, RetryStrategy.Linear, 5);

        // Act & Assert
        policy.ShouldRetry(0).Should().BeTrue();
        policy.ShouldRetry(1).Should().BeTrue();
        policy.ShouldRetry(2).Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_WhenAttemptEqualsMax_ShouldReturnFalse()
    {
        // Arrange
        var policy = new RetryPolicy(3, RetryStrategy.Linear, 5);

        // Act & Assert
        policy.ShouldRetry(3).Should().BeFalse();
        policy.ShouldRetry(4).Should().BeFalse();
    }
}
