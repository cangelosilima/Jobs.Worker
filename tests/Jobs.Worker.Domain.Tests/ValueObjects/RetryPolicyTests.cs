using System;
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
        var policy = new RetryPolicy(5, RetryStrategy.Linear, 10, 100);

        // Act & Assert
        policy.CalculateDelay(1).Should().Be(10);  // 10 * 1
        policy.CalculateDelay(2).Should().Be(20);  // 10 * 2
        policy.CalculateDelay(3).Should().Be(30);  // 10 * 3
        policy.CalculateDelay(10).Should().Be(100); // Capped at maxDelay
    }

    [Fact]
    public void RetryPolicy_WithExponentialStrategy_ShouldCalculateCorrectDelay()
    {
        // Arrange
        var policy = new RetryPolicy(5, RetryStrategy.Exponential, 2, 60);

        // Act & Assert
        policy.CalculateDelay(1).Should().Be(2);   // 2^0 * 2 = 2
        policy.CalculateDelay(2).Should().Be(4);   // 2^1 * 2 = 4
        policy.CalculateDelay(3).Should().Be(8);   // 2^2 * 2 = 8
        policy.CalculateDelay(4).Should().Be(16);  // 2^3 * 2 = 16
        policy.CalculateDelay(5).Should().Be(32);  // 2^4 * 2 = 32
        policy.CalculateDelay(6).Should().Be(60);  // 2^5 * 2 = 64, capped at 60
    }

    [Fact]
    public void RetryPolicy_WithExponentialJitter_ShouldCalculateDelayWithinRange()
    {
        // Arrange
        var policy = new RetryPolicy(5, RetryStrategy.ExponentialWithJitter, 2, 60);

        // Act
        var delay = policy.CalculateDelay(3);

        // Assert
        // For attempt 3, base calculation would be 2^2 * 2 = 8 seconds
        // Jitter multiplies by a factor between 0.5 and 1.0
        // So result should be between 4 and 8 seconds
        delay.Should().BeGreaterOrEqualTo(4);
        delay.Should().BeLessOrEqualTo(8);
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
    public void RetryPolicy_Constructor_WithMaxRetriesAboveTen_ShouldThrow()
    {
        // Act
        Action act = () => new RetryPolicy(11, RetryStrategy.Linear, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*maxRetries*");
    }

    [Fact]
    public void RetryPolicy_Constructor_WithNegativeBaseDelay_ShouldThrow()
    {
        // Act
        Action act = () => new RetryPolicy(3, RetryStrategy.Linear, -5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*baseDelaySeconds*");
    }

    [Fact]
    public void RetryPolicy_Constructor_WithZeroBaseDelay_ShouldThrow()
    {
        // Act
        Action act = () => new RetryPolicy(3, RetryStrategy.Linear, 0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*baseDelaySeconds*");
    }

    [Fact]
    public void RetryPolicy_NoRetry_ShouldCreatePolicyWithZeroRetries()
    {
        // Act
        var policy = RetryPolicy.NoRetry();

        // Assert
        policy.MaxRetries.Should().Be(0);
        policy.Strategy.Should().Be(RetryStrategy.None);
    }

    [Fact]
    public void RetryPolicy_Default_ShouldCreatePolicyWithDefaultValues()
    {
        // Act
        var policy = RetryPolicy.Default();

        // Assert
        policy.MaxRetries.Should().Be(3);
        policy.Strategy.Should().Be(RetryStrategy.ExponentialWithJitter);
        policy.BaseDelaySeconds.Should().Be(30);
        policy.MaxDelaySeconds.Should().Be(300);
    }
}
