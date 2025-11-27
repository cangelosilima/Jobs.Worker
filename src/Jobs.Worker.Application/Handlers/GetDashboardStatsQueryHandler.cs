using Jobs.Worker.Application.DTOs;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Application.Queries;
using Jobs.Worker.Domain.Enums;
using MediatR;

namespace Jobs.Worker.Application.Handlers;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobExecutionRepository _executionRepository;

    public GetDashboardStatsQueryHandler(
        IJobRepository jobRepository,
        IJobExecutionRepository executionRepository)
    {
        _jobRepository = jobRepository;
        _executionRepository = executionRepository;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var allJobs = await _jobRepository.GetAllAsync(cancellationToken);
        var runningExecutions = await _executionRepository.GetRunningExecutionsAsync(cancellationToken);
        var failedToday = await _executionRepository.GetFailedExecutionsTodayAsync(cancellationToken);
        var delayedOrSkipped = await _executionRepository.GetDelayedOrSkippedExecutionsAsync(cancellationToken);
        var exceedingDuration = await _executionRepository.GetExecutionsExceedingExpectedDurationAsync(cancellationToken);

        var totalJobs = allJobs.Count();
        var activeJobs = allJobs.Count(j => j.Status == JobStatus.Active);
        var disabledJobs = allJobs.Count(j => j.Status == JobStatus.Disabled);

        // Calculate success rate for today
        var succeededToday = (await _executionRepository.GetExecutionsByStatusAsync(ExecutionStatus.Succeeded, cancellationToken))
            .Where(e => e.CompletedAtUtc >= DateTime.UtcNow.Date)
            .Count();

        var totalToday = succeededToday + failedToday.Count();
        var successRate = totalToday > 0 ? (double)succeededToday / totalToday * 100 : 100.0;

        return new DashboardStatsDto
        {
            TotalJobs = totalJobs,
            ActiveJobs = activeJobs,
            DisabledJobs = disabledJobs,
            RunningExecutions = runningExecutions.Count(),
            FailedToday = failedToday.Count(),
            SucceededToday = succeededToday,
            DelayedOrSkipped = delayedOrSkipped.Count(),
            ExceedingExpectedDuration = exceedingDuration.Count(),
            AverageExecutionTimeSeconds = runningExecutions.Any() ? runningExecutions.Average(e => e.DurationSeconds) : 0,
            SuccessRatePercentage = Math.Round(successRate, 2)
        };
    }
}
