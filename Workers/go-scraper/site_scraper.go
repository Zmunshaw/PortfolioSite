package main

import (
	"bytes"
	"encoding/xml"
	"fmt"
	"sync"

	"github.com/gocolly/colly"
)

// ParseSitemap recursively fetches sitemaps -- NO LIMIT ON RECURSION
func ParseSitemap(url string, data []data) (Sitemap, error) {
	sitemap := Sitemap{Hostname: url}
	var fetchErr error	
	
	if (url.contains(".gz"))
	    
	
	if err != nil {
		return sitemap, fmt.Errorf("colly visit error: %w", err)
	}

	if fetchErr != nil {
		return sitemap, fmt.Errorf("fetch error: %w", fetchErr)
	}

	decoder := xml.NewDecoder(bytes.NewReader(data))
	decoder.Strict = false
	if err := decoder.Decode(&sitemap.SiteIndex); err == nil && len(sitemap.SiteIndex.Sitemap) > 0 {
		fmt.Println("Sitemap Index found:", url)

		for _, s := range sitemap.SiteIndex.Sitemap {
			fmt.Println(" >", s.Loc)
			childSitemap, err := ParseSitemap(s.Loc, col)
			if err != nil {
				fmt.Printf("Failed to parse child sitemap %s: %v\n", s.Loc, err)
				continue
			}
			sitemap.Sitemaps = append(sitemap.Sitemaps, childSitemap)
		}
		return sitemap, nil
	}

	decoder = xml.NewDecoder(bytes.NewReader(data))
	decoder.Strict = false
	if err := decoder.Decode(&sitemap.UrlSet); err == nil && len(sitemap.UrlSet.URL) > 0 {
		fmt.Printf("URL Set found with %d URLs: %s\n", len(sitemap.UrlSet.URL), url)
		return sitemap, nil
	}

	return sitemap, fmt.Errorf("unable to parse sitemap or urlset: %s", url)
}
