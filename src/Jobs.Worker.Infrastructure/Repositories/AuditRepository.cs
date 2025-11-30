using Jobs.Worker.Application.Interfaces;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;
using Jobs.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Jobs.Worker.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly JobSchedulerDbContext _context;

    public AuditRepository(JobSchedulerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(JobAudit audit, CancellationToken cancellationToken = default)
    {
        await _context.JobAudits.AddAsync(audit, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobAudit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.JobAudits
            .OrderByDescending(a => a.PerformedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobAudit>> GetAuditLogForJobAsync(Guid jobId, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        return await _context.JobAudits
            .Where(a => a.JobDefinitionId == jobId)
            .OrderByDescending(a => a.PerformedAtUtc)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobAudit>> GetAuditLogByActionAsync(AuditAction action, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.JobAudits.Where(a => a.Action == action);

        if (from.HasValue)
            query = query.Where(a => a.PerformedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.PerformedAtUtc <= to.Value);

        return await query
            .OrderByDescending(a => a.PerformedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobAudit>> GetAuditLogByUserAsync(string userName, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _context.JobAudits.Where(a => a.PerformedBy == userName);

        if (from.HasValue)
            query = query.Where(a => a.PerformedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.PerformedAtUtc <= to.Value);

        return await query
            .OrderByDescending(a => a.PerformedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
