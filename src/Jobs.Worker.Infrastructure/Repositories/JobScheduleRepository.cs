using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobs.Worker.Infrastructure.Repositories;

public class JobScheduleRepository : IJobScheduleRepository
{
    private readonly JobSchedulerDbContext _context;

    public JobScheduleRepository(JobSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task<JobSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobSchedules
            .Include(s => s.JobDefinition)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<JobSchedule>> GetSchedulesForJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobSchedules
            .Where(s => s.JobDefinitionId == jobId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobSchedule>> GetDueSchedulesAsync(DateTime currentTime, CancellationToken cancellationToken = default)
    {
        return await _context.JobSchedules
            .Include(s => s.JobDefinition)
            .Where(s => s.IsActive &&
                       s.NextExecutionUtc.HasValue &&
                       s.NextExecutionUtc.Value <= currentTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobSchedule>> GetUpcomingSchedulesAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _context.JobSchedules
            .Include(s => s.JobDefinition)
            .Where(s => s.IsActive &&
                       s.NextExecutionUtc.HasValue &&
                       s.NextExecutionUtc.Value >= startTime &&
                       s.NextExecutionUtc.Value <= endTime)
            .OrderBy(s => s.NextExecutionUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.JobSchedules.AddAsync(schedule, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JobSchedule schedule, CancellationToken cancellationToken = default)
    {
        _context.JobSchedules.Update(schedule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schedule = await GetByIdAsync(id, cancellationToken);
        if (schedule != null)
        {
            _context.JobSchedules.Remove(schedule);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
