using Cronos;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Infrastructure.Services;

public class ScheduleCalculator : IScheduleCalculator
{
    private static readonly HashSet<DateTime> Holidays = new();

    public DateTime? CalculateNextExecution(JobSchedule schedule, DateTime fromTime)
    {
        if (!schedule.IsWithinActiveWindow(fromTime))
            return null;

        return schedule.Rule.Type switch
        {
            ScheduleType.Daily => CalculateNextDaily(schedule, fromTime),
            ScheduleType.Weekly => CalculateNextWeekly(schedule, fromTime),
            ScheduleType.Monthly => CalculateNextMonthly(schedule, fromTime),
            ScheduleType.MonthlyBusinessDay => CalculateNextMonthlyBusinessDay(schedule, fromTime),
            ScheduleType.Cron => CalculateNextCron(schedule, fromTime),
            ScheduleType.OneTime => schedule.Rule.OneTimeExecutionDate > fromTime ? schedule.Rule.OneTimeExecutionDate : null,
            ScheduleType.Conditional => CalculateNextConditional(schedule, fromTime),
            _ => null
        };
    }

    private DateTime? CalculateNextDaily(JobSchedule schedule, DateTime fromTime)
    {
        if (!schedule.Rule.TimeOfDay.HasValue)
            return null;

        var timeOfDay = schedule.Rule.TimeOfDay.Value;
        var nextExecution = fromTime.Date.Add(timeOfDay);

        if (nextExecution <= fromTime)
            nextExecution = nextExecution.AddDays(1);

        return nextExecution;
    }

    private DateTime? CalculateNextWeekly(JobSchedule schedule, DateTime fromTime)
    {
        if (!schedule.Rule.DaysOfWeek.HasValue || !schedule.Rule.TimeOfDay.HasValue)
            return null;

        var daysOfWeek = schedule.Rule.DaysOfWeek.Value;
        var timeOfDay = schedule.Rule.TimeOfDay.Value;
        var currentDate = fromTime.Date;

        for (int i = 0; i < 7; i++)
        {
            var checkDate = currentDate.AddDays(i);
            var dayFlag = ConvertToDayOfWeekFlag(checkDate.DayOfWeek);

            if (daysOfWeek.HasFlag(dayFlag))
            {
                var nextExecution = checkDate.Add(timeOfDay);
                if (nextExecution > fromTime)
                    return nextExecution;
            }
        }

        return null;
    }

    private DateTime? CalculateNextMonthly(JobSchedule schedule, DateTime fromTime)
    {
        if (!schedule.Rule.DayOfMonth.HasValue || !schedule.Rule.TimeOfDay.HasValue)
            return null;

        var dayOfMonth = schedule.Rule.DayOfMonth.Value;
        var timeOfDay = schedule.Rule.TimeOfDay.Value;
        var currentDate = fromTime.Date;

        var nextMonth = currentDate.Month;
        var nextYear = currentDate.Year;

        if (currentDate.Day >= dayOfMonth && currentDate.Add(timeOfDay) <= fromTime)
        {
            nextMonth++;
            if (nextMonth > 12)
            {
                nextMonth = 1;
                nextYear++;
            }
        }

        var daysInMonth = DateTime.DaysInMonth(nextYear, nextMonth);
        var actualDay = Math.Min(dayOfMonth, daysInMonth);

        return new DateTime(nextYear, nextMonth, actualDay).Add(timeOfDay);
    }

    private DateTime? CalculateNextMonthlyBusinessDay(JobSchedule schedule, DateTime fromTime)
    {
        if (!schedule.Rule.BusinessDayOfMonth.HasValue || !schedule.Rule.TimeOfDay.HasValue)
            return null;

        var businessDay = schedule.Rule.BusinessDayOfMonth.Value;
        var timeOfDay = schedule.Rule.TimeOfDay.Value;
        var currentDate = fromTime.Date;

        var targetDate = GetNthBusinessDayOfMonth(currentDate.Year, currentDate.Month, businessDay);

        if (targetDate.Add(timeOfDay) <= fromTime)
        {
            var nextMonth = currentDate.AddMonths(1);
            targetDate = GetNthBusinessDayOfMonth(nextMonth.Year, nextMonth.Month, businessDay);
        }

        if (schedule.Rule.AdjustToPreviousBusinessDay && !IsBusinessDay(targetDate))
        {
            targetDate = AdjustToPreviousBusinessDay(targetDate);
        }

        return targetDate.Add(timeOfDay);
    }

    private DateTime? CalculateNextCron(JobSchedule schedule, DateTime fromTime)
    {
        if (string.IsNullOrEmpty(schedule.Rule.CronExpression))
            return null;

        try
        {
            var cronExpression = CronExpression.Parse(schedule.Rule.CronExpression);
            return cronExpression.GetNextOccurrence(fromTime, TimeZoneInfo.Utc);
        }
        catch
        {
            return null;
        }
    }

    private DateTime? CalculateNextConditional(JobSchedule schedule, DateTime fromTime)
    {
        // Simplified implementation - in production, this would evaluate the conditional expression
        return schedule.Rule.TimeOfDay.HasValue
            ? fromTime.Date.AddDays(1).Add(schedule.Rule.TimeOfDay.Value)
            : null;
    }

    public bool IsBusinessDay(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday &&
               date.DayOfWeek != DayOfWeek.Sunday &&
               !Holidays.Contains(date.Date);
    }

    public DateTime GetNthBusinessDayOfMonth(int year, int month, int businessDayNumber)
    {
        var date = new DateTime(year, month, 1);
        var businessDaysCount = 0;

        while (businessDaysCount < businessDayNumber)
        {
            if (IsBusinessDay(date))
                businessDaysCount++;

            if (businessDaysCount < businessDayNumber)
                date = date.AddDays(1);
        }

        return date;
    }

    public DateTime AdjustToPreviousBusinessDay(DateTime date)
    {
        var adjustedDate = date;
        while (!IsBusinessDay(adjustedDate))
        {
            adjustedDate = adjustedDate.AddDays(-1);
        }
        return adjustedDate;
    }

    private static DayOfWeekFlags ConvertToDayOfWeekFlag(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => DayOfWeekFlags.Sunday,
            DayOfWeek.Monday => DayOfWeekFlags.Monday,
            DayOfWeek.Tuesday => DayOfWeekFlags.Tuesday,
            DayOfWeek.Wednesday => DayOfWeekFlags.Wednesday,
            DayOfWeek.Thursday => DayOfWeekFlags.Thursday,
            DayOfWeek.Friday => DayOfWeekFlags.Friday,
            DayOfWeek.Saturday => DayOfWeekFlags.Saturday,
            _ => DayOfWeekFlags.None
        };
    }
}
