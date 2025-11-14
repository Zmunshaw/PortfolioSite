package main

import (
    "encoding/json"
    "fmt"
    "log"
    "net/http"
    "os"

    "github.com/syumai/workers"
)

var (
	ApiKey          = os.Getenv("API_KEY")
	ScrapeWorkerUrl = os.Getenv("SCRAPE_WORKER_URL")
	BackendURL      = os.Getenv("BACKEND_URL")
)

func main() {
    fmt.Printf("API Key: %s BACKEND_URL: %s SCRAPE_WORKER_URL: %s\n", ApiKey, BackendURL, ScrapeWorkerUrl)

	http.HandleFunc("/scrape", func(w http.ResponseWriter, req *http.Request) {
		scrapeRequestHandler(w, req)
	})

	http.HandleFunc("/map", func(w http.ResponseWriter, req *http.Request) {
		mapRequestHandler(w, req)
	})


	http.HandleFunc("/scrape-sitemap", func(w http.ResponseWriter, req *http.Request) {
		scrapeSitemapHandler(w, req)
	})

    // Run mode: if not in a Workers environment, start a local HTTP server
    port := os.Getenv("PORT")
    if port == "" {
        port = "9900"
    }

    if os.Getenv("CF_WORKERS") == "1" {
        workers.Serve(nil)
        return
    }

    addr := ":" + port
    fmt.Printf("Starting local crawler server on %s\n", addr)
    if err := http.ListenAndServe(addr, nil); err != nil {
        log.Fatalf("server failed: %v", err)
    }
}

func scrapeRequestHandler(w http.ResponseWriter, req *http.Request) {
	if req.Method != http.MethodPost {
		w.WriteHeader(http.StatusMethodNotAllowed)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"error": "POST only"})
		fmt.Println("FAIL: scrape request POST only")
		return
	}

	var urls []string
	err := json.NewDecoder(req.Body).Decode(&urls)
	if err != nil {
		w.WriteHeader(http.StatusBadRequest)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"error": "Invalid JSON"})
		return
	}
	defer req.Body.Close()

	results, err := ScrapeSites(urls)
	w.Header().Set("Content-Type", "application/json")
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		json.NewEncoder(w).Encode(map[string]string{"error": err.Error()})
		return
	}

	json.NewEncoder(w).Encode(results)
}

func mapRequestHandler(w http.ResponseWriter, req *http.Request) {
	var baseURL string

	if req.Method == http.MethodGet {
		// ?url=https://example.com
		baseURL = req.URL.Query().Get("url")
	} else if req.Method == http.MethodPost {
		// {"url": "https://example.com"}
		var body map[string]string
		json.NewDecoder(req.Body).Decode(&body)
		baseURL = body["url"]
	}

	if baseURL == "" {
		w.WriteHeader(http.StatusBadRequest)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"error": "url parameter required"})
		return
	}

	sitemapData, err := FindSitemap(baseURL)
	if err != nil {
		w.WriteHeader(http.StatusNotFound)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"error": err.Error()})
		return
	}

	sitemap, err := ParseSitemap(baseURL, sitemapData)
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"error": err.Error()})
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(sitemap)
}

func scrapeSitemapHandler(w http.ResponseWriter, req *http.Request) {
	var baseURL string
	var batchSize int = 50 // Default batch size
	var maxConcurrent int = 5 // Default concurrent sitemap fetches

	if req.Method == http.MethodGet {
		baseURL = req.URL.Query().Get("url")
	} else if req.Method == http.MethodPost {
		var body map[string]interface{}
		json.NewDecoder(req.Body).Decode(&body)
		baseURL, _ = body["url"].(string)

		// Allow customization of batch size and concurrency
		if bs, ok := body["batchSize"].(float64); ok {
			batchSize = int(bs)
		}
		if mc, ok := body["maxConcurrent"].(float64); ok {
			maxConcurrent = int(mc)
		}
	}

	if baseURL == "" {
		w.WriteHeader(http.StatusBadRequest)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"error": "url parameter required"})
		return
	}

	// Extract all URLs from sitemap
	urls, err := ExtractAllURLs(baseURL, maxConcurrent)
	if err != nil {
		w.WriteHeader(http.StatusInternalServerError)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{"error": err.Error()})
		return
	}

	fmt.Printf("Found %d URLs in sitemap for %s\n", len(urls), baseURL)

	// Process URLs in batches
	batches := BatchURLs(urls, batchSize)
	var allResults []map[string]interface{}

	for i, batch := range batches {
		fmt.Printf("Processing batch %d/%d (%d URLs)\n", i+1, len(batches), len(batch))

		results, err := ScrapeSites(batch)
		if err != nil {
			// Continue processing other batches even if one fails
			fmt.Printf("Warning: batch %d failed: %v\n", i+1, err)
			continue
		}

		allResults = append(allResults, results...)
	}

	w.Header().Set("Content-Type", "application/json")
	response := map[string]interface{}{
		"url": baseURL,
		"totalUrls": len(urls),
		"scrapedUrls": len(allResults),
		"results": allResults,
	}
	json.NewEncoder(w).Encode(response)
}
