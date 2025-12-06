using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Interfaces;

public interface IJobCircuitBreakerRepository
{
    Task<JobCircuitBreaker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JobCircuitBreaker?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobCircuitBreaker>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobCircuitBreaker>> GetByStateAsync(CircuitBreakerState state, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobCircuitBreaker>> GetOpenCircuitsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobCircuitBreaker>> GetHalfOpenCircuitsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(JobCircuitBreaker circuitBreaker, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobCircuitBreaker circuitBreaker, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
