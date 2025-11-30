using Jobs.Worker.Domain.Entities;

namespace Jobs.Worker.Application.Interfaces;

public interface IJobScheduleRepository
{
    Task<JobSchedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobSchedule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobSchedule>> GetSchedulesForJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobSchedule>> GetDueSchedulesAsync(DateTime currentTime, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobSchedule>> GetUpcomingSchedulesAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task AddAsync(JobSchedule schedule, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobSchedule schedule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
