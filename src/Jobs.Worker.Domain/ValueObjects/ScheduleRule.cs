using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.ValueObjects;

public class ScheduleRule
{
    public ScheduleType Type { get; private set; }
    public string? CronExpression { get; private set; }
    public TimeSpan? TimeOfDay { get; private set; }
    public DayOfWeekFlags? DaysOfWeek { get; private set; }
    public int? DayOfMonth { get; private set; }
    public int? BusinessDayOfMonth { get; private set; }
    public bool AdjustToPreviousBusinessDay { get; private set; }
    public DateTime? OneTimeExecutionDate { get; private set; }
    public string? ConditionalExpression { get; private set; }

    private ScheduleRule() { }

    public static ScheduleRule CreateDaily(TimeSpan timeOfDay)
    {
        return new ScheduleRule
        {
            Type = ScheduleType.Daily,
            TimeOfDay = timeOfDay
        };
    }

    public static ScheduleRule CreateWeekly(DayOfWeekFlags daysOfWeek, TimeSpan timeOfDay)
    {
        return new ScheduleRule
        {
            Type = ScheduleType.Weekly,
            DaysOfWeek = daysOfWeek,
            TimeOfDay = timeOfDay
        };
    }

    public static ScheduleRule CreateMonthly(int dayOfMonth, TimeSpan timeOfDay)
    {
        if (dayOfMonth < 1 || dayOfMonth > 31)
            throw new ArgumentException("Day of month must be between 1 and 31", nameof(dayOfMonth));

        return new ScheduleRule
        {
            Type = ScheduleType.Monthly,
            DayOfMonth = dayOfMonth,
            TimeOfDay = timeOfDay
        };
    }

    public static ScheduleRule CreateMonthlyBusinessDay(int businessDayOfMonth, TimeSpan timeOfDay, bool adjustToPrevious = false)
    {
        if (businessDayOfMonth < 1 || businessDayOfMonth > 31)
            throw new ArgumentException("Business day of month must be between 1 and 31", nameof(businessDayOfMonth));

        return new ScheduleRule
        {
            Type = ScheduleType.MonthlyBusinessDay,
            BusinessDayOfMonth = businessDayOfMonth,
            TimeOfDay = timeOfDay,
            AdjustToPreviousBusinessDay = adjustToPrevious
        };
    }

    public static ScheduleRule CreateCron(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new ArgumentException("Cron expression cannot be empty", nameof(cronExpression));

        return new ScheduleRule
        {
            Type = ScheduleType.Cron,
            CronExpression = cronExpression
        };
    }

    public static ScheduleRule CreateOneTime(DateTime executionDate)
    {
        return new ScheduleRule
        {
            Type = ScheduleType.OneTime,
            OneTimeExecutionDate = executionDate
        };
    }

    public static ScheduleRule CreateConditional(string expression, TimeSpan timeOfDay)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Conditional expression cannot be empty", nameof(expression));

        return new ScheduleRule
        {
            Type = ScheduleType.Conditional,
            ConditionalExpression = expression,
            TimeOfDay = timeOfDay
        };
    }
}
