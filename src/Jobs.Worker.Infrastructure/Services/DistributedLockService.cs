using Jobs.Worker.Application.Interfaces;
using StackExchange.Redis;

namespace Jobs.Worker.Infrastructure.Services;

public class DistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer? _redis;

    public DistributedLockService(IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
    }

    public async Task<bool> TryAcquireLockAsync(string lockKey, string ownerId, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        if (_redis == null)
            return true; // Fallback to single-instance mode

        var db = _redis.GetDatabase();
        return await db.StringSetAsync(lockKey, ownerId, lockDuration, When.NotExists);
    }

    public async Task<bool> ReleaseLockAsync(string lockKey, string ownerId, CancellationToken cancellationToken = default)
    {
        if (_redis == null)
            return true;

        var db = _redis.GetDatabase();
        var currentOwner = await db.StringGetAsync(lockKey);

        if (currentOwner == ownerId)
        {
            return await db.KeyDeleteAsync(lockKey);
        }

        return false;
    }

    public async Task<bool> ExtendLockAsync(string lockKey, string ownerId, TimeSpan additionalDuration, CancellationToken cancellationToken = default)
    {
        if (_redis == null)
            return true;

        var db = _redis.GetDatabase();
        var currentOwner = await db.StringGetAsync(lockKey);

        if (currentOwner == ownerId)
        {
            return await db.KeyExpireAsync(lockKey, additionalDuration);
        }

        return false;
    }

    public async Task<bool> IsLockedAsync(string lockKey, CancellationToken cancellationToken = default)
    {
        if (_redis == null)
            return false;

        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(lockKey);
    }
}
