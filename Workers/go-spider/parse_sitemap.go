package main

import (
	"bytes"
	"encoding/xml"
	"fmt"
)

func ParseSitemap(sitemapURL string, sitemapData []byte) (Sitemap, error) {
	sitemap := Sitemap{Hostname: sitemapURL}

	decoder := xml.NewDecoder(bytes.NewReader(sitemapData))
	decoder.Strict = false
	if err := decoder.Decode(&sitemap.SiteIndex); err == nil && len(sitemap.SiteIndex.Sitemap) > 0 {
		fmt.Println("Sitemap index parsed from sitemap")
	}

	if err := decoder.Decode(&sitemap.UrlSet); err == nil && len(sitemap.UrlSet.URL) > 0 {
		fmt.Printf("URL Set found with %d\n", len(sitemap.UrlSet.URL))
	}

	return sitemap, nil
}
