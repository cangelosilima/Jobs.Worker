using System;

namespace Jobs.Worker.Application.Commands;

public record TriggerJobCommand
{
    public Guid JobDefinitionId { get; init; }
    public string? InputPayload { get; init; }
    public string TriggeredBy { get; init; } = string.Empty;
}
