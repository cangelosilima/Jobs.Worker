using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Interfaces;

public interface IAuditRepository
{
    Task AddAsync(JobAudit audit, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobAudit>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobAudit>> GetAuditLogForJobAsync(Guid jobId, int pageSize = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobAudit>> GetAuditLogByActionAsync(AuditAction action, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobAudit>> GetAuditLogByUserAsync(string userName, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
