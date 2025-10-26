package main

import (
	"fmt"
	"time"
)

// Site Data
var (
	BackendHost        = "http://localhost"
	BackendPort        = "1234"
	BackendUrl         = BackendHost + ":" + BackendPort
	BackendCrawlerPath = "crawler"
	BackendScrapePath  = "scrape"
)

// Settings
var (
	// Batching
	PagesPerSubmission        = 50
	RequestMorePagesThreshold = 50

	// Scraping
	Parallelism = 50
)

// Headers
var (
	CrawlerHeaders = map[string]string{
		"Accept-Language":           "en-US,en;q=0.9",
		"User-Agent":                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
		"Accept":                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
		"Upgrade-Insecure-Requests": "1",
		"Referer":                   "https://www.google.com",
		"Accept-Encoding":           "gzip, deflate, br",
		"Connection":                "keep-alive",
		"DNT":                       "1",
		"Sec-Fetch-Dest":            "document",
		"Sec-Fetch-Site":            "none",
		"Sec-Fetch-Mode":            "navigate",
		"Sec-Fetch-User":            "?1",
		"Cache-Control":             "max-age=0",
	}
)

func main() {
	var scrapeRequests []DTOCrawlRequest
	dtoScrapeChannel := make(chan *DTOCrawlerData, 1024)
	collector := SetupCollector(dtoScrapeChannel)
	setupDTOChannelReader(dtoScrapeChannel)
	ticker := time.NewTicker(5 * time.Second)
	defer ticker.Stop()

	fmt.Println("Starting scrape requests...")
	for {
		select {
		case <-ticker.C:
			if len(scrapeRequests) <= RequestMorePagesThreshold {
				fmt.Println("Requesting more pages")
				newPages, err := RequestMorePages()
				fmt.Println("Requested more pages:", newPages)
				if err != nil {
					fmt.Println("Error fetching more pages:", err)
					continue
				}
				scrapeRequests = append(scrapeRequests, newPages...)
			}

			for _, req := range scrapeRequests {
				GetPage(collector, req)
			}

			fmt.Println("Waiting for scrape requests to finish...")
			collector.Wait()

			scrapeRequests = []DTOCrawlRequest{}
		}
	}
}

func setupDTOChannelReader(channel chan *DTOCrawlerData) {
	go func() {
		var scrapeData []DTOCrawlerData

		for dto := range channel {
			scrapeData = append(scrapeData, *dto)

			if len(scrapeData) >= PagesPerSubmission {
				fmt.Printf("Sending %s submissions to backend...\n", PagesPerSubmission)
				resp, err := SendPagesToBackend(BackendUrl, scrapeData)

				if err != nil {
					fmt.Println("Error sending submissions to backend:", err)

					if resp != "" {
						fmt.Println("Response:", resp)
					}
				}
				scrapeData = scrapeData[:0]
			}
		}
	}()
}
