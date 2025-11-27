using Jobs.Worker.Domain.Entities;

namespace Jobs.Worker.Application.Interfaces;

public interface IScheduleCalculator
{
    DateTime? CalculateNextExecution(JobSchedule schedule, DateTime fromTime);
    bool IsBusinessDay(DateTime date);
    DateTime GetNthBusinessDayOfMonth(int year, int month, int businessDayNumber);
    DateTime AdjustToPreviousBusinessDay(DateTime date);
}
