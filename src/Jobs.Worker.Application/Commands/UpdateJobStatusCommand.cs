using System;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Commands;

public record UpdateJobStatusCommand
{
    public Guid JobDefinitionId { get; init; }
    public JobStatus NewStatus { get; init; }
    public string UpdatedBy { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
