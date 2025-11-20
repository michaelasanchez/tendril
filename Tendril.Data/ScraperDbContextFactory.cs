using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Tendril.Data;

public class ScraperDbContextFactory : IDesignTimeDbContextFactory<TendrilDbContext>
{
    public TendrilDbContext CreateDbContext(string[] args)
    {
        // Find your appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TendrilDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("Default"));

        return new TendrilDbContext(optionsBuilder.Options);
    }
}
