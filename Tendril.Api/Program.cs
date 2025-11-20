using System.Text.Json.Serialization;
using Tendril.Api.Mapping;
using Tendril.Data;
using Tendril.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddEngineServices();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddAutoMapper(
    cfg => { },                               // no custom config for now
    typeof(ApiMappingProfile).Assembly       // scan this assembly for profiles
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
