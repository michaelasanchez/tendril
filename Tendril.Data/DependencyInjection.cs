using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tendril.Core.Interfaces.Repositories;
using Tendril.Data.Repositories;

namespace Tendril.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TendrilDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Default"));
        });

        // Repository DI
        services.AddScoped<IAttemptHistoryRepository, AttemptHistoryRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IMappingRuleRepository, MappingRuleRepository>();
        services.AddScoped<IRawEventRepository, RawEventRepository>();
        services.AddScoped<IScraperRepository, ScraperRepository>();
        services.AddScoped<ISelectorRepository, SelectorRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();

        return services;
    }
}
