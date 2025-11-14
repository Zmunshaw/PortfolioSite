import json
import sys
import stealth_requests
from extract_data import extract_main_content
from concurrent.futures import ThreadPoolExecutor, as_completed
import os

def scrape_url(url):
    """Scrape a single URL and return the result"""
    try:
        resp = stealth_requests.get(url)
        data = {
            "url": url,
            "links": resp.links,
            "title": resp.meta.title,
            "description": resp.meta.description,
            "content": extract_main_content(resp.text_content()),
            "images": resp.images,
            "keywords": resp.meta.keywords,
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