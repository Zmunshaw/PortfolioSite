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
				err := processRow(row)
				if err != nil {
					fmt.Println(err)
				}
				wg.Done()
			}
		}(i)
	}

	for _, row := range records {
		wg.Add(1)
		jobChan <- row
	}

	wg.Wait()
	close(jobChan)

	fmt.Println("All jobs completed.")
}

func processRow(row []string) error {
	domain := row[0]
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
		return fmt.Errorf("Error finding sitemap:", domain, err)
	}

	sitemap, err := ParseSitemap(sitemapURL, c)
	if err != nil {
		return fmt.Errorf("Error parsing sitemap:", domain, err)
	}

	fmt.Println("Parsed sitemap for:", sitemap.Hostname)

	dataToSend := TransformToBackendModel(sitemap)

	jsonData, err := json.Marshal(dataToSend)
	if err != nil {
		return fmt.Errorf("JSON marshal error:", err)
	}

	url := "http://localhost:1234/indexer/submit-sitemap" // TODO: Extract Vars
	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return fmt.Errorf("Request creation error:", err)
	}

	req.Header.Set("Content-Type", "application/json")
	client := &http.Client{}

	resp, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("HTTP request error:", err)
	}
	defer resp.Body.Close()

	bodyBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		return fmt.Errorf("Read response error:", err)
	}

	fmt.Printf("Domain: %s | Status: %s | Response: %s\n", domain, resp.Status, string(bodyBytes))
	return nil
}
