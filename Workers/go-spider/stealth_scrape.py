import json
import sys

import stealth_requests

urls = json.loads(sys.argv[1])
results = []

for url in urls:
    try:
        resp = stealth_requests.get(url)
        data = {
            "url": url,
            "links": resp.links,
            "title": resp.meta.title,
            "description": resp.meta.description,
            "content": resp.text_content(),
            "images": resp.images,
            "keywords": resp.meta.keywords,
        }
        results.append(data)
    except Exception as e:
        results.append({"url": url, "error": str(e)})

print(json.dumps(results))