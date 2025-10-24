package main

import "time"

type DTOCrawlerData struct {
	PageID int `json:"pageId"`

	URL   string `json:"pageUrl"`
	Title string `json:"title"`
	Text  string `json:"text"`

	CrawledAt time.Time `json:"crawledAt"`
}
