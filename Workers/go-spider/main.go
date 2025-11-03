package main

import (
	"encoding/json"
	"fmt"
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

	workers.Serve(nil)
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
