package main

import (
	"encoding/json"
	"log"
	"net/http"
	url2 "net/url"
	"sync"
	"time"
)

type PageScrapeRequest struct {
	PageURLsMu sync.RWMutex
	Pages      Root
}

type Root struct {
	ID     string     `json:"$id"`
	Values []PageData `json:"$values"`
}
type PageData struct {
	ID               string     `json:"$id"`
	PageID           int        `json:"pageID"`
	URL              URL        `json:"url"`
	Content          Content    `json:"content"`
	LastCrawlAttempt time.Time  `json:"lastCrawlAttempt"`
	LastCrawled      *time.Time `json:"lastCrawled"`
	Website          *string    `json:"website"`
}
type URL struct {
	ID           string  `json:"$id"`
	URLID        int     `json:"urlID"`
	Sitemap      *string `json:"sitemap"` // Nullable string
	Location     string  `json:"location"`
	LastModified *string `json:"lastModified"`    // Nullable string
	ChangeFreq   *string `json:"changeFrequency"` // Nullable string
	Priority     float64 `json:"priority"`
	Media        *string `json:"media"` // Nullable string
}
type Content struct {
	ID               string  `json:"$id"`
	ContentID        int     `json:"contentID"`
	Page             PageRef `json:"page"`
	Title            *string `json:"title"`
	Text             *string `json:"text"`
	Paragraphs       *string `json:"paragraphs"`
	Images           *string `json:"images"`
	ContentHash      *string `json:"contentHash"`
	ContentEmbedding *string `json:"contentEmbedding"`
}

// PageRef represents a reference to a page
type PageRef struct {
	Ref string `json:"$ref"`
}

func HandleRequests(reqRef *PageScrapeRequest) {
	http.HandleFunc("/"+CrawlerRequestScrapePath, func(w http.ResponseWriter, r *http.Request) {
		handlePagePost(w, r, reqRef)
	})
	log.Fatal(http.ListenAndServe(":"+CrawlerPort, nil))
}

func handlePagePost(w http.ResponseWriter, r *http.Request, reqRef *PageScrapeRequest) {
	if r.Method != http.MethodPost {
		w.WriteHeader(http.StatusMethodNotAllowed)
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var request PageScrapeRequest
	err := json.NewDecoder(r.Body).Decode(&request) // This should probably be limited to < 10mb
	if err != nil {
		w.WriteHeader(http.StatusBadRequest)
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}

	if len(request.Pages.Values) == 0 {
		w.WriteHeader(http.StatusBadRequest)
		http.Error(w, "page_urls is empty", http.StatusBadRequest)
		return
	}

	log.Println("Received URLs to scrape:")
	for i, url := range request.Pages.Values {
		log.Println(url)

		// TODO Fixit: https://0002112sasd--- passes, this shouldn't happen
		testUrl, err := url2.Parse("https://" + url.URL.Location)
		if err != nil {
			log.Println("Error parsing URL:", err)
			request.Pages.Values = append(request.Pages.Values[:i], request.Pages.Values[i+1:]...)
		} else if testUrl.Host == "" || testUrl.IsAbs() == false {
			log.Println("Invalid URL:", testUrl)
			request.Pages.Values = append(request.Pages.Values[:i], request.Pages.Values[i+1:]...)
		}
	}

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	response := map[string]interface{}{
		"status":        "success",
		"message":       "URLs received",
		"urls_received": len(request.Pages.Values),
	}

	json.NewEncoder(w).Encode(response)

	reqRef.PageURLsMu.Lock()
	defer reqRef.PageURLsMu.Unlock()
	for _, url := range request.Pages.Values {
		reqRef.Pages.Values = append(reqRef.Pages.Values, url)
	}
}
