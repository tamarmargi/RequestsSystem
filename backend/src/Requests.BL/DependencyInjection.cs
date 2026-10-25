using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Requests.BL.Caching;
using Requests.BL.Services;

namespace Requests.BL;

public static class DependencyInjection
{
    public static IServiceCollection AddBl(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Singleton over the singleton IMemoryCache; it never captures scoped services (the factory is passed per call).
        // CacheOptions are bound from the "Cache" section by the host.
        services.AddMemoryCache();
        services.AddSingleton<IStatsCache, MemoryStatsCache>();

        services.AddScoped<IRequestQueryService, RequestQueryService>();
        services.AddScoped<IRequestCommandService, RequestCommandService>();

        return services;
    }
}
