using System;

namespace Jobs.Worker.Application.Queries;

public record GetExecutionsByJobIdQuery(Guid JobId, int PageSize = 50);
