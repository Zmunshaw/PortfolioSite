package main

import (
	"fmt"

	"github.com/gocolly/colly/v2"
)

type PageData struct {
	WebsiteID int
	Title     string
	Content   string
}

func GetPage(c *colly.Collector, urlToScrape string, siteID int) (error, PageData) {
	pageData := PageData{
		WebsiteID: siteID,
	}

	c.OnHTML("title", func(e *colly.HTMLElement) {
		fmt.Println("Title:", e.Text)
		pageData.Title = e.Text
	})

	c.OnHTML("article, div#content, main", func(e *colly.HTMLElement) {
		fmt.Println("Content:\n", e.Text)
		pageData.Content = e.Text
	})

	c.OnError(func(r *colly.Response, err error) {

	})

	urlToScrape = "https://" + urlToScrape
	err := c.Visit(urlToScrape)
	if err != nil {
		return err, pageData
	}

	return nil, pageData
}
