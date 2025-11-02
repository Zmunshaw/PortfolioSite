package main

import "encoding/xml"

type Sitemap struct {
	Hostname  string
	Sitemaps  []Sitemap
	SiteIndex SitemapIndex
	UrlSet    UrlSet
}

type SitemapIndex struct {
	XMLName xml.Name `xml:"sitemapindex"`
	Text    string   `xml:",chardata"`
	Sitemap []struct {
		Text    string `xml:",chardata"`
		Loc     string `xml:"loc"`
		Lastmod string `xml:"lastmod"`
	} `xml:"sitemap"`
}

type UrlSet struct {
	XMLName xml.Name `xml:"urlset"`
	Xmlns   string   `xml:"xmlns,attr"`
	Video   string   `xml:"video,attr"`

	URL []struct {
		Loc string `xml:"loc"`
		// Image
		Image []struct {
			Loc string `xml:"loc"`
		} `xml:"image"`
		// Video
		Video []struct {
			ThumbnailLoc    string `xml:"thumbnail_loc"`
			Title           string `xml:"title"`
			Description     string `xml:"description"`
			ContentLoc      string `xml:"content_loc"`
			PlayerLoc       string `xml:"player_loc"`
			Duration        string `xml:"duration"`
			ExpirationDate  string `xml:"expiration_date"`
			Rating          string `xml:"rating"`
			ViewCount       string `xml:"view_count"`
			PublicationDate string `xml:"publication_date"`
			FamilyFriendly  string `xml:"family_friendly"`

			Restriction struct {
				Relationship string `xml:"relationship,attr"`
			} `xml:"restriction"`

			Price struct {
				Currency string `xml:"currency,attr"`
			} `xml:"price"`

			RequiresSubscription string `xml:"requires_subscription"`

			Uploader struct {
				Info string `xml:"info,attr"`
			} `xml:"uploader"`

			Live string `xml:"live"`
		} `xml:"video"`
		// News
		News struct {
			Publication struct {
				Text     string `xml:",chardata"`
				Name     string `xml:"name"`
				Language string `xml:"language"`
			} `xml:"publication"`
			PublicationDate string `xml:"publication_date"`
			Title           string `xml:"title"`
		} `xml:"news"`
	} `xml:"url"`
}
