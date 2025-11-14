import json
import sys
import stealth_requests
from extract_data import (
    extract_main_content,
    extract_metadata,
    categorize_links,
    extract_image_data
)
from concurrent.futures import ThreadPoolExecutor, as_completed
from bs4 import BeautifulSoup
import os

def scrape_url(url):
    """Scrape a single URL and return comprehensive data"""
    try:
        resp = stealth_requests.get(url)
        html = resp.text_content()
        soup = BeautifulSoup(html, 'html.parser')

        # Extract comprehensive metadata
        metadata = extract_metadata(html, url)

        # Categorize links
        link_data = categorize_links(resp.links, url)

        # Extract detailed image data
        image_data = extract_image_data(soup)

        # Build comprehensive response
        data = {
            "url": url,
            "title": resp.meta.title,
            "description": resp.meta.description,
            "content": extract_main_content(html),
            "keywords": resp.meta.keywords,

            # Enhanced metadata
            "author": metadata.get('author'),
            "published": metadata.get('published'),
            "modified": metadata.get('modified'),
            "canonical": metadata.get('canonical'),
            "language": metadata.get('language'),
            "wordCount": metadata.get('wordCount', 0),

            # Structured data
            "headers": metadata.get('headers'),
            "openGraph": metadata.get('og'),
            "twitterCard": metadata.get('twitter'),

            # Link analysis
            "links": resp.links,  # Keep full list for backward compatibility
            "internalLinks": link_data['internal'],
            "externalLinks": link_data['external'],
            "internalLinkCount": link_data['internalCount'],
            "externalLinkCount": link_data['externalCount'],

            # Enhanced image data
            "images": resp.images,  # Keep simple list for backward compatibility
            "imageData": image_data,
        }
        return data
    except Exception as e:
        return {"url": url, "error": str(e)}

urls = json.loads(sys.argv[1])
results = []

# Use concurrent processing for better speed
# Default to 10 workers, can be configured via environment variable
max_workers = int(os.getenv("SCRAPER_MAX_WORKERS", "10"))

if len(urls) == 1:
    # For single URL, don't use thread pool overhead
    results.append(scrape_url(urls[0]))
else:
    # Process multiple URLs concurrently
    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        # Submit all scraping tasks
        future_to_url = {executor.submit(scrape_url, url): url for url in urls}

        # Collect results as they complete
        for future in as_completed(future_to_url):
            try:
                result = future.result()
                results.append(result)
            except Exception as e:
                url = future_to_url[future]
                results.append({"url": url, "error": f"Executor error: {str(e)}"})

print(json.dumps(results))