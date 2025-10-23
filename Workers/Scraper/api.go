package main

import (
	"encoding/json"
	"log"
	"net/http"
	url2 "net/url"
	"sync"
)

type PageScrapeRequest struct {
	PageURLsMu sync.RWMutex
	PageURLs   []string `json:"page_urls"`
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

	if len(request.PageURLs) == 0 {
		w.WriteHeader(http.StatusBadRequest)
		http.Error(w, "page_urls is empty", http.StatusBadRequest)
		return
	}

	log.Println("Received URLs to scrape:")
	for i, url := range request.PageURLs {
		log.Println(url)

		// TODO Fixit: https://0002112sasd--- passes, this shouldn't happen
		testUrl, err := url2.Parse("https://" + url)
		if err != nil {
			log.Println("Error parsing URL:", err)
			request.PageURLs = append(request.PageURLs[:i], request.PageURLs[i+1:]...)
		} else if testUrl.Host == "" || testUrl.IsAbs() == false {
			log.Println("Invalid URL:", testUrl)
			request.PageURLs = append(request.PageURLs[:i], request.PageURLs[i+1:]...)
		}
	}

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	response := map[string]interface{}{
		"status":        "success",
		"message":       "URLs received",
		"urls_received": len(request.PageURLs),
	}

	json.NewEncoder(w).Encode(response)

	reqRef.PageURLsMu.Lock()
	defer reqRef.PageURLsMu.Unlock()
	for _, url := range request.PageURLs {
		reqRef.PageURLs = append(reqRef.PageURLs, url)
	}
}
