using Microsoft.Extensions.DependencyInjection;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Logic;
using Tendril.Engine.Runtime;

namespace Tendril.Engine;

public static class DependencyInjection
{
    public static IServiceCollection AddEngineServices(this IServiceCollection services)
    {
        services.AddTransient<ScrapeResourceManager>();

        services.AddScoped<IEventMapper, EventMapper>();
        services.AddScoped<IJsonLdProcessor, JsonLdProcessor>();

        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<IScrapeExecutor, ScrapeExecutor>();

        services.AddScoped<DynamicScraper>();
        services.AddScoped<StaticScraper>();

        return services;
    }
}