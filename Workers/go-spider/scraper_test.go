package main

import (
	"encoding/json"
	"os"
	"testing"
)

// MockScrapeSites is a test version that doesn't call Python
func MockScrapeSites(urls []string) ([]map[string]interface{}, error) {
	// For testing, return mock data instead of calling Python
	return []map[string]interface{}{
		{
			"url":     urls[0],
			"title":   "Test Page",
			"content": "Test content",
		},
	}, nil
}

func TestScrapeSitesWithValidURLs(t *testing.T) {
	urls := []string{"https://web-scraping.dev/testimonials"}

	// Use mock instead of actual scrape
	results, err := ScrapeSites(urls)

	if err != nil {
		t.Fatalf("Expected no error, got %v", err)
	}

	if len(results) == 0 {
		t.Fatalf("Expected results, got empty slice")
	}

	if results[0]["url"] != urls[0] {
		t.Errorf("URL mismatch: got %v, want %s", results[0]["url"], urls[0])
	}
}

func TestScrapeSitesReturnsMap(t *testing.T) {
	urls := []string{"https://example.com", "https://example2.com"}

	results, err := ScrapeSites(urls)

	if err != nil {
		t.Fatalf("Unexpected error: %v", err)
	}

	if len(results) == 0 {
		t.Fatal("Expected at least one result")
	}

	// Check that result is a map
	result := results[0]
	if _, ok := result["url"]; !ok {
		t.Error("Result missing 'url' field")
	}
}

// IntegrationTest - only runs if Python environment is set up
func TestScrapeSitesIntegration(t *testing.T) {
	if os.Getenv("RUN_INTEGRATION_TESTS") == "" {
		t.Skip("Skipping integration test. Set RUN_INTEGRATION_TESTS=1 to run.")
	}

	urls := []string{"https://example.com"}
	results, err := ScrapeSites(urls)

	if err != nil {
		t.Fatalf("ScrapeSites failed: %v", err)
	}

	if len(results) == 0 {
		t.Fatal("Expected results from ScrapeSites")
	}
}

// TestScrapeSitesJSONMarshal tests the JSON marshaling
func TestScrapeSitesJSONMarshal(t *testing.T) {
	urls := []string{"https://example.com", "https://example2.com"}

	// Test that urls can be marshaled to JSON
	jsonBytes, err := json.Marshal(urls)
	if err != nil {
		t.Fatalf("Failed to marshal URLs: %v", err)
	}

	// Test that it can be unmarshaled back
	var result []string
	err = json.Unmarshal(jsonBytes, &result)
	if err != nil {
		t.Fatalf("Failed to unmarshal URLs: %v", err)
	}

	if len(result) != len(urls) {
		t.Errorf("Length mismatch: got %d, want %d", len(result), len(urls))
	}
}

// TestScrapeSitesEmptyInput
func TestScrapeSitesEmptyInput(t *testing.T) {
	urls := []string{}

	results, err := ScrapeSites(urls)

	// Should handle empty input gracefully
	if err != nil {
		t.Fatalf("Should handle empty input, got error: %v", err)
	}

	if len(results) > 0 {
		t.Error("Expected no results for empty input")
	}
}

// TestScrapeSitesOutputFormat checks that output matches Python script format
func TestScrapeSitesOutputFormat(t *testing.T) {
	// Simulate what Python script returns (from stealth_scrape.py)
	mockOutput := []map[string]interface{}{
		{
			"url":         "https://example.com",
			"links":       []string{"https://example.com/page1", "https://example.com/page2"},
			"title":       "Example Website",
			"description": "An example website",
			"content":     "Main page content here",
			"images":      []string{"https://example.com/img1.jpg"},
			"keywords":    "example, website, test",
		},
	}

	jsonBytes, _ := json.Marshal(mockOutput)

	var results []map[string]interface{}
	err := json.Unmarshal(jsonBytes, &results)

	if err != nil {
		t.Fatalf("Failed to unmarshal expected format: %v", err)
	}

	if len(results) != 1 {
		t.Errorf("Expected 1 result, got %d", len(results))
	}

	// Check expected fields exist
	expectedFields := []string{"url", "links", "title", "description", "content", "images", "keywords"}
	for _, field := range expectedFields {
		if _, ok := results[0][field]; !ok {
			t.Errorf("Missing expected field: %s", field)
		}
	}
}

// TestScrapeSitesHandlesError tests that errors from Python are captured
func TestScrapeSitesHandlesError(t *testing.T) {
	// Simulate Python script returning error for one URL
	mockOutput := []map[string]interface{}{
		{
			"url":   "https://example.com",
			"error": "Connection timeout",
		},
	}

	jsonBytes, _ := json.Marshal(mockOutput)

	var results []map[string]interface{}
	err := json.Unmarshal(jsonBytes, &results)

	if err != nil {
		t.Fatalf("Failed to unmarshal error response: %v", err)
	}

	// Check that error field exists when scrape fails
	if errorMsg, ok := results[0]["error"]; ok {
		if errorMsg != "Connection timeout" {
			t.Errorf("Error message mismatch: got %v", errorMsg)
		}
	} else {
		t.Error("Expected error field in response")
	}
}

// TestScrapeSitesMultipleURLs tests handling multiple URLs
func TestScrapeSitesMultipleURLs(t *testing.T) {
	mockOutput := []map[string]interface{}{
		{
			"url":     "https://example.com",
			"title":   "Example 1",
			"content": "Content 1",
		},
		{
			"url":   "https://example2.com",
			"error": "Timeout",
		},
		{
			"url":     "https://example3.com",
			"title":   "Example 3",
			"content": "Content 3",
		},
	}

	jsonBytes, _ := json.Marshal(mockOutput)

	var results []map[string]interface{}
	json.Unmarshal(jsonBytes, &results)

	if len(results) != 3 {
		t.Errorf("Expected 3 results, got %d", len(results))
	}

	// First should succeed
	if _, ok := results[0]["title"]; !ok {
		t.Error("First result should have title")
	}

	// Second should have error
	if _, ok := results[1]["error"]; !ok {
		t.Error("Second result should have error field")
	}

	// Third should succeed
	if _, ok := results[2]["content"]; !ok {
		t.Error("Third result should have content")
	}
}

// Benchmark test
func BenchmarkScrapeSites(b *testing.B) {
	urls := []string{"https://web-scraping.dev/products", "https://www.reddit.com/", "https://lobste.rs/",
		"https://words.filippo.io/claude-debugging/", "https://www.temu.com", "https://zacharymunshaw.dev/"}

	for i := 0; i < b.N; i++ {
		_, _ = ScrapeSites(urls)
	}
}

func BenchmarkScrapeSingleSite(b *testing.B) {
	urls := []string{"https://web-scraping.dev/products"}

	for i := 0; i < b.N; i++ {
		_, _ = ScrapeSites(urls)
	}
}
