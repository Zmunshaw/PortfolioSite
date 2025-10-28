package main

import (
	"errors"
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

func FindSitemap(baseURL string) (string, error) {
	var sitemap string
	sitemap, err := checkRobots(baseURL)
	if err == nil && sitemap != "" {
		return sitemap, nil
	}

	sitemap, err = checkMostCommonConfigs(baseURL)
	if err == nil && sitemap != "" {
		return sitemap, nil
	}

	return "", errors.New("sitemap not found")
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
	return "", errors.New("robots.txt not found")
}

func checkMostCommonConfigs(baseURL string) (string, error) {
	for _, path := range commonSitemaps {
		fullURL := strings.TrimRight(baseURL, "/") + path
		resp, err := http.Head(fullURL)
		if err == nil && resp.StatusCode == http.StatusOK {
			return fullURL, nil
		}
	}
	return "", errors.New("no common sitemap found")
}
