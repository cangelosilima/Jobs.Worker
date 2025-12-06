namespace Jobs.Worker.Domain.Enums;

public enum ScheduleType
{
    OneTime = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4,
    MonthlyBusinessDay = 5,
    Cron = 6,
    Conditional = 7
}
