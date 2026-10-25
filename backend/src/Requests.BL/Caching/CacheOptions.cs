namespace Requests.BL.Caching;

/// <summary>Bound from the "Cache" configuration section.</summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Absolute expiration of the cached stats: they are never older than this. 0 disables the cache
    /// (used to measure the uncached query, e.g. with the environment variable Cache__StatsSeconds=0).
    /// </summary>
    public int StatsSeconds { get; set; } = 60;
}
