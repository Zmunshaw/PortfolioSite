import json
import sys

import stealth_requests

urls = json.loads(sys.argv[1])
results = []

for url in urls:
    resp = stealth_requests.get(url)
    data = {
        "links": resp.links,
        "title": resp.meta.title,
        "description": resp.meta.description,
        "content": resp.text_content(),
        "images": resp.images,
        "keywords": resp.meta.keywords,
    }
    results.append(data)

print(json.dumps(results))
