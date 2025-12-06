using System.Threading;
using System.Threading.Tasks;
using Jobs.Worker.Domain.Entities;

namespace Jobs.Worker.Application.Interfaces;

public interface IJobExecutor
{
    Task<JobExecution> ExecuteAsync(JobDefinition job, JobExecution execution, CancellationToken cancellationToken = default);
}
