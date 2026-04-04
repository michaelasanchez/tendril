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
        services.AddScoped<IApiParameterRepository, ApiParameterRepository>();
        services.AddScoped<IAttemptHistoryRepository, AttemptHistoryRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventRevisionRepository, EventRevisionRepository>();
        services.AddScoped<IClassificationRuleRepository, ClassificationRuleRepository>();
        services.AddScoped<IMappingRuleRepository, MappingRuleRepository>();
        services.AddScoped<IRawEventRepository, RawEventRepository>();
        services.AddScoped<IScraperRepository, ScraperRepository>();
        services.AddScoped<IActionRepository, ActionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();

        return services;
    }
}
