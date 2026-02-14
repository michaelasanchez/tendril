using Microsoft.Extensions.DependencyInjection;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Runtime;
using Tendril.Engine.Scrapers;
using Tendril.Engine.Services;

namespace Tendril.Engine;

public static class DependencyInjection
{
    public static IServiceCollection AddEngineServices(this IServiceCollection services)
    {
        services.AddTransient<ScrapeResourceManager>();

        services.AddScoped<IClassificationService, ClassificationService>();
        services.AddScoped<IMapperService, MapperService>();
        services.AddScoped<IJsonLdProcessor, JsonLdProcessor>();

        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<IScrapeExecutor, ScrapeExecutor>();

        services.AddScoped<DynamicScraper>();
        services.AddScoped<StaticScraper>();

        return services;
    }
}