using System.Text.Json.Serialization;
using Tendril.Api.Mapping;
using Tendril.Data;
using Tendril.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("ScraperClient", client =>
{
    // 1. Set a modern Browser User-Agent (Chrome on Windows)
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

    // 2. Set the Accept header to standard HTML navigation
    client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");

    // 3. Set Accept-Language (Sometimes required by negotiation logic)
    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

    // 4. (Optional but recommended) Set Accept-Encoding to handle gzip/br automatically
    // Note: If you do this, ensure your HttpClientHandler has AutomaticDecompression enabled.
});

builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddEngineServices();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddAutoMapper(
    cfg => { },                               // no custom config for now
    typeof(ApiMappingProfile).Assembly       // scan this assembly for profiles
);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
