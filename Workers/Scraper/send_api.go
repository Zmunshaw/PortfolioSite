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

type DTOPageScrapeData struct {
	ContentID int `json:"websiteID"`

	Title    string    `json:"title"`
	Text     string    `json:"text"`
	CrawTime time.Time `json:"crawTime"`
}

func RequestMorePages() (PageScrapeRequest, error) {
	// Create the request object
	req, err := http.NewRequest("GET", BackendUrl+"/"+BackendCrawlerPath+"/"+BackendScrapePath, nil)
	if err != nil {
		return PageScrapeRequest{}, err
	}

	// Initialize client with timeout and TLS config
	client := &http.Client{
		Timeout: 10 * time.Second, // Set a timeout as necessary
		Transport: &http.Transport{
			TLSClientConfig: &tls.Config{
				InsecureSkipVerify: true, // Disables certificate validation
			},
		},
	}

	// Make the request
	resp, err := client.Do(req)
	if err != nil {
		fmt.Println(err)
		return PageScrapeRequest{}, err
	}
	defer resp.Body.Close()

	// Check for non-OK status code
	if resp.StatusCode != http.StatusOK {
		fmt.Println(resp.Status)
		return PageScrapeRequest{}, fmt.Errorf("request failed with status: %s, on %s", resp.Status, resp.Request.URL.String())
	}

	// Read the response body
	body, err := io.ReadAll(resp.Body)
	if err != nil {
		fmt.Println(err)
		return PageScrapeRequest{}, err // Return nil and the error if reading body fails
	}

	// Initialize the root object to unmarshal the response
	var root Root
	err = json.Unmarshal(body, &root)
	if err != nil {
		fmt.Println(err)
		return PageScrapeRequest{}, err
	}

	// Print for debugging purposes (optional)
	fmt.Println(root, string(body))

	// Create a PageScrapeRequest and assign the root to Pages
	var requests PageScrapeRequest
	requests.Pages = root

	return requests, nil
}

func SendPagesToBackend(backendURL string, pageData []PageData) (string, error) {
	pageDTOs := []DTOPageScrapeData{}

	for _, page := range pageData {
		pageDTOs = append(pageDTOs, DTOPageScrapeData{page.Content.ContentID,
			*page.Content.Text,
			*page.Content.Text,
			*page.LastCrawled})
	}
	return postToBackend(backendURL, pageDTOs)
}

func postToBackend(backendURL string, pages []DTOPageScrapeData) (string, error) {
	dataToSend, err := json.Marshal(pages)
	if err != nil {
		fmt.Println(err)
		return "", err
	}

	fmt.Println("Dest:", backendURL+"/"+BackendCrawlerPath+"/"+BackendScrapePath)
	fmt.Println("Sending", string(dataToSend))
	req, err := http.NewRequest("POST", backendURL+"/"+BackendCrawlerPath+"/"+BackendScrapePath, bytes.NewBuffer(dataToSend))
	if err != nil {
		fmt.Println(err)
		return "", err
	}

	req.Header.Set("Content-Type", "application/json")
	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		fmt.Println(err)
		return "", err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		fmt.Println(err)
	}
	fmt.Println(err, string(body))
	return string(body), err
}
