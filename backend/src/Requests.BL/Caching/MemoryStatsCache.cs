using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Requests.BL.Dtos;

namespace Requests.BL.Caching;

/// <summary>
/// <see cref="IStatsCache"/> over <see cref="IMemoryCache"/>, with absolute (not sliding) expiration.
/// Registered as a singleton; it depends only on singletons.
/// </summary>
public sealed class MemoryStatsCache : IStatsCache
{
    private const string Key = "requests:stats";

    private readonly IMemoryCache _cache;
    private readonly TimeSpan _duration;
    private readonly ILogger<MemoryStatsCache> _logger;

    public MemoryStatsCache(IMemoryCache cache, IOptions<CacheOptions> options, ILogger<MemoryStatsCache> logger)
    {
        _cache = cache;
        _duration = TimeSpan.FromSeconds(options.Value.StatsSeconds);
        _logger = logger;
    }

    public async Task<RequestStatsDto> GetOrCreateAsync(Func<Task<RequestStatsDto>> factory)
    {
        if (_cache.TryGetValue(Key, out RequestStatsDto? cached) && cached is not null)
        {
            _logger.LogDebug("Stats cache hit");
            return cached;
        }

        _logger.LogDebug("Stats cache miss");
        var stats = await factory();
        _cache.Set(Key, stats, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = _duration });
        return stats;
    }

    public void Invalidate()
    {
        _cache.Remove(Key);
        _logger.LogDebug("Stats cache invalidated");
    }
}
