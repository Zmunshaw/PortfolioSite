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

// API
var API_PORT = Environment.GetEnvironmentVariable("API_PORT");

// Databases
var SEARCH_ENGINE_DB_CONN = Environment.GetEnvironmentVariable("SE_DB_CONN");
var SITE_DB_CONN = Environment.GetEnvironmentVariable("SITE_DB_CONN");

// State
var IsDevelopment = builder.Environment.EnvironmentName == "Development";

// URLs
builder.WebHost.UseUrls($"http://0.0.0.0:{API_PORT}");

// Services.
AddCORS(builder);
AddDatabases(builder);
AddRepositories(builder);
AddServices(builder);
AddControllers(builder);
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
        options.UseNpgsql(SEARCH_ENGINE_DB_CONN,
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