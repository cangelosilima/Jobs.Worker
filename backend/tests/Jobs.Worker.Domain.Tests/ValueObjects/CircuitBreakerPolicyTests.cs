using System;
using FluentAssertions;
using Jobs.Worker.Domain.ValueObjects;
using Xunit;

namespace Jobs.Worker.Domain.Tests.ValueObjects;

public class CircuitBreakerPolicyTests
{
    [Fact]
    public void CircuitBreakerPolicy_WhenCreatedWithValidParameters_ShouldSetPropertiesCorrectly()
    {
        // Act
        var policy = new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 5,
            consecutiveFailuresWindow: 10,
            openDurationSeconds: 300,
            autoRecover: true,
            halfOpenMaxAttempts: 3
        );

        // Assert
        policy.IsEnabled.Should().BeTrue();
        policy.FailureThreshold.Should().Be(5);
        policy.ConsecutiveFailuresWindow.Should().Be(10);
        policy.OpenDurationSeconds.Should().Be(300);
        policy.AutoRecover.Should().BeTrue();
        policy.HalfOpenMaxAttempts.Should().Be(3);
    }

    [Fact]
    public void CircuitBreakerPolicy_WithInvalidFailureThreshold_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 0,
            consecutiveFailuresWindow: 10,
            openDurationSeconds: 300
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Failure threshold must be at least 1*");
    }

    [Fact]
    public void CircuitBreakerPolicy_WithInvalidConsecutiveFailuresWindow_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 5,
            consecutiveFailuresWindow: 0,
            openDurationSeconds: 300
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Consecutive failures window must be at least 1*");
    }

    [Fact]
    public void CircuitBreakerPolicy_WithInvalidOpenDuration_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 5,
            consecutiveFailuresWindow: 10,
            openDurationSeconds: 0
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Open duration must be at least 1 second*");
    }

    [Fact]
    public void CircuitBreakerPolicy_WithInvalidHalfOpenMaxAttempts_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new CircuitBreakerPolicy(
            isEnabled: true,
            failureThreshold: 5,
            consecutiveFailuresWindow: 10,
            openDurationSeconds: 300,
            autoRecover: true,
            halfOpenMaxAttempts: 0
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Half-open max attempts must be at least 1*");
    }

    [Fact]
    public void Disabled_ShouldCreatePolicyWithIsEnabledFalse()
    {
        // Act
        var policy = CircuitBreakerPolicy.Disabled();

        // Assert
        policy.IsEnabled.Should().BeFalse();
        policy.FailureThreshold.Should().Be(5);
        policy.ConsecutiveFailuresWindow.Should().Be(10);
        policy.OpenDurationSeconds.Should().Be(300);
        policy.AutoRecover.Should().BeTrue();
        policy.HalfOpenMaxAttempts.Should().Be(3);
    }

    [Fact]
    public void Default_ShouldCreatePolicyWithStandardValues()
    {
        // Act
        var policy = CircuitBreakerPolicy.Default();

        // Assert
        policy.IsEnabled.Should().BeTrue();
        policy.FailureThreshold.Should().Be(5);
        policy.ConsecutiveFailuresWindow.Should().Be(10);
        policy.OpenDurationSeconds.Should().Be(300);
        policy.AutoRecover.Should().BeTrue();
        policy.HalfOpenMaxAttempts.Should().Be(3);
    }

    [Fact]
    public void Aggressive_ShouldCreatePolicyWithStrictValues()
    {
        // Act
        var policy = CircuitBreakerPolicy.Aggressive();

        // Assert
        policy.IsEnabled.Should().BeTrue();
        policy.FailureThreshold.Should().Be(3);
        policy.ConsecutiveFailuresWindow.Should().Be(5);
        policy.OpenDurationSeconds.Should().Be(600);
        policy.AutoRecover.Should().BeTrue();
        policy.HalfOpenMaxAttempts.Should().Be(2);
    }

    [Fact]
    public void Lenient_ShouldCreatePolicyWithRelaxedValues()
    {
        // Act
        var policy = CircuitBreakerPolicy.Lenient();

        // Assert
        policy.IsEnabled.Should().BeTrue();
        policy.FailureThreshold.Should().Be(10);
        policy.ConsecutiveFailuresWindow.Should().Be(20);
        policy.OpenDurationSeconds.Should().Be(180);
        policy.AutoRecover.Should().BeTrue();
        policy.HalfOpenMaxAttempts.Should().Be(5);
    }
}
