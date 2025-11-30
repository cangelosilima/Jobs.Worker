using FluentAssertions;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Domain.ValueObjects;
using Xunit;

namespace Jobs.Worker.Domain.Tests.Entities;

public class JobDefinitionTests
{
    [Fact]
    public void JobDefinition_WhenCreated_ShouldHaveCorrectInitialState()
    {
        // Arrange & Act
        var job = new JobDefinition(
            "TestJob",
            "Test Description",
            "TestAssembly",
            "TestClass",
            "TestMethod",
            "testuser"
        );

        // Assert
        job.Name.Should().Be("TestJob");
        job.Description.Should().Be("Test Description");
        job.AssemblyName.Should().Be("TestAssembly");
        job.ClassName.Should().Be("TestClass");
        job.MethodName.Should().Be("TestMethod");
        job.Status.Should().Be(JobStatus.Draft);
        job.CreatedBy.Should().Be("testuser");
        job.Version.Should().Be(1);
    }

    [Fact]
    public void Activate_WhenCalled_ShouldSetStatusToActive()
    {
        // Arrange
        var job = new JobDefinition("Test", "Desc", "Asm", "Class", "Method", "user");

        // Act
        job.Activate("admin");

        // Assert
        job.Status.Should().Be(JobStatus.Active);
        job.UpdatedBy.Should().Be("admin");
    }

    [Fact]
    public void Disable_WhenCalled_ShouldSetStatusToDisabled()
    {
        // Arrange
        var job = new JobDefinition("Test", "Desc", "Asm", "Class", "Method", "user");
        job.Activate("admin");

        // Act
        job.Disable("admin", "No longer needed");

        // Assert
        job.Status.Should().Be(JobStatus.Disabled);
        job.DisabledReason.Should().Be("No longer needed");
    }

    [Fact]
    public void Archive_WhenCalled_ShouldSetStatusToArchived()
    {
        // Arrange
        var job = new JobDefinition("Test", "Desc", "Asm", "Class", "Method", "user");

        // Act
        job.Archive("admin");

        // Assert
        job.Status.Should().Be(JobStatus.Archived);
    }

    [Fact]
    public void SetRetryPolicy_WhenCalled_ShouldUpdateRetryPolicy()
    {
        // Arrange
        var job = new JobDefinition("Test", "Desc", "Asm", "Class", "Method", "user");
        var retryPolicy = new RetryPolicy(3, RetryStrategy.Exponential, 5, 60);

        // Act
        job.SetRetryPolicy(retryPolicy, "admin");

        // Assert
        job.RetryPolicy.Should().NotBeNull();
        job.RetryPolicy!.MaxRetries.Should().Be(3);
        job.RetryPolicy.Strategy.Should().Be(RetryStrategy.Exponential);
    }

    [Fact]
    public void SetConcurrency_WhenCalledWithValidValue_ShouldUpdateMaxConcurrentExecutions()
    {
        // Arrange
        var job = new JobDefinition("Test", "Desc", "Asm", "Class", "Method", "user");

        // Act
        job.SetConcurrency(5, "admin");

        // Assert
        job.MaxConcurrentExecutions.Should().Be(5);
    }

    [Fact]
    public void SetConcurrency_WhenCalledWithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var job = new JobDefinition("Test", "Desc", "Asm", "Class", "Method", "user");

        // Act
        Action act = () => job.SetConcurrency(0, "admin");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void Update_WhenCalled_ShouldIncrementVersion()
    {
        // Arrange
        var job = new JobDefinition("Test", "Desc", "Asm", "Class", "Method", "user");
        var originalVersion = job.Version;

        // Act
        job.SetRetryPolicy(new RetryPolicy(3, RetryStrategy.Linear, 5), "admin");

        // Assert
        job.Version.Should().Be(originalVersion + 1);
    }
}
