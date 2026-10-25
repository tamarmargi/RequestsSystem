using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Requests.BL.Services;

namespace Requests.BL;

public static class DependencyInjection
{
    public static IServiceCollection AddBl(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IRequestQueryService, RequestQueryService>();
        services.AddScoped<IRequestCommandService, RequestCommandService>();

        return services;
    }
}
