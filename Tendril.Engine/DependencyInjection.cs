using Microsoft.Extensions.DependencyInjection;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Runtime;

namespace Tendril.Engine;

public static class DependencyInjection
{
    public static IServiceCollection AddEngineServices(this IServiceCollection services)
    {
        services.AddScoped<IEventMapper, EventMapper>();
        services.AddScoped<IScraperFactory, ScraperFactory>();
        services.AddScoped<IScrapeExecutor, ScrapeExecutor>();

        return services;
    }
}