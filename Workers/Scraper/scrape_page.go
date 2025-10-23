package main

import (
	"fmt"

	"github.com/gocolly/colly/v2"
)

type PageData struct {
	PageURL string `json:"url"`
	Title   string `json:"title"`
	Text    string `json:"text"`
}

func GetPage(c *colly.Collector, urlToScrape string, scrapedPagesData *ScrapedPagesData) error {
	pageData := PageData{
		PageURL: urlToScrape,
	}

	c.OnHTML("title", func(e *colly.HTMLElement) {
		fmt.Println("Title:", e.Text)
		pageData.Title = e.Text
	})

	c.OnHTML("article, div#content, main", func(e *colly.HTMLElement) {
		fmt.Println("Content:\n", e.Text)
		pageData.Text = e.Text
	})

	c.OnError(func(r *colly.Response, err error) {

	})

	urlToScrape = "https://" + urlToScrape
	err := c.Visit(urlToScrape)
	if err != nil {
		return err
	}

	scrapedPagesData.ScrapedPageMu.Lock()
	defer scrapedPagesData.ScrapedPageMu.Unlock()
	scrapedPagesData.ScrapedPages = append(scrapedPagesData.ScrapedPages, pageData)
	return nil
}
