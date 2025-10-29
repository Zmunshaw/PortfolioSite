package main

import (
	"errors"
	"fmt"
	"io"
	"net/http"
	"strings"
)

var commonSitemaps = []string{
	"/sitemap.xml",
	"/sitemap_index.xml",
	"/sitemap/sitemap.xml",
	"/sitemap-index.xml",
	"/sitemaps.xml",
}

func FindSitemap(baseURL string) ([]byte, error) {
	sitemapUrl, err := checkRobots(baseURL)
	if err != nil {
		return nil, fmt.Errorf("sitemap url check failed: %v", err)
	}
	if sitemapUrl != "" {
		return GetSitemap(sitemapUrl)
	}

	resp, err := checkMostCommonConfigs(baseURL)
	if err != nil {
		return nil, err
	}

	if resp != nil {
		return resp, nil
	} else {
		return nil, fmt.Errorf("FindSitemap ended without any response found url: %s, si", baseURL)
	}
}

func GetSitemap(baseURL string) ([]byte, error) {
	resp, err := http.Head(baseURL)
	if err != nil || resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("Get sitemap failed: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusOK {
		return io.ReadAll(resp.Body)
	} else {
		return nil, fmt.Errorf("Get sitemap failed: %v", resp.StatusCode)
	}
}

func checkRobots(baseURL string) (string, error) {
	resp, err := http.Get(baseURL + "/" + "robots.txt")
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	lines := strings.Split(string(body), "\n")
	for _, line := range lines {
		if strings.HasPrefix(strings.ToLower(line), "sitemap:") {
			sitemapURL := strings.TrimSpace(strings.TrimPrefix(line, "Sitemap:"))
			return sitemapURL, nil
		}
	}
	return "", nil
}

func checkMostCommonConfigs(baseURL string) ([]byte, error) {
	var err error
	var resp []byte

	for _, path := range commonSitemaps {
		fullURL := strings.TrimRight(baseURL, "/") + path
		resp, err = GetSitemap(fullURL)

		if err != nil {
			err = fmt.Errorf("sitemap url check failed: %v", err)
		} else if resp != nil && len(resp) > 0 {
			return resp, err
		}
	}

	if err == nil && resp == nil || len(resp) == 0 {
		err = errors.New("checkMostCommonConfigs failed with unknown error")
	}
	return nil, err
}
