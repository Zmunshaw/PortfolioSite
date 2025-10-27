using EFCore.BulkExtensions;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Data.SeedData;

public static class LoadSeedData
{
    private static readonly string SiteSeedPath = Environment
        .GetEnvironmentVariable("SITE_SEED_PATH") ?? "/app/data/seed.csv";

    private static readonly string WordSeedPath = Environment
        .GetEnvironmentVariable("WORD_SEED_PATH") ?? "/app/data/words.txt";

    public static async Task SeedDatabase(SearchEngineCtx dbCtx, bool isDevelopment = true)
    {
        List<string[]> sites = [];
        string[] words = new string[] { };

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

            Console.WriteLine($"Loaded {sites.Count} sites from {SiteSeedPath}");

            words = File.ReadAllLines(WordSeedPath);
            Console.WriteLine($"Loaded {words.Length} words");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        var websites = sites.AsParallel()
            .Select(site => new Website(site[1].Trim('"'))).ToList();
        var dictionary = words.AsParallel()
            .Select(word => new Word { Text = word }).ToList();


        var bulkConfig = new BulkConfig
        {
            PreserveInsertOrder = false,
            SetOutputIdentity = false,
            BatchSize = 250000,
            BulkCopyTimeout = 0,
            EnableStreaming = true,
            TrackingEntities = false,
            WithHoldlock = false,
            IncludeGraph = true,
        };

        dbCtx.Database.EnsureCreated();
        Console.WriteLine($"Inserting {dictionary.Count} Words...");
        await dbCtx.BulkInsertAsync(dictionary, bulkConfig);
        Console.WriteLine($"Inserting {websites.Count} Websites...");
        await dbCtx.BulkInsertAsync(websites, bulkConfig);

        Console.WriteLine($"Saving {websites.Count + dictionary.Count} Seed Items...");
        dbCtx.SaveChanges();
    }
}