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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if not already configured (respects dependency injection)
        if (optionsBuilder.IsConfigured)
            return;

        var dbconn = Environment.GetEnvironmentVariable("SE_DB_CONN")
                     ?? throw new InvalidOperationException("SE_DB_CONN environment variable is not set");

        optionsBuilder.UseNpgsql(dbconn,
            npgsqlOptions => npgsqlOptions.UseVector());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add pgVector for search
        modelBuilder.HasPostgresExtension("vector");

        // Website -> Sitemap (one-to-one)
        modelBuilder.Entity<Website>()
            .HasOne(w => w.Sitemap)
            .WithOne(s => s.Website)
            .HasForeignKey<Sitemap>(s => s.WebsiteID);

        // Website -> Page (one-to-many)
        modelBuilder.Entity<Website>()
            .HasMany(w => w.Pages)
            .WithOne(p => p.Website)
            .HasForeignKey(p => p.WebsiteID)
            .OnDelete(DeleteBehavior.Cascade);

        // Page -> Url (one-to-one)
        modelBuilder.Entity<Page>()
            .HasOne(p => p.Url)
            .WithOne(u => u.Page)
            .HasForeignKey<Url>(u => u.PageID)
            .OnDelete(DeleteBehavior.Cascade);

        // Sitemap -> Url (one-to-many)
        modelBuilder.Entity<Sitemap>()
            .HasMany(s => s.UrlSet)
            .WithOne(u => u.Sitemap)
            .HasForeignKey(u => u.SitemapID)
            .OnDelete(DeleteBehavior.SetNull);

        // Url -> MediaEntry (one-to-many)
        modelBuilder.Entity<Url>()
            .HasMany(u => u.Media)
            .WithOne(m => m.Url)
            .HasForeignKey(m => m.UrlID)
            .OnDelete(DeleteBehavior.Cascade);

        // Page -> Content (one-to-one)
        modelBuilder.Entity<Page>()
            .HasOne(p => p.Content)
            .WithOne(c => c.Page)
            .HasForeignKey<Content>(c => c.PageID);

        // Content -> TextEmbedding (one-to-many)
        modelBuilder.Entity<Content>()
            .HasMany(c => c.Embeddings)
            .WithOne(te => te.Content)
            .HasForeignKey(te => te.ContentID);

        // TextEmbedding vector columns
        modelBuilder.Entity<TextEmbedding>()
            .Property(te => te.DenseEmbedding)
            .HasColumnType("vector(768)");

        modelBuilder.Entity<TextEmbedding>()
            .Property(te => te.SparseEmbedding)
            .HasColumnType("sparsevec");

        // Polymorphic MediaEntry table with discriminator
        modelBuilder.Entity<MediaEntry>()
            .HasDiscriminator<MediaType>("Type")
            .HasValue<ImageEntry>(MediaType.Image)
            .HasValue<VideoEntry>(MediaType.Video)
            .HasValue<NewsEntry>(MediaType.News);

        // Recursive sitemap relationships
        modelBuilder.Entity<Sitemap>()
            .HasOne(s => s.ParentSitemap)
            .WithMany(s => s.SitemapIndex)
            .HasForeignKey(s => s.ParentSitemapId)
            .OnDelete(DeleteBehavior.Restrict);

        // DateTime UTC conversions
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
        var connectionString = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production"
            ? "host=localhost;port=5021;database=se-prod-db;username=se-prod-master;password=se-prod-pass;"
            : "host=localhost;port=1288;database=se-dev-db;username=se-dev-master;password=se-dev-pass;";

        var optionsBuilder = new DbContextOptionsBuilder<SearchEngineCtx>();
        optionsBuilder.UseNpgsql(connectionString,
            npgsqlOptions => npgsqlOptions.UseVector());

        return new SearchEngineCtx(optionsBuilder.Options, new Logger<SearchEngineCtx>(new LoggerFactory()));
    }
}