# Go-Spider API Documentation

## Overview

Go-Spider is a high-performance web scraping service with sitemap support. It provides three main endpoints for web scraping and sitemap processing.

## Performance Optimizations

The spider includes several performance optimizations:

- **Concurrent URL scraping**: Uses ThreadPoolExecutor in Python (default: 10 workers)
- **Concurrent sitemap fetching**: Fetches nested sitemaps in parallel (default: 5 concurrent)
- **Batch processing**: Processes large URL sets in configurable batches (default: 50 URLs/batch)

### Configuration

Set environment variables to tune performance:

```bash
# Configure Python scraper concurrency (default: 10)
export SCRAPER_MAX_WORKERS=20

# Server port (default: 9900)
export PORT=8080
```

## Endpoints

### 1. POST /scrape

Scrape a list of URLs and extract content.

**Request:**
```bash
curl -X POST http://localhost:9900/scrape \
  -H "Content-Type: application/json" \
  -d '["https://example.com", "https://example.com/about"]'
```

**Response:**
```json
[
  {
    "url": "https://example.com",
    "title": "Example Domain",
    "description": "Example description",
    "content": "Main content extracted...",
    "links": ["https://example.com/page1"],
    "images": ["https://example.com/image.jpg"],
    "keywords": "example, domain, test"
  }
]
```

**Error Response:**
```json
[
  {
    "url": "https://example.com",
    "error": "Connection timeout"
  }
]
```

---

### 2. GET/POST /map

Discover and parse a website's sitemap.

**GET Request:**
```bash
curl "http://localhost:9900/map?url=https://example.com"
```

**POST Request:**
```bash
curl -X POST http://localhost:9900/map \
  -H "Content-Type: application/json" \
  -d '{"url": "https://example.com"}'
```

**Response:**
```json
{
  "Hostname": "https://example.com",
  "SiteIndex": {
    "sitemap": [
      {
        "loc": "https://example.com/sitemap-posts.xml",
        "lastmod": "2025-01-01T00:00:00Z"
      }
    ]
  },
  "UrlSet": {
    "url": [
      {
        "loc": "https://example.com/page1",
        "image": [],
        "video": []
      }
    ]
  }
}
```

---

### 3. GET/POST /scrape-sitemap ⚡ NEW

Discover a sitemap and scrape all URLs found in it. This is the most powerful endpoint, combining sitemap discovery with concurrent scraping.

**GET Request:**
```bash
curl "http://localhost:9900/scrape-sitemap?url=https://example.com"
```

**POST Request with Options:**
```bash
curl -X POST http://localhost:9900/scrape-sitemap \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "batchSize": 100,
    "maxConcurrent": 10
  }'
```

**Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `url` | string | required | Base URL to discover sitemap |
| `batchSize` | int | 50 | Number of URLs to process per batch |
| `maxConcurrent` | int | 5 | Max concurrent sitemap fetches for sitemap indexes |

**Response:**
```json
{
  "url": "https://example.com",
  "totalUrls": 150,
  "scrapedUrls": 148,
  "results": [
    {
      "url": "https://example.com/page1",
      "title": "Page 1",
      "content": "Content here...",
      "links": [...],
      "images": [...],
      "keywords": "..."
    },
    ...
  ]
}
```

**Features:**
- Automatically discovers sitemap from robots.txt or common paths
- Handles sitemap indexes recursively (fetches all nested sitemaps concurrently)
- Processes URLs in batches to manage memory
- Scrapes all URLs concurrently within each batch
- Continues processing even if individual batches fail

---

## Sitemap Discovery

The service automatically discovers sitemaps using:

1. **robots.txt** - Checks for `Sitemap:` directive
2. **Common paths** - Tries these paths:
   - `/sitemap.xml`
   - `/sitemap_index.xml`
   - `/sitemap/sitemap.xml`
   - `/sitemap-index.xml`
   - `/sitemaps.xml`

## Error Handling

- Individual URL scraping errors are captured in the results with an `error` field
- Batch processing continues even if some batches fail
- Nested sitemap fetch errors are logged but don't stop processing
- HTTP errors return appropriate status codes (400, 404, 500)

## Performance Tips

For optimal performance when scraping large sitemaps:

1. **Increase batch size** for sites with many small pages (e.g., 100-200)
2. **Increase maxConcurrent** if the sitemap has many nested sitemap indexes
3. **Adjust SCRAPER_MAX_WORKERS** based on your system resources
4. **Monitor memory usage** when processing very large sitemaps (10,000+ URLs)

## Example Workflow

```bash
# 1. Discover and view sitemap structure
curl "http://localhost:9900/map?url=https://example.com" | jq

# 2. Scrape all pages from sitemap with custom settings
curl -X POST http://localhost:9900/scrape-sitemap \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "batchSize": 100,
    "maxConcurrent": 10
  }' | jq '.scrapedUrls'

# 3. Scrape specific URLs
curl -X POST http://localhost:9900/scrape \
  -H "Content-Type: application/json" \
  -d '["https://example.com/page1", "https://example.com/page2"]' | jq
```
