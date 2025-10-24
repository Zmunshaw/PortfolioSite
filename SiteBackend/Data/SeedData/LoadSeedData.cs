using System.Collections.Concurrent;
using System.Diagnostics;
using EFCore.BulkExtensions;
using SiteBackend.Database;
using SiteBackend.Models.SearchEngine;
using SiteBackend.Models.SearchEngine.Index;

namespace SiteBackend.Data.SeedData;

public static class LoadSeedData
{
    // TODO: not this.
    private static readonly string SiteSeedPath = "/home/zachary/Downloads/seed.csv";
    private static readonly string WordSeedPath = "/home/zachary/Downloads/words.txt";
    public static void SeedDatabase(SearchEngineCtx dbCtx, bool isDevelopment = true)
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


        if (isDevelopment)
        {
            Console.Write("WARNING - WARNING ");
            Console.WriteLine();
            Console.WriteLine($"Press Y to wipe db");
            var resp = Console.ReadKey();
            if (resp.Key != ConsoleKey.Y) return;
            Console.WriteLine("Deleting Database...");
            dbCtx.Database.EnsureDeleted();
            Console.WriteLine("Database deleted...");
            Console.WriteLine("Creating Database...");
            dbCtx.Database.EnsureCreated();
            Console.WriteLine("Database created...");
            Console.WriteLine("Getting Seed Sites...");
            
            sites = sites.Take(sites.Count / 500).ToList();
        }

        var websites = new ConcurrentBag<Website>();
        var dictionary = new ConcurrentBag<Word>();
        Parallel.Invoke(
            () =>
            {
                Parallel.ForEach(sites, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
                    site =>
                    {
                        string trimmedUrl = site[1].Trim('"');
                        var newSite = new Website(trimmedUrl);
                        if (Uri.TryCreate(trimmedUrl, UriKind.RelativeOrAbsolute, out Uri _))
                            websites.Add(newSite);
                        else
                            Console.WriteLine($"Site {trimmedUrl} could not be parsed into a valid URL.");
                        
                        Debug.Assert(newSite.Sitemap != null, "sitemap null...");
                    });
            },
            () =>
            {
                Parallel.ForEach(words, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
                    word =>
                    {
                        dictionary.Add(new Word { Text = word });
                    });
            }
        );
        
        
        var bulkConfig = new BulkConfig
        {
            PreserveInsertOrder = false,
            SetOutputIdentity = false,
            BatchSize = 5000,
            BulkCopyTimeout = 0,
            EnableStreaming = true,
            TrackingEntities = false,
            WithHoldlock = false,
            IncludeGraph = true,
        };

        dbCtx.Database.EnsureCreated();
        Console.WriteLine($"Inserting {dictionary.Count} Words...");
        bulkConfig.BatchSize = 50000;
        dbCtx.BulkInsert(dictionary, bulkConfig);
        dbCtx.SaveChanges();
        Console.WriteLine($"Inserting {websites.Count} Websites...");
        bulkConfig.BatchSize = 50000;
        dbCtx.BulkInsert(websites, bulkConfig);
        
        Console.WriteLine($"Saving {websites.Count + dictionary.Count} Seed Items...");
        dbCtx.SaveChanges();
    }
}