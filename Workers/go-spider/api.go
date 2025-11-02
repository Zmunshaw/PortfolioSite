package main

import (
	"fmt"
)

func SendSitemap(sitemap Sitemap, apiKey string, backendURL string) {
	dtoSitemap := TransformToBackendModel(sitemap)
	fmt.Println("SEND SITEMAP NOT IMPLEMENTED")
	fmt.Println(dtoSitemap)
}
