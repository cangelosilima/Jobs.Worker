namespace Jobs.Worker.Application.DTOs;

public record DashboardStatsDto
{
    public int TotalJobs { get; init; }
    public int ActiveJobs { get; init; }
    public int DisabledJobs { get; init; }
    public int RunningExecutions { get; init; }
    public int FailedToday { get; init; }
    public int SucceededToday { get; init; }
    public int DelayedOrSkipped { get; init; }
    public int ExceedingExpectedDuration { get; init; }
    public double AverageExecutionTimeSeconds { get; init; }
    public double SuccessRatePercentage { get; init; }
}
