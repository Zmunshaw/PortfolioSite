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
        options.UseNpgsql("Host=se-db;Port=5021;Database=postgres;Username=se-master;Password=se-pass",
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

    if (bldr.Environment.IsDevelopment())
    {
        using var scope = bldr.Services.BuildServiceProvider().CreateScope();
        var dbCtx = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SearchEngineCtx>>().CreateDbContext();

        var seedTask = Task.Run(() => LoadSeedData.SeedDatabase(dbCtx)); // This should run on seperate thread
        Task.WaitAll(seedTask);
    }
    else
    {
        for (var i = 0; i < 20; i++)
            Console.WriteLine("You should probably fill in the logic production");
        throw new Exception("You should probably fill in the logic production environment.....");
    }

    Console.WriteLine($"Environment: {bldr.Environment.EnvironmentName} Initialized");
}