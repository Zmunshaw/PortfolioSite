// main.go
package main

import (
	"fmt"
	"net/http"

	"github.com/syumai/workers" // A Go package for Cloudflare Workers
)

func main() {
	http.HandleFunc("/hello", func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, "Hello from Go on Cloudflare Workers!")
	})
	workers.Serve(nil) // Use http.DefaultServeMux
}
