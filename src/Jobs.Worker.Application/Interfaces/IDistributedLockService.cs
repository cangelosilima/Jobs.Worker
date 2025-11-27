namespace Jobs.Worker.Application.Interfaces;

public interface IDistributedLockService
{
    Task<bool> TryAcquireLockAsync(string lockKey, string ownerId, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    Task<bool> ReleaseLockAsync(string lockKey, string ownerId, CancellationToken cancellationToken = default);
    Task<bool> ExtendLockAsync(string lockKey, string ownerId, TimeSpan additionalDuration, CancellationToken cancellationToken = default);
    Task<bool> IsLockedAsync(string lockKey, CancellationToken cancellationToken = default);
}
