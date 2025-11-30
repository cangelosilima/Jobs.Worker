using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Interfaces;

public interface IJobRepository
{
    Task<JobDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobDefinition>> GetActiveJobsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobDefinition>> GetJobsByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobDefinition>> GetJobsByEnvironmentAsync(DeploymentEnvironment environment, CancellationToken cancellationToken = default);
    Task AddAsync(JobDefinition job, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobDefinition job, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddOwnershipAsync(JobOwnership ownership, CancellationToken cancellationToken = default);
}
