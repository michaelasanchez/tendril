using Auth;
using System.Net;
using System.Text.Json.Serialization;
using Tendril.Api.Mapping;
using Tendril.Data;
using Tendril.Engine;

var builder = WebApplication.CreateBuilder(args);

var cookieContainer = new CookieContainer();
builder.Services.AddSingleton(cookieContainer);

builder.Services.AddHttpClient("ScraperClient")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
    {
        // TicketWeb/Akamai often requires these for modern bot detection bypass
        //AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        CookieContainer = cookieContainer, // This is crucial for session persistence
        AutomaticDecompression = DecompressionMethods.All
    })
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        // Add these additional "Sec-CH" headers to look like a real Chromium browser
        client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
        client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
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

builder.Services.AddAuthServices(builder.Configuration, builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:Origin"]!)
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
