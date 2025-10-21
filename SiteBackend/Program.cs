using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
AddDatabases(builder);
AddRepositories(builder);
AddServices(builder);
AddControllers(builder);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();

// Enable CORS middleware before routing
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers(); // Maps [ApiController] classes

app.Run();

void AddDatabases(WebApplicationBuilder bldr)
{
    bldr.Services.AddDbContextFactory<SearchEngineCtx>(options =>
        options.UseNpgsql(("Host=localhost;Port=5433;Database=dev-db;Username=pg-dev;Password=dev-pw"),
            npgsqlOptions => npgsqlOptions.UseVector())
    );
}

void AddServices(WebApplicationBuilder bldr)
{
    bldr.Services.AddScoped<IAiClient, AiClient>();
    bldr.Services.AddScoped<IAIService, AIService>();
    bldr.Services.AddScoped<ISitemapService, SitemapService>();
}

void AddRepositories(WebApplicationBuilder bldr)
{
    bldr.Services.AddScoped<IWebsiteRepo, WebsiteRepo>();
}

void AddControllers(WebApplicationBuilder bldr)
{
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });
}
