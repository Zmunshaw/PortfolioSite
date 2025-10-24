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

type CrawlReqRoot struct {
	ID     string            `json:"$id"`
	Values []DTOCrawlRequest `json:"$values"`
}

func RequestMorePages() ([]DTOCrawlRequest, error) {
	req, err := http.NewRequest("GET", BackendUrl+"/"+BackendCrawlerPath+"/"+BackendScrapePath, nil)
	if err != nil {
		return []DTOCrawlRequest{}, err
	}

	client := &http.Client{
		Timeout: 10 * time.Second,
		Transport: &http.Transport{
			TLSClientConfig: &tls.Config{
				InsecureSkipVerify: true,
			},
		},
	}

	resp, err := client.Do(req)
	if err != nil {
		fmt.Println(err)
		return []DTOCrawlRequest{}, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		fmt.Println(resp.Status)
		return []DTOCrawlRequest{}, fmt.Errorf("request failed with status: %s, on %s", resp.Status, resp.Request.URL.String())
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		fmt.Println(err)
		return []DTOCrawlRequest{}, err // Return nil and the error if reading body fails
	}

	var root CrawlReqRoot
	err = json.Unmarshal(body, &root)
	if err != nil {
		fmt.Println(err)
		return []DTOCrawlRequest{}, err
	}

	fmt.Println(root, string(body))

	var requests = root.Values

	for _, req := range requests {
		fmt.Printf("New Request with PID %d and URL %s\n", req.PageID, req.URL)
	}
	return requests, nil
}

func SendPagesToBackend(backendURL string, pageDTOs []DTOCrawlerData) (string, error) {
	return postToBackend(backendURL, pageDTOs)
}

func postToBackend(backendURL string, pages []DTOCrawlerData) (string, error) {
	dataToSend, err := json.Marshal(pages)
	if err != nil {
		fmt.Println(err)
		return "", err
	}

	fmt.Println("Dest:", backendURL+"/"+BackendCrawlerPath+"/"+BackendScrapePath)
	//fmt.Println("Sending", string(dataToSend))
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
