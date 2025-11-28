namespace Jobs.Worker.Domain.ValueObjects;

public class SlaPolicy
{
    public bool IsEnabled { get; private set; }
    public int MaxDurationSeconds { get; private set; }
    public int WarningThresholdSeconds { get; private set; }
    public Guid? OnMissHandlerJobId { get; private set; }
    public bool AutoTriggerHandler { get; private set; }

    private SlaPolicy() { }

    public SlaPolicy(
        bool isEnabled,
        int maxDurationSeconds,
        int warningThresholdSeconds,
        Guid? onMissHandlerJobId = null,
        bool autoTriggerHandler = false)
    {
        if (maxDurationSeconds < 1)
            throw new ArgumentException("Max duration must be at least 1 second", nameof(maxDurationSeconds));

        if (warningThresholdSeconds < 1)
            throw new ArgumentException("Warning threshold must be at least 1 second", nameof(warningThresholdSeconds));

        IsEnabled = isEnabled;
        MaxDurationSeconds = maxDurationSeconds;
        WarningThresholdSeconds = warningThresholdSeconds;
        OnMissHandlerJobId = onMissHandlerJobId;
        AutoTriggerHandler = autoTriggerHandler;
    }

    public static SlaPolicy Disabled()
    {
        return new SlaPolicy(false, 3600, 1800);
    }

    public bool IsSlaMissed(int durationSeconds)
    {
        return IsEnabled && durationSeconds > MaxDurationSeconds;
    }

    public bool IsWarningThresholdExceeded(int durationSeconds)
    {
        return IsEnabled && durationSeconds > WarningThresholdSeconds;
    }
}
