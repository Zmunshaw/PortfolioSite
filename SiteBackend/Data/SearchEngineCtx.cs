using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Database;

public class SearchEngineCtx(DbContextOptions<SearchEngineCtx> options, ILogger<SearchEngineCtx> logger)
    : DbContext(options)
{
    #region DB Helper Methods

    /// <summary>
    ///     Quick and dirty way to make sure an entity exists within the db, while also trying to find it.
    /// </summary>
    /// <param name="predicate">
    ///     Search params
    ///     <code>page => page.Url == example.com</code>
    /// </param>
    /// <param name="createEntity">
    ///     Function for creation
    ///     <code>someVar => new TEntity { TVal1 = someVar.val1, TVal2 = someVar.Val2}</code>
    /// </param>
    /// <returns>If no predicate exists it creates one, otherwise it returns the first or default from db</returns>
    /// <remarks>Won't save any changes it makes, that's up to you, champ.</remarks>
    public TEntity FindOrCreate<TEntity>(Expression<Func<TEntity, bool>> predicate, Func<TEntity> createEntity)
        where TEntity : class
    {
        var entity = Set<TEntity>().FirstOrDefault(predicate);
        if (entity != null) return entity;

        logger.LogDebug("no entity found, creating new {Name}.", typeof(TEntity).Name);
        entity = createEntity();
        Set<TEntity>().Add(entity);
        return entity;
    }

    #endregion

    #region DBSets

    // Sitemap
    public DbSet<Sitemap> Sitemaps { get; set; }
    public DbSet<Sitemap> SitemapIndexes { get; set; }
    public DbSet<Url> Urls { get; set; }

    public DbSet<ImageEntry> ImageEntries { get; set; }
    public DbSet<VideoEntry> VideoEntries { get; set; }
    public DbSet<NewsEntry> NewsEntries { get; set; }

    // Website
    public DbSet<Website> Websites { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<Content> Contents { get; set; }

    // Search
    public DbSet<TextEmbedding> TextEmbeddings { get; set; }
    public DbSet<Word> Words { get; set; }

    #endregion

    #region DB Model Rules

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var dbconn = Environment.GetEnvironmentVariable("SE_DB_CONN") ?? "FUCK";

        var optionsBuilder = new DbContextOptionsBuilder<SearchEngineCtx>();
        optionsBuilder.UseNpgsql(dbconn,
            npgsqlOptions => npgsqlOptions
                .UseVector());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add pgVector for search
        modelBuilder.HasPostgresExtension("vector");

        // (Co)Dependant relationships
        // Content Depends on page
        modelBuilder.Entity<Content>()
            .HasOne(c => c.Page)
            .WithOne(p => p.Content)
            .HasForeignKey<Content>(ct => ct.ContentID);

        modelBuilder.Entity<Url>()
            .HasOne(p => p.Page)
            .WithOne(p => p.Url)
            .HasForeignKey<Url>(url => url.UrlID);

        modelBuilder.Entity<Sitemap>()
            .HasOne(sm => sm.Website)
            .WithOne(ws => ws.Sitemap)
            .HasForeignKey<Sitemap>(sm => sm.SitemapID);

        // For paragraph meaning
        modelBuilder.Entity<TextEmbedding>()
            .Property(te => te.DenseEmbedding)
            .HasColumnType("vector(768)");
        // For keyword meaning
        modelBuilder.Entity<TextEmbedding>()
            .Property(te => te.SparseEmbedding)
            .HasColumnType("sparsevec");

        // Establish polymorphic nature of MediaEntry table
        modelBuilder.Entity<MediaEntry>()
            .HasDiscriminator<MediaType>("Type")
            .HasValue<ImageEntry>(MediaType.Image)
            .HasValue<VideoEntry>(MediaType.Video)
            .HasValue<NewsEntry>(MediaType.News);

        // Define recursive sitemap relationships
        modelBuilder.Entity<Sitemap>()
            .HasOne(s => s.ParentSitemap)
            .WithMany(s => s.SitemapIndex)
            .HasForeignKey(s => s.ParentSitemapId)
            .OnDelete(DeleteBehavior.Restrict);

        // Fix non-UTC errors
        modelBuilder.Entity<Sitemap>()
            .Property(lm => lm.LastModified)
            .HasConversion(
                lm => lm.HasValue
                    ? (DateTime?)(lm.Value.Kind == DateTimeKind.Utc ? lm.Value : lm.Value.ToUniversalTime())
                    : null,
                lm => lm.HasValue
                    ? DateTime.SpecifyKind(lm.Value, DateTimeKind.Utc)
                    : null
            );
        modelBuilder.Entity<Url>()
            .Property(lm => lm.LastModified)
            .HasConversion(
                lm => lm.HasValue
                    ? (DateTime?)(lm.Value.Kind == DateTimeKind.Utc ? lm.Value : lm.Value.ToUniversalTime())
                    : null,
                lm => lm.HasValue
                    ? DateTime.SpecifyKind(lm.Value, DateTimeKind.Utc)
                    : null
            );
        modelBuilder.Entity<VideoEntry>()
            .Property(pd => pd.PublicationDate)
            .HasConversion(
                pd => pd.HasValue
                    ? (DateTime?)(pd.Value.Kind == DateTimeKind.Utc ? pd.Value : pd.Value.ToUniversalTime())
                    : null,
                pd => pd.HasValue
                    ? DateTime.SpecifyKind(pd.Value, DateTimeKind.Utc)
                    : null
            );
        modelBuilder.Entity<NewsEntry>()
            .Property(pd => pd.PublicationDate)
            .HasConversion(
                pd => pd.HasValue
                    ? (DateTime?)(pd.Value.Kind == DateTimeKind.Utc ? pd.Value : pd.Value.ToUniversalTime())
                    : null,
                pd => pd.HasValue
                    ? DateTime.SpecifyKind(pd.Value, DateTimeKind.Utc)
                    : null
            );
    }

    #endregion
}

public class SearchEngineCtxDesignTimeFactory : IDesignTimeDbContextFactory<SearchEngineCtx>
{
    public SearchEngineCtx CreateDbContext(string[] args)
    {
        // TODO: Fix this
        var connectionString =
            "host=localhost;port=1288;database=se-dev-db;username=se-dev-master;password=se-dev-pass;";
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            connectionString =
                "host=localhost;port=5021;database=se-prod-db;username=se-prod-master;password=se-prod-pass;";

        var optionsBuilder = new DbContextOptionsBuilder<SearchEngineCtx>();
        optionsBuilder.UseNpgsql(connectionString,
            npgsqlOptions => npgsqlOptions.UseVector());

        return new SearchEngineCtx(optionsBuilder.Options, new Logger<SearchEngineCtx>(new LoggerFactory()));
    }
}