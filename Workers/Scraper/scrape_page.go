package main

import (
	"fmt"
	"net/http"
	"regexp"
	"strings"
	"time"

	"github.com/gocolly/colly/v2"
)

func GetPage(c *colly.Collector, scrapeTarget DTOCrawlRequest) {
	ctx := colly.NewContext()
	ctx.Put("pageID", scrapeTarget.PageID)
	ctx.Put("url", scrapeTarget.URL)

	err := c.Request("GET", "http://"+scrapeTarget.URL, nil, ctx, nil)
	if err != nil {
		fmt.Println(err)
	}
}

func SetupCollector(dataChan chan *DTOCrawlerData) *colly.Collector {
	c := colly.NewCollector()

	c.Limit(&colly.LimitRule{
		DomainGlob:  "*", // Apply to all domains
		Parallelism: Parallelism,
	})

	c.OnResponse(func(r *colly.Response) {
		if r.StatusCode == http.StatusOK {
			fmt.Printf("Scraping page %s\n", r.Request.URL)
		} else {
			r.Request.Abort()
		}
	})

	c.OnHTML("title", func(e *colly.HTMLElement) {
		if e.Text == "" {
			fmt.Println("Title is empty on", e.Request.URL.String())
		}
		e.Response.Ctx.Put("title", sanitizeText(e.Text))
	})

	c.OnHTML("article, div#content, main", func(e *colly.HTMLElement) {
		if e.Text == "" {
			fmt.Println("Content is empty on", e.Request.URL.String())
		}
		e.Response.Ctx.Put("content", sanitizeText(e.Text))
	})

	c.OnError(func(r *colly.Response, err error) {
		fmt.Println("On Error Triggered:", err)
	})

	c.OnScraped(func(r *colly.Response) {
		pageID, ok := r.Ctx.GetAny("pageID").(int)
		if !ok {
			fmt.Println("Error: pageID is not an int")
			return
		}

		dtoScrapeObject := DTOCrawlerData{
			PageID:    pageID,
			URL:       r.Ctx.Get("url"),
			Title:     r.Ctx.Get("title"),
			Text:      r.Ctx.Get("content"),
			CrawledAt: time.Now().UTC(),
		}
		fmt.Printf("Scrape complete on %s\n", r.Request.URL)
		dataChan <- &dtoScrapeObject
	})

	return c
}

func sanitizeText(text string) string {
	htmlRegEx := regexp.MustCompile("<(.*?)>")
	whitespaceRegEx := regexp.MustCompile(`\s+`) // \s+ apparently means whitespace chars e.g., "\r", " ", "\n", etc

	sanitizedText := htmlRegEx.ReplaceAllString(text, " ")
	sanitizedText = whitespaceRegEx.ReplaceAllString(sanitizedText, " ")

	sanitizedText = strings.TrimSpace(sanitizedText)
	fmt.Println(sanitizedText)

	return sanitizedText
}
