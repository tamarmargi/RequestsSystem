using Requests.BL.Dtos;

namespace Requests.BL.Caching;

/// <summary>
/// Cache for the global request stats. Behind an interface so a distributed implementation (e.g. Redis) can
/// replace the in-memory one when the API runs on several instances.
/// </summary>
public interface IStatsCache
{
    /// <summary>The cached stats, or the result of <paramref name="factory"/> (which is then cached).</summary>
    /// <remarks>The factory is passed per call, so the cache never holds on to scoped services such as the DbContext.</remarks>
    Task<RequestStatsDto> GetOrCreateAsync(Func<Task<RequestStatsDto>> factory);

    /// <summary>Drops the cached stats. Call only after a change has been saved.</summary>
    void Invalidate();
}
