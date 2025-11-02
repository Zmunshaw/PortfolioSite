package main

import (
	"bytes"
	"fmt"
	"io"
	"net/http"

	"github.com/syumai/workers"
	"github.com/syumai/workers/cloudflare"
)

func main() {
	apiKey := cloudflare.Getenv("API_KEY")
	backendURL := cloudflare.Getenv("BACKEND_URL")
	fmt.Printf("API Key: %s\n", apiKey)
        // fmt
	http.HandleFunc("/scrape", func(w http.ResponseWriter, req *http.Request) {
		msg := "This should scrape!"
		w.Write([]byte(msg))
	})
	http.HandleFunc("/map", func(w http.ResponseWriter, req *http.Request) {
		msg := "This should sitemap!"
		w.Write([]byte(msg))
	})
	workers.Serve(nil) // use http.DefaultServeMux
}
