using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobs.Worker.Infrastructure.Repositories;

public class JobCircuitBreakerRepository : IJobCircuitBreakerRepository
{
    private readonly JobSchedulerDbContext _context;

    public JobCircuitBreakerRepository(JobSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<JobCircuitBreaker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobCircuitBreakers
            .Include(cb => cb.JobDefinition)
            .FirstOrDefaultAsync(cb => cb.Id == id, cancellationToken);
    }

    public async Task<JobCircuitBreaker?> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobCircuitBreakers
            .Include(cb => cb.JobDefinition)
            .FirstOrDefaultAsync(cb => cb.JobDefinitionId == jobId, cancellationToken);
    }

    public async Task<IEnumerable<JobCircuitBreaker>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobCircuitBreakers
            .Include(cb => cb.JobDefinition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobCircuitBreaker>> GetByStateAsync(CircuitBreakerState state, CancellationToken cancellationToken = default)
    {
        return await _context.JobCircuitBreakers
            .Where(cb => cb.State == state)
            .Include(cb => cb.JobDefinition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobCircuitBreaker>> GetOpenCircuitsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStateAsync(CircuitBreakerState.Open, cancellationToken);
    }

    public async Task<IEnumerable<JobCircuitBreaker>> GetHalfOpenCircuitsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStateAsync(CircuitBreakerState.HalfOpen, cancellationToken);
    }

    public async Task AddAsync(JobCircuitBreaker circuitBreaker, CancellationToken cancellationToken = default)
    {
        await _context.JobCircuitBreakers.AddAsync(circuitBreaker, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JobCircuitBreaker circuitBreaker, CancellationToken cancellationToken = default)
    {
        _context.JobCircuitBreakers.Update(circuitBreaker);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var circuitBreaker = await GetByIdAsync(id, cancellationToken);
        if (circuitBreaker != null)
        {
            _context.JobCircuitBreakers.Remove(circuitBreaker);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
