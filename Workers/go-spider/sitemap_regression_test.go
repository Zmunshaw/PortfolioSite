package main

import (
	"encoding/json"
	"testing"
	"time"
)

func TestSitemapStructureIntegrity(t *testing.T) {
	tests := []struct {
		name     string
		sitemap  BackendSitemap
		validate func(t *testing.T, s BackendSitemap)
	}{
		{
			name: "Basic sitemap with URLs",
			sitemap: BackendSitemap{
				Location:     "https://example.com/sitemap.xml",
				IsMapped:     true,
				LastModified: time.Now(),
				UrlSet: []BackendUrl{
					{Location: "https://example.com/page1", Priority: 0.8},
					{Location: "https://example.com/page2", Priority: 0.5},
				},
			},
			validate: func(t *testing.T, s BackendSitemap) {
				if len(s.UrlSet) != 2 {
					t.Errorf("Expected 2 URLs, got %d", len(s.UrlSet))
				}
				if s.UrlSet[0].Location != "https://example.com/page1" {
					t.Errorf("URL location mismatch")
				}
			},
		},
		{
			name: "Nested sitemap index",
			sitemap: BackendSitemap{
				Location:     "https://example.com/sitemap_index.xml",
				IsMapped:     false,
				LastModified: time.Now(),
				SitemapIndex: []BackendSitemap{
					{Location: "https://example.com/sitemap1.xml", IsMapped: true},
					{Location: "https://example.com/sitemap2.xml", IsMapped: true},
				},
			},
			validate: func(t *testing.T, s BackendSitemap) {
				if len(s.SitemapIndex) != 2 {
					t.Errorf("Expected 2 sitemaps, got %d", len(s.SitemapIndex))
				}
			},
		},
		{
			name: "URL with media entries",
			sitemap: BackendSitemap{
				Location: "https://example.com/sitemap.xml",
				UrlSet: []BackendUrl{
					{
						Location: "https://example.com/video",
						Priority: 0.9,
						Media: []BackendMediaEntry{
							{Location: "https://example.com/video.mp4", Type: Video},
							{Location: "https://example.com/thumb.jpg", Type: Image},
						},
					},
				},
			},
			validate: func(t *testing.T, s BackendSitemap) {
				if len(s.UrlSet[0].Media) != 2 {
					t.Errorf("Expected 2 media entries, got %d", len(s.UrlSet[0].Media))
				}
			},
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			data, err := json.Marshal(tt.sitemap)
			if err != nil {
				t.Fatalf("Marshal failed: %v", err)
			}

			var result BackendSitemap
			err = json.Unmarshal(data, &result)
			if err != nil {
				t.Fatalf("Unmarshal failed: %v", err)
			}

			tt.validate(t, result)
		})
	}
}

func TestMediaEntryAllTypes(t *testing.T) {
	mediaTypes := []MediaType{Image, Video, News}

	for _, mt := range mediaTypes {
		t.Run(string(mt), func(t *testing.T) {
			entry := BackendMediaEntry{
				Location: "https://example.com/media",
				Type:     mt,
			}

			data, _ := json.Marshal(entry)
			var result BackendMediaEntry
			json.Unmarshal(data, &result)

			if result.Type != mt {
				t.Errorf("Type not preserved: got %s, want %s", result.Type, mt)
			}
		})
	}
}

func TestChangeFrequencyValues(t *testing.T) {
	freqs := []ChangeFrequency{Always, Hourly, Daily, Weekly, Monthly, Yearly, Unknown}

	for _, freq := range freqs {
		t.Run(string(freq), func(t *testing.T) {
			url := BackendUrl{
				Location:   "https://example.com",
				ChangeFreq: stringPtr(string(freq)),
			}

			data, _ := json.Marshal(url)
			var result BackendUrl
			json.Unmarshal(data, &result)

			if result.ChangeFreq == nil || *result.ChangeFreq != string(freq) {
				t.Errorf("ChangeFreq not preserved")
			}
		})
	}
}

func stringPtr(s string) *string {
	return &s
}
