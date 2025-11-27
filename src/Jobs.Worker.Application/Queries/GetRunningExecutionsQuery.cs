using Jobs.Worker.Application.DTOs;
using MediatR;

namespace Jobs.Worker.Application.Queries;

public record GetRunningExecutionsQuery : IRequest<IEnumerable<JobExecutionDto>>;
