package main

import (
	"fmt"
	"net/http"
	"time"

	"github.com/gocolly/colly/v2"
)

func GetPage(c *colly.Collector, scrapeTarget string, dataChan chan *DTOPageScrapeData) error {
	dtoScrapeObject := &DTOPageScrapeData{}

	c.OnResponse(func(r *colly.Response) {
		if r.StatusCode == http.StatusOK {
			fmt.Printf("Scraping page %s\n", scrapeTarget)
		}
	})

	c.OnHTML("title", func(e *colly.HTMLElement) {
		if e.Text == "" {
			fmt.Println("Title is empty on", e.Request.URL.String())
		}
		dtoScrapeObject.Title = e.Text
	})

	c.OnHTML("article, div#content, main", func(e *colly.HTMLElement) {
		if e.Text == "" {
			fmt.Println("Content is empty on", e.Request.URL.String())
		}
		dtoScrapeObject.Text = e.Text
	})

	c.OnError(func(r *colly.Response, err error) {
		fmt.Println("OnError:", r.StatusCode)
		fmt.Println(err)
	})

	c.OnScraped(func(r *colly.Response) {
		dtoScrapeObject.CrawTime = time.Now().UTC()
		dataChan <- dtoScrapeObject
	})

	urlToScrape := "https://" + scrapeTarget

	err := c.Visit(urlToScrape)
	if err != nil {
		return err
	}

	return nil
}
