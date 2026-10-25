namespace Requests.BL.Caching;

/// <summary>Bound from the "Cache" configuration section.</summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>Absolute expiration of the cached stats: they are never older than this.</summary>
    public int StatsSeconds { get; set; } = 60;
}
