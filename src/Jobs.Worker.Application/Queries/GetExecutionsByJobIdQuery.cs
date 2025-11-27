using Jobs.Worker.Application.DTOs;
using MediatR;

namespace Jobs.Worker.Application.Queries;

public record GetExecutionsByJobIdQuery(Guid JobId, int PageSize = 50) : IRequest<IEnumerable<JobExecutionDto>>;
