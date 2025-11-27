using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Commands;

public record CreateScheduleCommand
{
    public Guid JobDefinitionId { get; init; }
    public ScheduleType ScheduleType { get; init; }
    public string? CronExpression { get; init; }
    public TimeSpan? TimeOfDay { get; init; }
    public DayOfWeekFlags? DaysOfWeek { get; init; }
    public int? DayOfMonth { get; init; }
    public int? BusinessDayOfMonth { get; init; }
    public bool AdjustToPreviousBusinessDay { get; init; }
    public DateTime? OneTimeExecutionDate { get; init; }
    public string? ConditionalExpression { get; init; }
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}
