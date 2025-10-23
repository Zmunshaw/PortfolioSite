using System.Collections.Concurrent;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Data.SeedData;

public static class LoadSeedData
{
    // TODO: not this.
    private static readonly string SiteSeedPath = "/home/zachary/Downloads/seed.csv";
    public static void SeedDatabase(SearchEngineCtx dbCtx, bool isDevelopment = true)
    {
        List<string[]> sites = [];

        if (!File.Exists(SiteSeedPath))
        {
            Console.WriteLine($"Error: File not found at {SiteSeedPath}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(SiteSeedPath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var values = line.Split(','); 
                sites.Add(values);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }


        if (isDevelopment)
        {
            Console.Write("WARNING - WARNING ");
            Console.WriteLine();
            Console.WriteLine($"PRESS ANY KEY TO DELETE DATABASE CONTINUE TO THE DEVELOPMENT");
            Console.ReadKey();
            Console.WriteLine("Deleting Database...");
            dbCtx.Database.EnsureDeleted();
            Console.WriteLine("Database deleted...");
            Console.WriteLine("Creating Database...");
            dbCtx.Database.EnsureCreated();
            Console.WriteLine("Database created...");
            Console.WriteLine("Getting Seed Sites...");
            sites = sites.Take(sites.Count / 400).ToList(); // otherwise, will take longer than heat-death of universe.
        }

        ConcurrentBag<Website> websites = [];
        Parallel.ForEach(sites, site =>
            {
                websites.Add(new Website(site[1]));
            }
        );
        
        dbCtx.ChangeTracker.AutoDetectChangesEnabled = false;
        Console.WriteLine($"Loaded {websites.Count} websites");
        dbCtx.Websites.AddRange(websites);
        Console.WriteLine($"Detecting changes for {websites.Count} websites...");
        dbCtx.ChangeTracker.DetectChanges();
        Console.WriteLine($"Saving {websites.Count} websites...");
        dbCtx.SaveChanges();
    }
}