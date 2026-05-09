using Microsoft.Extensions.DependencyInjection;
using Tendril.Engine.Abstractions;
using Tendril.Engine.Interfaces;
using Tendril.Engine.Playwright;
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
        services.AddScoped<IMappingService, MappingService>();
        services.AddScoped<IJsonLdProcessor, JsonLdProcessor>();
        services.AddScoped<ITemplateService, TemplateService>();

        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<IScrapeExecutor, ScrapeExecutor>();

        services.AddScoped<ApiScraper>();
        services.AddScoped<DynamicScraper>();
        services.AddScoped<StaticScraper>();

        services.AddSingleton<PlaywrightContextFactory>();

        return services;
    }
}