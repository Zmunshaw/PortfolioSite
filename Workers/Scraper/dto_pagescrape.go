package main

type DTOCrawlRequest struct {
	PageID int    `json:"pageId"`
	URL    string `json:"url"`
}
