using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jobs.Worker.Application.Handlers;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Application.Queries;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Moq;
using Xunit;

namespace Jobs.Worker.Application.Tests.Handlers;

public class GetDashboardStatsQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepoMock;
    private readonly Mock<IJobExecutionRepository> _execRepoMock;
    private readonly GetDashboardStatsQueryHandler _handler;

    public GetDashboardStatsQueryHandlerTests()
    {
        _jobRepoMock = new Mock<IJobRepository>();
        _execRepoMock = new Mock<IJobExecutionRepository>();
        _handler = new GetDashboardStatsQueryHandler(_jobRepoMock.Object, _execRepoMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnCorrectStats_WhenJobsExist()
    {
        // Arrange
        var activeJob = new JobDefinition("Job1", "Desc", "Category", DeploymentEnvironment.All, ExecutionMode.InProcess, "Asm.dll", "Namespace.Type", 600, "user");
        activeJob.Activate("admin");

        var disabledJob = new JobDefinition("Job2", "Desc", "Category", DeploymentEnvironment.All, ExecutionMode.InProcess, "Asm.dll", "Namespace.Type", 600, "user");
        disabledJob.Activate("admin");
        disabledJob.Disable("admin", "test");

        var jobs = new List<JobDefinition> { activeJob, disabledJob };

        var runningExec = CreateExecution(activeJob.Id, ExecutionStatus.Running);
        var succeededExec = CreateExecution(activeJob.Id, ExecutionStatus.Succeeded);
        var failedExec = CreateExecution(activeJob.Id, ExecutionStatus.Failed);

        _jobRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        _execRepoMock.Setup(x => x.GetRunningExecutionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution> { runningExec });

        _execRepoMock.Setup(x => x.GetFailedExecutionsTodayAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution> { failedExec });

        _execRepoMock.Setup(x => x.GetDelayedOrSkippedExecutionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution>());

        _execRepoMock.Setup(x => x.GetExecutionsExceedingExpectedDurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution>());

        var query = new GetDashboardStatsQuery();

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalJobs.Should().Be(2);
        result.ActiveJobs.Should().Be(1);
        result.DisabledJobs.Should().Be(1);
        result.RunningExecutions.Should().Be(1);
        result.FailedToday.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ShouldCalculateSuccessRate_Correctly()
    {
        // Arrange
        var job = new JobDefinition("Job1", "Desc", "Category", DeploymentEnvironment.All, ExecutionMode.InProcess, "Asm.dll", "Namespace.Type", 600, "user");

        _jobRepoMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobDefinition> { job });

        _execRepoMock.Setup(x => x.GetRunningExecutionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution>());

        _execRepoMock.Setup(x => x.GetFailedExecutionsTodayAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution> { CreateExecution(job.Id, ExecutionStatus.Failed) });

        _execRepoMock.Setup(x => x.GetDelayedOrSkippedExecutionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution>());

        _execRepoMock.Setup(x => x.GetExecutionsExceedingExpectedDurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobExecution>());

        var query = new GetDashboardStatsQuery();

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.FailedToday.Should().Be(1);
    }

    private JobExecution CreateExecution(Guid jobId, ExecutionStatus status)
    {
        var scheduleId = Guid.NewGuid();
        var context = Jobs.Worker.Domain.ValueObjects.ExecutionContext.Create("test-host");
        var execution = new JobExecution(jobId, scheduleId, context, null, "test-user", false, 3);

        if (status == ExecutionStatus.Running)
        {
            execution.Start();
        }
        else if (status == ExecutionStatus.Succeeded)
        {
            execution.Start();
            execution.Complete("Success");
        }
        else if (status == ExecutionStatus.Failed)
        {
            execution.Start();
            execution.Fail("Error message");
        }

        return execution;
    }
}
