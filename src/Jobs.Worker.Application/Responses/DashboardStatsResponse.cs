namespace Jobs.Worker.Application.Responses;

public record DashboardStatsResponse
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
