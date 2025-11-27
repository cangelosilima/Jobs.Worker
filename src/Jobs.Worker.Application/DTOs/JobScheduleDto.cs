using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.DTOs;

public record JobScheduleDto
{
    public Guid Id { get; init; }
    public Guid JobDefinitionId { get; init; }
    public ScheduleType ScheduleType { get; init; }
    public string? CronExpression { get; init; }
    public string? TimeOfDay { get; init; }
    public string? DaysOfWeek { get; init; }
    public int? DayOfMonth { get; init; }
    public int? BusinessDayOfMonth { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastExecutionUtc { get; init; }
    public DateTime? NextExecutionUtc { get; init; }
}
