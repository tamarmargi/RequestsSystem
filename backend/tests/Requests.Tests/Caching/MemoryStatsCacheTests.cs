using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Requests.BL.Caching;
using Requests.BL.Dtos;

namespace Requests.Tests.Caching;

/// <summary>A real MemoryCache with a controllable clock, so expiration is tested without waiting.</summary>
public sealed class MemoryStatsCacheTests : IDisposable
{
    private const int StatsSeconds = 60;

    private readonly FakeClock _clock = new();
    private readonly MemoryCache _memoryCache;
    private readonly MemoryStatsCache _cache;
    private int _factoryCalls;

    public MemoryStatsCacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions { Clock = _clock });
        _cache = new MemoryStatsCache(
            _memoryCache,
            Options.Create(new CacheOptions { StatsSeconds = StatsSeconds }),
            NullLogger<MemoryStatsCache>.Instance);
    }

    [Fact]
    public async Task SecondCall_IsServedFromCache()
    {
        var first = await _cache.GetOrCreateAsync(Factory);
        var second = await _cache.GetOrCreateAsync(Factory);

        _factoryCalls.Should().Be(1);
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task AfterInvalidate_FactoryIsCalledAgain()
    {
        await _cache.GetOrCreateAsync(Factory);

        _cache.Invalidate();
        var afterInvalidate = await _cache.GetOrCreateAsync(Factory);

        _factoryCalls.Should().Be(2);
        afterInvalidate.Total.Should().Be(2);
    }

    [Fact]
    public async Task BeforeExpiration_IsServedFromCache_AfterExpiration_FactoryIsCalledAgain()
    {
        await _cache.GetOrCreateAsync(Factory);

        _clock.Advance(TimeSpan.FromSeconds(StatsSeconds - 1));
        await _cache.GetOrCreateAsync(Factory);
        _factoryCalls.Should().Be(1);

        _clock.Advance(TimeSpan.FromSeconds(2));
        await _cache.GetOrCreateAsync(Factory);
        _factoryCalls.Should().Be(2);
    }

    [Fact]
    public async Task ExpirationIsAbsolute_ReadsDoNotExtendIt()
    {
        await _cache.GetOrCreateAsync(Factory);

        // Read every 30 s: with sliding expiration the entry would never expire.
        for (var i = 0; i < 3; i++)
        {
            _clock.Advance(TimeSpan.FromSeconds(30));
            await _cache.GetOrCreateAsync(Factory);
        }

        _factoryCalls.Should().BeGreaterThan(1);
    }

    public void Dispose() => _memoryCache.Dispose();

    private Task<RequestStatsDto> Factory()
    {
        _factoryCalls++;
        return Task.FromResult(new RequestStatsDto(
            new StatusCountsDto(0, 0, 0, _factoryCalls), new PriorityCountsDto(_factoryCalls, 0, 0), _factoryCalls));
    }

    private sealed class FakeClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; private set; } = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => UtcNow += by;
    }
}
