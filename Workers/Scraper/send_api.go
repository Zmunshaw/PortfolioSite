package main

import (
	"bytes"
	"crypto/tls"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"time"
)

func RequestMorePages() (PageScrapeRequest, error) {
	req, err := http.NewRequest("GET", BackendUrl+"/"+BackendCrawlerPath+"/"+BackendScrapePath, nil)
	if err != nil {
		return PageScrapeRequest{}, err
	}

	client := &http.Client{
		Timeout: 10 * time.Second, // Set a timeout as necessary
		Transport: &http.Transport{
			TLSClientConfig: &tls.Config{
				InsecureSkipVerify: true, // Disables certificate validation
			},
		},
	}
	resp, err := client.Do(req)
	if err != nil {
		return PageScrapeRequest{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return PageScrapeRequest{}, fmt.Errorf("request failed with status: %s", resp.Status,
			"on", resp.Request.URL.String())
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return PageScrapeRequest{}, err // Return nil and the error if reading body fails
	}

	var requests PageScrapeRequest
	err = json.Unmarshal(body, &requests)
	if err != nil {
		return PageScrapeRequest{}, err
	}

	return requests, nil
}

func SendPagesToBackend(backendURL string, pageData []PageData) (string, error) {
	var validPages []PageData

	for _, page := range pageData {
		if validatePage(&page) == false {
			fmt.Println("Invalid page", page.Title)
		} else {
			validPages = append(validPages, page)
		}
	}

	return postToBackend(backendURL, validPages)
}

func validatePage(page *PageData) bool {
	if page.PageURL == "" {
		return false
	}

	if page.Title == "" {
		page.Title = "MISSING"
	}

	if page.Text == "" {
		return false
	}

	return true
}

func postToBackend(backendURL string, pages []PageData) (string, error) {
	dataToSend, err := json.Marshal(pages)
	if err != nil {
		return "", err
	}

	req, err := http.NewRequest("POST", backendURL, bytes.NewBuffer(dataToSend))
	if err != nil {
		return "", err
	}

	req.Header.Set("Content-Type", "application/json")
	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)

	return string(body), err
}
