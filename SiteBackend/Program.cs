using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Database;
using SiteBackend.Data.SeedData;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
AddDatabases(builder);
AddRepositories(builder);
AddServices(builder);
AddControllers(builder);
AddCORS(builder);
BuildDev(builder);
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
    bldr.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
}

void AddCORS(WebApplicationBuilder bldr)
{
    bldr.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });
}

void BuildDev(WebApplicationBuilder bldr)
{
    Console.WriteLine($"Environment: {bldr.Environment.EnvironmentName} Initializing...");
    
    if (bldr.Environment.IsDevelopment())
    {
        using var scope = bldr.Services.BuildServiceProvider().CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchEngineCtx>>().CreateDbContext();
        
        LoadSeedData.SeedDatabase(dbCtx);
    }
    else
    {
        for(int i = 0; i < 20; i++)
            Console.WriteLine("You should probably fill in the logic production");
        throw new Exception("You should probably fill in the logic production environment.....");
    }
    
    Console.WriteLine($"Environment: {bldr.Environment.EnvironmentName} Initialized");
}
