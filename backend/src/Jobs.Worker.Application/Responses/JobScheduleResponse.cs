using System;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Responses;

public record JobScheduleResponse
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
