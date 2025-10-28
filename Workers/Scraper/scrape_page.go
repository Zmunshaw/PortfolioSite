package main

import (
	"fmt"
	"net"
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

// TODO IMRPOVE: Data recieved, specifically text borders are being noisy garbage, need to extract more "meaning" and less noise.
func SetupCollector(dataChan chan *DTOCrawlerData) *colly.Collector {
	c := colly.NewCollector()

	c.WithTransport(&http.Transport{
		Proxy: http.ProxyFromEnvironment,
		DialContext: (&net.Dialer{
			Timeout:   3 * time.Second, // Timeout for establishing a connection
			KeepAlive: 30 * time.Second,
		}).DialContext,
		MaxIdleConns:          100,
		IdleConnTimeout:       100 * time.Second, // Timeout for idle connections
		TLSHandshakeTimeout:   10 * time.Second,  // Timeout for TLS handshake
		ExpectContinueTimeout: 1 * time.Second,
	})

	c.Limit(&colly.LimitRule{
		DomainGlob:  "*", // Apply to all domains
		Parallelism: Parallelism,
	})

	c.OnRequest(func(r *colly.Request) {
		for key, value := range CrawlerHeaders {
			r.Headers.Set(key, value)
		}
	})

	c.OnResponse(func(r *colly.Response) {
		if r.StatusCode == http.StatusOK {
			fmt.Printf("Scraping page %s\n", r.Request.URL)

			bodyStr := string(r.Body)
			blockingKeywords := []string{
				"captcha",
				"access denied",
				"blocked",
				"too many requests",
				"rate limit",
				"suspicious activity",
			}

			for _, keyword := range blockingKeywords {
				if strings.Contains(strings.ToLower(bodyStr), strings.ToLower(keyword)) {
					fmt.Printf("Blocked on %s by %s\n", r.Request.URL.String(), keyword)
					r.Request.Abort()
				}
			}
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

	c.OnHTML("main, body", func(e *colly.HTMLElement) {
		e.DOM.Find("script, style, nav, footer, aside, header, code, button, [role='button']").Remove()
		if e.Text == "" {
			fmt.Println("Content is empty on", e.Request.URL.String())
		}

		var currText = e.Response.Ctx.Get("content")
		if currText != "" {
			currText += "\n"
		}
		currText += e.Text
		e.Response.Ctx.Put("content", currText)
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
	fmt.Println("sanitizing text:", text)
	htmlRegEx := regexp.MustCompile("<(.*?)>")
	whitespaceRegEx := regexp.MustCompile(`\s+`) // \s+ apparently means whitespace chars e.g., "\r", " ", "\n", etc

	sanitizedText := htmlRegEx.ReplaceAllString(text, " ")
	sanitizedText = whitespaceRegEx.ReplaceAllString(sanitizedText, " ")

	sanitizedText = strings.TrimSpace(sanitizedText)
	fmt.Println(sanitizedText)

	return sanitizedText
}
