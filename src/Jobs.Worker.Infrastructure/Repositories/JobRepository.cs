using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobs.Worker.Infrastructure.Repositories;

public class JobRepository : IJobRepository
{
    private readonly JobSchedulerDbContext _context;

    public JobRepository(JobSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<JobDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobDefinitions
            .Include(j => j.Ownership)
            .Include(j => j.Schedules)
            .Include(j => j.Parameters)
            .Include(j => j.Notifications)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<JobDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobDefinitions
            .Include(j => j.Ownership)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobDefinition>> GetActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobDefinitions
            .Where(j => j.Status == JobStatus.Active)
            .Include(j => j.Schedules)
            .Include(j => j.Parameters)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobDefinition>> GetJobsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.JobDefinitions
            .Where(j => j.Category == category)
            .Include(j => j.Ownership)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobDefinition>> GetJobsByEnvironmentAsync(DeploymentEnvironment environment, CancellationToken cancellationToken = default)
    {
        return await _context.JobDefinitions
            .Where(j => (j.AllowedEnvironments & environment) == environment)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobDefinition job, CancellationToken cancellationToken = default)
    {
        await _context.JobDefinitions.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JobDefinition job, CancellationToken cancellationToken = default)
    {
        _context.JobDefinitions.Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await GetByIdAsync(id, cancellationToken);
        if (job != null)
        {
            _context.JobDefinitions.Remove(job);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobDefinitions.AnyAsync(j => j.Id == id, cancellationToken);
    }
}
