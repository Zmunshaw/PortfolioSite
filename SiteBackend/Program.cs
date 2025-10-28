using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SiteBackend.Data.SeedData;
using SiteBackend.Database;
using SiteBackend.Middleware.AIClient;
using SiteBackend.Repositories.SearchEngine;
using SiteBackend.Services;
using SiteBackend.Services.Controllers;
using SiteBackend.Singletons;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:2125");
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
    var connectionString = Environment.GetEnvironmentVariable("SE_DB_CONN");

    bldr.Services.AddDbContextFactory<SearchEngineCtx>(options =>
        options.UseNpgsql(connectionString,
            npgsqlOptions => npgsqlOptions.UseVector())
    );
}

void AddServices(WebApplicationBuilder bldr)
{
    bldr.Services.AddScoped<IAiClient, AiClient>();
    bldr.Services.AddScoped<IAIService, AIService>();
    bldr.Services.AddScoped<ISitemapService, SitemapService>();
    bldr.Services.AddScoped<ICrawlerService, CrawlerService>();
    bldr.Services.AddScoped<ISearchService, SearchService>();
    bldr.Services.AddHostedService<EmbeddingManager>();
}

void AddRepositories(WebApplicationBuilder bldr)
{
    bldr.Services.AddScoped<IWebsiteRepo, WebsiteRepo>();
    bldr.Services.AddScoped<IPageRepo, PageRepo>();
    bldr.Services.AddScoped<IContentRepo, ContentRepo>();
    bldr.Services.AddScoped<IDictionaryRepo, DictionaryRepo>();
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

    using var scope = bldr.Services.BuildServiceProvider().CreateScope();
    var dbCtx = scope.ServiceProvider.GetRequiredService<SearchEngineCtx>();
    var seedTask = Task.Run(() => LoadSeedData.SeedDatabase(dbCtx)); // This should run on seperate thread
    Task.WaitAll(seedTask);


    Console.WriteLine($"Environment: {bldr.Environment.EnvironmentName} Initialized");
}