using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Database;

public class SearchEngineCtx : DbContext
{
    public SearchEngineCtx(DbContextOptions<SearchEngineCtx> options)
        : base(options)
    {
        
    }
    
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add pgVector for search
        modelBuilder.HasPostgresExtension("vector");

        // For paragraph meaning
        modelBuilder.Entity<Content>()
            .Property(p => p.ContentEmbedding)
            .HasColumnType("vector(768)");
        // For word meaning
        modelBuilder.Entity<Word>()
            .Property(p => p.SparseVectors)
            .HasColumnType("sparsevec(768)");
        
        // Sort google sitemap conventions
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
        
        // Fix bullshit non-UTC errors
        modelBuilder.Entity<Sitemap>()
            .Property(lm => lm.LastModified)
            .HasConversion(
                lm => lm.HasValue 
                    ? (DateTime?) (lm.Value.Kind == DateTimeKind.Utc ? lm.Value : lm.Value.ToUniversalTime()) 
                    : null,
                lm => lm.HasValue 
                    ? DateTime.SpecifyKind(lm.Value, DateTimeKind.Utc) 
                    : null
            );
        modelBuilder.Entity<Url>()
            .Property(lm => lm.LastModified)
            .HasConversion(
                lm => lm.HasValue 
                    ? (DateTime?) (lm.Value.Kind == DateTimeKind.Utc ? lm.Value : lm.Value.ToUniversalTime()) 
                    : null,
                lm => lm.HasValue 
                    ? DateTime.SpecifyKind(lm.Value, DateTimeKind.Utc) 
                    : null
            );
        modelBuilder.Entity<VideoEntry>()
            .Property(pd => pd.PublicationDate)
            .HasConversion(
                pd => pd.HasValue 
                    ? (DateTime?) (pd.Value.Kind == DateTimeKind.Utc ? pd.Value : pd.Value.ToUniversalTime()) 
                    : null,
                pd => pd.HasValue 
                    ? DateTime.SpecifyKind(pd.Value, DateTimeKind.Utc) 
                    : null
            );
        modelBuilder.Entity<NewsEntry>()
            .Property(pd => pd.PublicationDate)
            .HasConversion(
                pd => pd.HasValue 
                    ? (DateTime?) (pd.Value.Kind == DateTimeKind.Utc ? pd.Value : pd.Value.ToUniversalTime()) 
                    : null,
                pd => pd.HasValue 
                    ? DateTime.SpecifyKind(pd.Value, DateTimeKind.Utc) 
                    : null
            );
    }
}

public class SitemapCtxFactory : IDesignTimeDbContextFactory<SearchEngineCtx>
{
    public SearchEngineCtx CreateDbContext(string[] args)
    {
        // TODO: get conn string from appsettings.json
        var optionsBuilder = new DbContextOptionsBuilder<SearchEngineCtx>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=dev-db;Username=pg-dev;Password=dev-pw",
            npgsqlOptions => npgsqlOptions.UseVector());

        return new SearchEngineCtx(optionsBuilder.Options);
    }
}