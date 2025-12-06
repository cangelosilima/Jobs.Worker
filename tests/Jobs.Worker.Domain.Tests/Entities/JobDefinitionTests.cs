using System;
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
            "TestCategory",
            DeploymentEnvironment.All,
            ExecutionMode.InProcess,
            "TestAssembly.dll",
            "TestNamespace.TestClass",
            3600,
            "testuser"
        );

        // Assert
        job.Name.Should().Be("TestJob");
        job.Description.Should().Be("Test Description");
        job.Category.Should().Be("TestCategory");
        job.ExecutionAssembly.Should().Be("TestAssembly.dll");
        job.ExecutionTypeName.Should().Be("TestNamespace.TestClass");
        job.ExecutionMode.Should().Be(ExecutionMode.InProcess);
        job.TimeoutSeconds.Should().Be(3600);
        job.Status.Should().Be(JobStatus.Draft);
        job.CreatedBy.Should().Be("testuser");
        job.Version.Should().Be(1);
        job.MaxConcurrentExecutions.Should().Be(1);
        job.AllowManualTrigger.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenCalled_ShouldSetStatusToActive()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );

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
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );
        job.Activate("admin");

        // Act
        job.Disable("admin", "No longer needed");

        // Assert
        job.Status.Should().Be(JobStatus.Disabled);
        job.DisabledReason.Should().Be("No longer needed");
        job.DisabledBy.Should().Be("admin");
    }

    [Fact]
    public void Archive_WhenCalled_ShouldSetStatusToArchived()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );

        // Act
        job.Archive("admin");

        // Assert
        job.Status.Should().Be(JobStatus.Archived);
        job.UpdatedBy.Should().Be("admin");
    }

    [Fact]
    public void SetRetryPolicy_WhenCalled_ShouldUpdateRetryPolicy()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );
        var retryPolicy = new RetryPolicy(3, RetryStrategy.Exponential, 5, 60);

        // Act
        job.SetRetryPolicy(retryPolicy);

        // Assert
        job.RetryPolicy.Should().NotBeNull();
        job.RetryPolicy.MaxRetries.Should().Be(3);
        job.RetryPolicy.Strategy.Should().Be(RetryStrategy.Exponential);
    }

    [Fact]
    public void SetConcurrency_WhenCalledWithValidValue_ShouldUpdateMaxConcurrentExecutions()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );

        // Act
        job.SetConcurrency(5);

        // Assert
        job.MaxConcurrentExecutions.Should().Be(5);
    }

    [Fact]
    public void SetConcurrency_WhenCalledWithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );

        // Act
        Action act = () => job.SetConcurrency(0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be at least 1*");
    }

    [Fact]
    public void UpdateDetails_WhenCalled_ShouldIncrementVersion()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );
        var originalVersion = job.Version;

        // Act
        job.UpdateDetails("NewName", "NewDesc", "NewCategory", "admin");

        // Assert
        job.Version.Should().Be(originalVersion + 1);
        job.Name.Should().Be("NewName");
        job.Description.Should().Be("NewDesc");
        job.Category.Should().Be("NewCategory");
    }

    [Fact]
    public void CanExecuteInEnvironment_WithMatchingEnvironment_ShouldReturnTrue()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development | DeploymentEnvironment.Homologation,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );

        // Act & Assert
        job.CanExecuteInEnvironment(DeploymentEnvironment.Development).Should().BeTrue();
        job.CanExecuteInEnvironment(DeploymentEnvironment.Homologation).Should().BeTrue();
        job.CanExecuteInEnvironment(DeploymentEnvironment.Production).Should().BeFalse();
    }

    [Fact]
    public void SetTimeout_WithValidValue_ShouldUpdateTimeout()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );

        // Act
        job.SetTimeout(1200);

        // Assert
        job.TimeoutSeconds.Should().Be(1200);
    }

    [Fact]
    public void SetTimeout_WithZero_ShouldThrowArgumentException()
    {
        // Arrange
        var job = new JobDefinition(
            "Test",
            "Desc",
            "Category",
            DeploymentEnvironment.Development,
            ExecutionMode.InProcess,
            "Asm.dll",
            "Type",
            600,
            "user"
        );

        // Act
        Action act = () => job.SetTimeout(0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be at least 1 second*");
    }
}
