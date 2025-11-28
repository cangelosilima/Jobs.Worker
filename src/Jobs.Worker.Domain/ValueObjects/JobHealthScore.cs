namespace Jobs.Worker.Domain.ValueObjects;

public class JobHealthScore
{
    public double Score { get; private set; } // 0-100
    public double SuccessRate { get; private set; }
    public double SlaComplianceRate { get; private set; }
    public int TotalExecutionsLast7Days { get; private set; }
    public int FailedExecutionsLast7Days { get; private set; }
    public double AverageDurationSeconds { get; private set; }
    public string Trend { get; private set; } = "stable"; // improving, stable, declining
    public DateTime CalculatedAtUtc { get; private set; }

    private JobHealthScore() { }

    public JobHealthScore(
        double successRate,
        double slaComplianceRate,
        int totalExecutions,
        int failedExecutions,
        double averageDuration,
        string trend)
    {
        SuccessRate = successRate;
        SlaComplianceRate = slaComplianceRate;
        TotalExecutionsLast7Days = totalExecutions;
        FailedExecutionsLast7Days = failedExecutions;
        AverageDurationSeconds = averageDuration;
        Trend = trend;
        CalculatedAtUtc = DateTime.UtcNow;

        // Calculate overall health score (weighted average)
        Score = (SuccessRate * 0.5) + (SlaComplianceRate * 0.3) + (GetTrendScore() * 0.2);
    }

    private double GetTrendScore()
    {
        return Trend switch
        {
            "improving" => 100,
            "stable" => 75,
            "declining" => 25,
            _ => 50
        };
    }

    public string GetHealthLevel()
    {
        return Score switch
        {
            >= 90 => "Excellent",
            >= 75 => "Good",
            >= 60 => "Fair",
            >= 40 => "Poor",
            _ => "Critical"
        };
    }
}
