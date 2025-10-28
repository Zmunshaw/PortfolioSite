using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
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

    public static async Task SeedDatabase(SearchEngineCtx dbCtx)
    {
        var safeMode = Environment.GetEnvironmentVariable("SAFE_MODE") != "false";
        var wipeDatabase = Environment.GetEnvironmentVariable("WIPE_ON_START") == "true";

        // safety first.
        if (safeMode)
            return;

        if (!safeMode && wipeDatabase)
        {
            dbCtx.Database.EnsureDeleted();
            dbCtx.Database.EnsureCreated();

            List<string[]> sites = [];
            var words = new string[] { };

            if (!File.Exists(SiteSeedPath))
            {
                Console.WriteLine($"Error: File not found at {SiteSeedPath}");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(SiteSeedPath);
                foreach (var line in lines)
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
            var chunkSize = (int)Math.Ceiling(websites.Count / 200f);
            var websiteLists = websites
                .Select((website, index) => new { website, index })
                .GroupBy(x => x.index / chunkSize)
                .Select(g => g.Select(x => x.website).ToList())
                .ToList();
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
                IncludeGraph = true
            };

            Console.WriteLine($"Actual Conn STR {dbCtx.Database.GetConnectionString()}");
            Console.WriteLine($"Inserting {dictionary.Count} Words...");
            await dbCtx.BulkInsertAsync(dictionary, bulkConfig);
            Console.WriteLine($"Inserting {websites.Count} Websites...");

            // TODO: make all these hard-coded vars consts or smnthng
            var cntr = 200;
            foreach (var chunk in websiteLists)
            {
                await dbCtx.BulkInsertAsync(chunk, bulkConfig);
                cntr--;
                Console.WriteLine($"Inserted {chunk.Count} Websites {cntr} chunks to go!...");

                if (cntr % 10 == 0)
                {
                    Console.WriteLine("Checkpoint reached, saving...");
                    await dbCtx.SaveChangesAsync();
                    Console.WriteLine($"{chunk.Count * 10} websites committed to the DB...");
                }
            }

            Console.WriteLine($"Saving {websites.Count + dictionary.Count} Seed Items...");
            dbCtx.SaveChanges();
        }
    }
}