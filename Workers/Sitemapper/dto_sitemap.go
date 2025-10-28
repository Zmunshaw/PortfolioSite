package main

import (
	"strconv"
	"time"
)

type ChangeFrequency string
type MediaType string

const (
	Always  ChangeFrequency = "Always"
	Hourly  ChangeFrequency = "Hourly"
	Daily   ChangeFrequency = "Daily"
	Weekly  ChangeFrequency = "Weekly"
	Monthly ChangeFrequency = "Monthly"
	Yearly  ChangeFrequency = "Yearly"
	Unknown ChangeFrequency = "Unknown"

	Image MediaType = "Image"
	Video MediaType = "Video"
	News  MediaType = "News"
)

type BackendSitemap struct {
	SitemapID    int              `json:"SitemapID,omitempty"`
	Location     string           `json:"Location"`
	LastModified time.Time        `json:"LastModified"`
	SitemapIndex []BackendSitemap `json:"SitemapIndex,omitempty"`
	UrlSet       []BackendUrl     `json:"UrlSet,omitempty"`
	IsMapped     bool             `json:"IsMapped"`
}

type BackendUrl struct {
	UrlID        int                 `json:"UrlID,omitempty"`
	Location     string              `json:"Location"`
	LastModified *time.Time          `json:"LastModified,omitempty"`
	ChangeFreq   *string             `json:"ChangeFrequency,omitempty"`
	Priority     float32             `json:"Priority"`
	Media        []BackendMediaEntry `json:"Media,omitempty"`
}

type BackendMediaEntry struct {
	MediaEntryID int       `json:"MediaEntryID,omitempty"`
	Location     string    `json:"Location"`
	Type         MediaType `json:"Type"`

	// Video
	ThumbnailLocation    *string    `json:"ThumbnailLocation,omitempty"`
	Title                *string    `json:"Title,omitempty"`
	Description          *string    `json:"Description,omitempty"`
	ContentLocation      *string    `json:"ContentLocation,omitempty"`
	PlayerLocation       *string    `json:"PlayerLocation,omitempty"`
	Duration             *string    `json:"Duration,omitempty"`
	Rating               *float32   `json:"Rating,omitempty"`
	ViewCount            *int       `json:"ViewCount,omitempty"`
	PublicationDate      *time.Time `json:"PublicationDate,omitempty"`
	Restrictions         *string    `json:"Restrictions,omitempty"`
	Platform             *string    `json:"Platform,omitempty"`
	RequiresSubscription *string    `json:"RequiresSubscription,omitempty"`
	Tag                  *string    `json:"Tag,omitempty"`

	// News
	Publication *string `json:"Publication,omitempty"`
	Language    *string `json:"Language,omitempty"`
}

// TODO: This is AI Generated - Probably broken in some way -- READ IT
func TransformToBackendModel(original Sitemap) BackendSitemap {
	newSitemap := BackendSitemap{
		Location:     original.Hostname,
		LastModified: time.Now(), // or parsed from XML
		IsMapped:     false,
	}

	// Transform sub-sitemaps
	for _, sm := range original.SiteIndex.Sitemap {
		t, _ := time.Parse(time.RFC3339, sm.Lastmod)
		newSitemap.SitemapIndex = append(newSitemap.SitemapIndex, BackendSitemap{
			Location:     sm.Loc,
			LastModified: t,
			IsMapped:     false,
		})
	}

	// Transform URLs
	for _, url := range original.UrlSet.URL {
		u := BackendUrl{
			Location: url.Loc,
			Priority: 0.5, // default
		}

		// Images
		for _, img := range url.Image {
			u.Media = append(u.Media, BackendMediaEntry{
				Location: img.Loc,
				Type:     Image,
			})
		}

		// Videos
		for _, vid := range url.Video {
			rating := parseFloatPointer(vid.Rating)
			duration := vid.Duration
			u.Media = append(u.Media, BackendMediaEntry{
				Location:          vid.ContentLoc,
				Type:              Video,
				ThumbnailLocation: &vid.ThumbnailLoc,
				Title:             &vid.Title,
				Description:       &vid.Description,
				ContentLocation:   &vid.ContentLoc,
				PlayerLocation:    &vid.PlayerLoc,
				Duration:          &duration,
				Rating:            rating,
			})
		}

		// News
		if url.News.Publication.Name != "" {
			pubDate, _ := time.Parse(time.RFC3339, url.News.PublicationDate)
			u.Media = append(u.Media, BackendMediaEntry{
				Type:            News,
				Publication:     &url.News.Publication.Name,
				Language:        &url.News.Publication.Language,
				Title:           &url.News.Title,
				PublicationDate: &pubDate,
			})
		}

		newSitemap.UrlSet = append(newSitemap.UrlSet, u)
	}

	return newSitemap
}

func parseFloatPointer(s string) *float32 {
	if s == "" {
		return nil
	}
	f, err := strconv.ParseFloat(s, 32)
	if err != nil {
		return nil
	}
	f32 := float32(f)
	return &f32
}
