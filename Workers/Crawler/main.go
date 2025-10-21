package main

import (
	"bytes"
	"encoding/csv"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"sync"
	"time"

	"github.com/gocolly/colly/v2"
)

const WorkerCount = 16

func main() {
	file, err := os.Open("seed.csv")
	if err != nil {
		log.Fatal("Error open file:", err)
	}
	defer file.Close()

	reader := csv.NewReader(file)
	records, err := reader.ReadAll()
	if err != nil {
		log.Fatal("Error read CSV:", err)
	}

	jobChan := make(chan []string)
	var wg sync.WaitGroup
	for i := 0; i < WorkerCount; i++ {
		go func(workerID int) {
			for row := range jobChan {
				processRow(row)
				wg.Done()
			}
		}(i)
	}

	for i, row := range records {
		if i == 0 || len(row) < 2 {
			continue
		}
		wg.Add(1)
		jobChan <- row
	}

	wg.Wait()
	close(jobChan)

	fmt.Println("All jobs completed.")
}

func processRow(row []string) {
	domain := row[1]
	fmt.Println("Processing domain:", domain)

	c := colly.NewCollector(
		colly.Async(true),
		colly.UserAgent("Mozilla/5.0 (compatible; MySitemapBot/1.0; +http://adsasd.com/bot)"),
	)

	c.Limit(&colly.LimitRule{
		DomainGlob:  "*",
		Parallelism: 5,
		Delay:       1 * time.Second,
	})

	sitemapURL, err := FindSitemap("https://" + domain)
	if err != nil {
		log.Println("Error finding sitemap:", domain, err)
		return
	}

	sitemap, err := ParseSitemap(sitemapURL, c)
	if err != nil {
		log.Println("Error parsing sitemap:", domain, err)
		return
	}

	fmt.Println("Parsed sitemap for:", sitemap.Hostname)

	dataToSend := TransformToBackendModel(sitemap)

	jsonData, err := json.Marshal(dataToSend)
	if err != nil {
		log.Println("JSON marshal error:", err)
		return
	}

	url := "http://localhost:1234/indexer/submit-sitemap" // TODO: Extract Vars
	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		log.Println("Request creation error:", err)
		return
	}

	req.Header.Set("Content-Type", "application/json")
	client := &http.Client{}

	resp, err := client.Do(req)
	if err != nil {
		log.Println("HTTP request error:", err)
		return
	}
	defer resp.Body.Close()

	bodyBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		log.Println("Read response error:", err)
		return
	}

	fmt.Printf("Domain: %s | Status: %s | Response: %s\n", domain, resp.Status, string(bodyBytes))
}
