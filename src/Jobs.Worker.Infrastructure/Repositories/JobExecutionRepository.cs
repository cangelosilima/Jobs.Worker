using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobs.Worker.Infrastructure.Repositories;

public class JobExecutionRepository : IJobExecutionRepository
{
    private readonly JobSchedulerDbContext _context;

    public JobExecutionRepository(JobSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<JobExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Include(e => e.JobDefinition)
            .Include(e => e.Logs)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<JobExecution>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Include(e => e.JobDefinition)
            .OrderByDescending(e => e.QueuedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobExecution>> GetByJobIdAsync(Guid jobId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(e => e.JobDefinitionId == jobId)
            .OrderByDescending(e => e.QueuedAtUtc)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobExecution>> GetRunningExecutionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(e => e.Status == ExecutionStatus.Running || e.Status == ExecutionStatus.Queued)
            .Include(e => e.JobDefinition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobExecution>> GetFailedExecutionsTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.JobExecutions
            .Where(e => e.Status == ExecutionStatus.Failed && e.QueuedAtUtc >= today)
            .Include(e => e.JobDefinition)
            .OrderByDescending(e => e.CompletedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobExecution>> GetExecutionsByStatusAsync(ExecutionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(e => e.Status == status)
            .Include(e => e.JobDefinition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobExecution>> GetDelayedOrSkippedExecutionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(e => e.Status == ExecutionStatus.Skipped)
            .Include(e => e.JobDefinition)
            .OrderByDescending(e => e.QueuedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobExecution>> GetExecutionsExceedingExpectedDurationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(e => e.Status == ExecutionStatus.Running &&
                        e.JobDefinition.ExpectedDurationSeconds != null &&
                        e.DurationSeconds > e.JobDefinition.ExpectedDurationSeconds)
            .Include(e => e.JobDefinition)
            .ToListAsync(cancellationToken);
    }

    public async Task<JobExecution?> GetLastExecutionForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .Where(e => e.JobDefinitionId == jobId)
            .OrderByDescending(e => e.QueuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(JobExecution execution, CancellationToken cancellationToken = default)
    {
        await _context.JobExecutions.AddAsync(execution, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JobExecution execution, CancellationToken cancellationToken = default)
    {
        _context.JobExecutions.Update(execution);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetActiveExecutionCountForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobExecutions
            .CountAsync(e => e.JobDefinitionId == jobId &&
                            (e.Status == ExecutionStatus.Running || e.Status == ExecutionStatus.Queued),
                       cancellationToken);
    }
}
