package main

import (
	"fmt"

	"github.com/syumai/workers/cloudflare"
)

func SendSitemap(sitemap Sitemap) {
	dtoSitemap := TransformToBackendModel(sitemap)
	apiKey := cloudflare.Getenv("API_KEY")
	fmt.Println(apiKey)

	fmt.Println(dtoSitemap)
}
