using Microsoft.EntityFrameworkCore;
using Tendril.Data;
using Tendril.Engine;
using Tendril.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<TendrilDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddEngineServices();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
