using System.Threading;
using FluentAssertions;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;
using Xunit;

namespace Jobs.Worker.Domain.Tests.Entities;

public class JobExecutionTests
{
    [Fact]
    public void JobExecution_WhenCreated_ShouldHaveCorrectInitialState()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var context = ExecutionContext.Create("test-host");
        var triggeredBy = "test-user";

        // Act
        var execution = new JobExecution(jobId, scheduleId, context, "input", triggeredBy, false, 3);

        // Assert
        execution.Id.Should().Be(context.ExecutionId);
        execution.JobDefinitionId.Should().Be(jobId);
        execution.JobScheduleId.Should().Be(scheduleId);
        execution.CorrelationId.Should().Be(context.CorrelationId);
        execution.TraceId.Should().Be(context.TraceId);
        execution.Status.Should().Be(ExecutionStatus.Queued);
        execution.HostInstance.Should().Be("test-host");
        execution.InputPayload.Should().Be("input");
        execution.TriggeredBy.Should().Be(triggeredBy);
        execution.IsManualTrigger.Should().BeFalse();
        execution.RetryAttempt.Should().Be(0);
        execution.MaxRetryAttempts.Should().Be(3);
        execution.QueuedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Start_WhenCalled_ShouldSetStatusToRunningAndRecordStartTime()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        execution.Start();

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Running);
        execution.StartedAtUtc.Should().NotBeNull();
        execution.StartedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Complete_WhenCalled_ShouldSetStatusToSucceededAndCalculateDuration()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        Thread.Sleep(100); // Small delay to ensure duration > 0

        // Act
        execution.Complete("output data");

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Succeeded);
        execution.CompletedAtUtc.Should().NotBeNull();
        execution.OutputPayload.Should().Be("output data");
        execution.DurationSeconds.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Fail_WhenCalled_ShouldSetStatusToFailedAndRecordError()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        execution.Fail("Test error", "Stack trace");

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Failed);
        execution.CompletedAtUtc.Should().NotBeNull();
        execution.ErrorMessage.Should().Be("Test error");
        execution.StackTrace.Should().Be("Stack trace");
        execution.DurationSeconds.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Timeout_WhenCalled_ShouldSetStatusToTimedOut()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        execution.Timeout();

        // Assert
        execution.Status.Should().Be(ExecutionStatus.TimedOut);
        execution.CompletedAtUtc.Should().NotBeNull();
        execution.ErrorMessage.Should().Be("Execution timed out");
        execution.DurationSeconds.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Cancel_WhenCalled_ShouldSetStatusToCancelledWithReason()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        execution.Cancel("User requested cancellation");

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Cancelled);
        execution.CompletedAtUtc.Should().NotBeNull();
        execution.ErrorMessage.Should().Be("Cancelled: User requested cancellation");
        execution.DurationSeconds.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Skip_WhenCalled_ShouldSetStatusToSkippedWithReason()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        execution.Skip("Dependency failed");

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Skipped);
        execution.CompletedAtUtc.Should().NotBeNull();
        execution.ErrorMessage.Should().Be("Skipped: Dependency failed");
    }

    [Fact]
    public void ScheduleRetry_WhenCalled_ShouldIncrementRetryAttemptAndSetNextRetryTime()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Fail("Temporary error");
        var nextRetryTime = DateTime.UtcNow.AddSeconds(30);

        // Act
        execution.ScheduleRetry(nextRetryTime, 30);

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Retrying);
        execution.RetryAttempt.Should().Be(1);
        execution.NextRetryAtUtc.Should().Be(nextRetryTime);
        execution.DurationSeconds.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void CanRetry_WhenRetryAttemptsRemain_ShouldReturnTrue()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Fail("Error");

        // Act
        var canRetry = execution.CanRetry();

        // Assert
        canRetry.Should().BeTrue();
        execution.RetryAttempt.Should().Be(0);
        execution.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public void CanRetry_WhenMaxRetriesReached_ShouldReturnFalse()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Fail("Error 1");
        execution.ScheduleRetry(DateTime.UtcNow.AddSeconds(10), 10);
        execution.Start();
        execution.Fail("Error 2");
        execution.ScheduleRetry(DateTime.UtcNow.AddSeconds(10), 10);
        execution.Start();
        execution.Fail("Error 3");
        execution.ScheduleRetry(DateTime.UtcNow.AddSeconds(10), 10);
        execution.Start();
        execution.Fail("Error 4");

        // Act
        var canRetry = execution.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
        execution.RetryAttempt.Should().Be(3);
    }

    [Fact]
    public void CanRetry_WhenExecutionSucceeded_ShouldReturnFalse()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Complete();

        // Act
        var canRetry = execution.CanRetry();

        // Assert
        canRetry.Should().BeFalse();
    }

    [Fact]
    public void IsCompleted_WhenExecutionSucceeded_ShouldReturnTrue()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Complete();

        // Act
        var isCompleted = execution.IsCompleted();

        // Assert
        isCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsCompleted_WhenExecutionFailed_ShouldReturnTrue()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Fail("Error");

        // Act
        var isCompleted = execution.IsCompleted();

        // Assert
        isCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsCompleted_WhenExecutionRunning_ShouldReturnFalse()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        var isCompleted = execution.IsCompleted();

        // Assert
        isCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsCompleted_WhenExecutionCancelled_ShouldReturnTrue()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Cancel("User cancelled");

        // Act
        var isCompleted = execution.IsCompleted();

        // Assert
        isCompleted.Should().BeTrue();
    }

    [Fact]
    public void IsCompleted_WhenExecutionSkipped_ShouldReturnTrue()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Skip("Dependency failed");

        // Act
        var isCompleted = execution.IsCompleted();

        // Assert
        isCompleted.Should().BeTrue();
    }

    private JobExecution CreateExecution()
    {
        var jobId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var context = ExecutionContext.Create("test-host");
        return new JobExecution(jobId, scheduleId, context, null, "test-user", false, 3);
    }
}
