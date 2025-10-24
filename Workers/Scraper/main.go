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
