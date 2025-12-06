using System;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Api
{
    // Command record for cancelling executions
    public record CancelExecutionCommand(string ExecutionId, string CancelledBy, string? Reason);

    // Command record for updating job
    public record UpdateJobCommand(Guid Id, string Name, string Description);

    // Command record for updating job status
    public record UpdateJobStatusCommand(JobStatus NewStatus, string UpdatedBy, string? Reason);

    // Make the implicit Program class accessible to integration tests
    public partial class Program { }
}
