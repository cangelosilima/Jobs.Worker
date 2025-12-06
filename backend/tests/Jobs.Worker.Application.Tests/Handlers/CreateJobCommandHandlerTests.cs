using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobs.Worker.Application.Commands;
using Jobs.Worker.Application.Handlers;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Moq;
using Xunit;

namespace Jobs.Worker.Application.Tests.Handlers;

public class CreateJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepoMock;
    private readonly Mock<IAuditRepository> _auditRepoMock;
    private readonly CreateJobCommandHandler _handler;

    public CreateJobCommandHandlerTests()
    {
        _jobRepoMock = new Mock<IJobRepository>();
        _auditRepoMock = new Mock<IAuditRepository>();
        _handler = new CreateJobCommandHandler(_jobRepoMock.Object, _auditRepoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreateJobAndReturnId()
    {
        // Arrange
        var command = new CreateJobCommand
        {
            Name = "Test Job",
            Description = "Test Description",
            Category = "Testing",
            AllowedEnvironments = DeploymentEnvironment.All,
            ExecutionMode = ExecutionMode.InProcess,
            ExecutionAssembly = "Test.Assembly.dll",
            ExecutionTypeName = "Test.Namespace.JobType",
            TimeoutSeconds = 3600,
            MaxRetries = 3,
            RetryStrategy = RetryStrategy.Exponential,
            BaseDelaySeconds = 30,
            MaxConcurrentExecutions = 1,
            OwnerName = "Test Owner",
            OwnerEmail = "owner@test.com",
            TeamName = "Test Team",
            CreatedBy = "test-user"
        };

        _jobRepoMock.Setup(x => x.AddAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _auditRepoMock.Setup(x => x.AddAsync(It.IsAny<JobAudit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var jobId = await _handler.HandleAsync(command);

        // Assert
        jobId.Should().NotBeEmpty();
        _jobRepoMock.Verify(x => x.AddAsync(
            It.Is<JobDefinition>(j =>
                j.Name == command.Name &&
                j.Description == command.Description &&
                j.Category == command.Category &&
                j.ExecutionAssembly == command.ExecutionAssembly &&
                j.ExecutionTypeName == command.ExecutionTypeName &&
                j.TimeoutSeconds == command.TimeoutSeconds &&
                j.MaxConcurrentExecutions == command.MaxConcurrentExecutions
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _auditRepoMock.Verify(x => x.AddAsync(
            It.IsAny<JobAudit>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExecutionCommand_ShouldSetExecutionCommandOnJob()
    {
        // Arrange
        var command = new CreateJobCommand
        {
            Name = "Docker Job",
            Description = "Test Description",
            Category = "Testing",
            AllowedEnvironments = DeploymentEnvironment.Production,
            ExecutionMode = ExecutionMode.Container,
            ExecutionAssembly = "Test.Assembly.dll",
            ExecutionTypeName = "Test.Namespace.JobType",
            ExecutionCommand = "dotnet run",
            ContainerImage = "mcr.microsoft.com/dotnet/sdk:8.0",
            TimeoutSeconds = 3600,
            MaxRetries = 3,
            RetryStrategy = RetryStrategy.Linear,
            BaseDelaySeconds = 10,
            MaxConcurrentExecutions = 5,
            OwnerName = "Test Owner",
            OwnerEmail = "owner@test.com",
            TeamName = "Test Team",
            CreatedBy = "test-user"
        };

        JobDefinition? capturedJob = null;
        _jobRepoMock.Setup(x => x.AddAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<JobDefinition, CancellationToken>((job, _) => capturedJob = job)
            .Returns(Task.CompletedTask);

        _auditRepoMock.Setup(x => x.AddAsync(It.IsAny<JobAudit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var jobId = await _handler.HandleAsync(command);

        // Assert
        capturedJob.Should().NotBeNull();
        capturedJob!.ExecutionCommand.Should().Be("dotnet run");
        capturedJob.ContainerImage.Should().Be("mcr.microsoft.com/dotnet/sdk:8.0");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetRetryPolicyOnJob()
    {
        // Arrange
        var command = new CreateJobCommand
        {
            Name = "Test Job",
            Description = "Test Description",
            Category = "Testing",
            AllowedEnvironments = DeploymentEnvironment.Development,
            ExecutionMode = ExecutionMode.InProcess,
            ExecutionAssembly = "Test.Assembly.dll",
            ExecutionTypeName = "Test.Namespace.JobType",
            TimeoutSeconds = 1800,
            MaxRetries = 5,
            RetryStrategy = RetryStrategy.ExponentialWithJitter,
            BaseDelaySeconds = 60,
            MaxConcurrentExecutions = 1,
            OwnerName = "Test Owner",
            OwnerEmail = "owner@test.com",
            TeamName = "Test Team",
            CreatedBy = "test-user"
        };

        JobDefinition? capturedJob = null;
        _jobRepoMock.Setup(x => x.AddAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<JobDefinition, CancellationToken>((job, _) => capturedJob = job)
            .Returns(Task.CompletedTask);

        _auditRepoMock.Setup(x => x.AddAsync(It.IsAny<JobAudit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var jobId = await _handler.HandleAsync(command);

        // Assert
        capturedJob.Should().NotBeNull();
        capturedJob!.RetryPolicy.Should().NotBeNull();
        capturedJob.RetryPolicy.MaxRetries.Should().Be(5);
        capturedJob.RetryPolicy.Strategy.Should().Be(RetryStrategy.ExponentialWithJitter);
        capturedJob.RetryPolicy.BaseDelaySeconds.Should().Be(60);
    }

    [Fact]
    public async Task HandleAsync_WithoutExecutionCommand_ShouldNotSetExecutionCommand()
    {
        // Arrange
        var command = new CreateJobCommand
        {
            Name = "Test Job",
            Description = "Test Description",
            Category = "Testing",
            AllowedEnvironments = DeploymentEnvironment.All,
            ExecutionMode = ExecutionMode.InProcess,
            ExecutionAssembly = "Test.Assembly.dll",
            ExecutionTypeName = "Test.Namespace.JobType",
            TimeoutSeconds = 3600,
            MaxRetries = 3,
            RetryStrategy = RetryStrategy.Linear,
            BaseDelaySeconds = 10,
            MaxConcurrentExecutions = 1,
            OwnerName = "Test Owner",
            OwnerEmail = "owner@test.com",
            TeamName = "Test Team",
            CreatedBy = "test-user"
        };

        JobDefinition? capturedJob = null;
        _jobRepoMock.Setup(x => x.AddAsync(It.IsAny<JobDefinition>(), It.IsAny<CancellationToken>()))
            .Callback<JobDefinition, CancellationToken>((job, _) => capturedJob = job)
            .Returns(Task.CompletedTask);

        _auditRepoMock.Setup(x => x.AddAsync(It.IsAny<JobAudit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var jobId = await _handler.HandleAsync(command);

        // Assert
        capturedJob.Should().NotBeNull();
        capturedJob!.ExecutionCommand.Should().BeNull();
        capturedJob.ContainerImage.Should().BeNull();
    }
}
