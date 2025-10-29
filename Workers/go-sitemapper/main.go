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

	http.HandleFunc("/hello", func(w http.ResponseWriter, req *http.Request) {
		msg := "Hello!"
		w.Write([]byte(msg))
	})
	http.HandleFunc("/echo", func(w http.ResponseWriter, req *http.Request) {
		b, err := io.ReadAll(req.Body)
		if err != nil {
			panic(err)
		}
		io.Copy(w, bytes.NewReader(b))
	})
	workers.Serve(nil) // use http.DefaultServeMux
}
