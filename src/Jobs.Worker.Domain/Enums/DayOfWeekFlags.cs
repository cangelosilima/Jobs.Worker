namespace Jobs.Worker.Domain.Enums;

[Flags]
public enum DayOfWeekFlags
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekends = Saturday | Sunday,
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}
