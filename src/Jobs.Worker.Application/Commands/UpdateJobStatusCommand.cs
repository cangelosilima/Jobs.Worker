using Jobs.Worker.Domain.Enums;
using MediatR;

namespace Jobs.Worker.Application.Commands;

public record UpdateJobStatusCommand : IRequest<Unit>
{
    public Guid JobDefinitionId { get; init; }
    public JobStatus NewStatus { get; init; }
    public string UpdatedBy { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
