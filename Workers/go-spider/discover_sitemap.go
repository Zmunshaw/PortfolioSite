package main

import (
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

	if resp != nil && len(resp) > 0 {
		return resp, nil
	}

	return nil, fmt.Errorf("FindSitemap failed: no sitemap found for url: %s", baseURL)
}

func GetSitemap(baseURL string) ([]byte, error) {
	resp, err := http.Get(baseURL)
	if err != nil {
		return nil, fmt.Errorf("get sitemap failed: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("get sitemap failed: status code %d", resp.StatusCode)
	}

	return io.ReadAll(resp.Body)
}

func checkRobots(baseURL string) (string, error) {
	resp, err := http.Get(baseURL + "/" + "robots.txt")
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return "", err
	}

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
	for _, path := range commonSitemaps {
		fullURL := strings.TrimRight(baseURL, "/") + path
		resp, err := GetSitemap(fullURL)

		if err == nil && resp != nil && len(resp) > 0 {
			return resp, nil
		}
	}

	return nil, fmt.Errorf("checkMostCommonConfigs: no sitemap found at common locations for %s", baseURL)
}
