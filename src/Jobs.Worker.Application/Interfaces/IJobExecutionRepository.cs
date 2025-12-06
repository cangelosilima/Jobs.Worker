using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Interfaces;

public interface IJobExecutionRepository
{
    Task<JobExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobExecution>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobExecution>> GetByJobIdAsync(Guid jobId, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobExecution>> GetFailedExecutionsTodayAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobExecution>> GetDelayedOrSkippedExecutionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobExecution>> GetExecutionsExceedingExpectedDurationAsync(CancellationToken cancellationToken = default);
    Task<JobExecution?> GetLastExecutionForJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task AddAsync(JobExecution execution, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobExecution execution, CancellationToken cancellationToken = default);
    Task<int> GetActiveExecutionCountForJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
