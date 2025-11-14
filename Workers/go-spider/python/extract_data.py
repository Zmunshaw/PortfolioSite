from bs4 import BeautifulSoup
from urllib.parse import urlparse
from datetime import datetime

def extract_main_content(html):
    """Extract main content text from HTML"""
    soup = BeautifulSoup(html, 'html.parser')

    # Remove all script and style tags
    for tag in soup.find_all(['script', 'style', 'nav', 'footer', 'header', 'link', 'meta', 'noscript', 'iframe']):
        tag.decompose()

    # Remove ad/noise containers
    for selector in ['.ad', '.advertisement', '.sidebar', '.comments', '.navigation',
                     '.header', '.footer', '[id*="ad"]', '[class*="ad"]',
                     '[class*="sidebar"]', '[class*="widget"]']:
        for tag in soup.select(selector):
            tag.decompose()

    # Find main content
    main_content = None
    for selector in ['main', '[role="main"]', '.main-content', '.content',
                     '.post-content', '.article-content', '.page-content',
                     '.products-wrap', '#content', '.container', 'article']:
        main_content = soup.select_one(selector)
        if main_content:
            break

    if not main_content:
        main_content = soup.find('body') or soup

    text = main_content.get_text(separator='\n', strip=True)
    lines = [line.strip() for line in text.split('\n') if line.strip()]

    # Filter out CSS/JS patterns
    lines = [line for line in lines
             if len(line) > 5
             and not line.startswith(('@', 'function', 'var ', 'const ', 'let ', 'if (', 'for ('))
             and '--' not in line  # CSS variables
             and '::' not in line  # CSS pseudo-elements
             and '{' not in line[:10]  # CSS blocks
             and 'px' not in line[-3:]  # CSS units
             ]

    return '\n'.join(lines)


def extract_metadata(html, base_url):
    """Extract comprehensive metadata from HTML"""
    soup = BeautifulSoup(html, 'html.parser')
    metadata = {}

    # Author
    author = None
    author_tag = soup.find('meta', attrs={'name': 'author'}) or \
                 soup.find('meta', attrs={'property': 'article:author'}) or \
                 soup.find('meta', attrs={'property': 'og:author'})
    if author_tag:
        author = author_tag.get('content')
    metadata['author'] = author

    # Published date
    published = None
    date_tag = soup.find('meta', attrs={'property': 'article:published_time'}) or \
               soup.find('meta', attrs={'name': 'publish_date'}) or \
               soup.find('meta', attrs={'name': 'date'}) or \
               soup.find('time', attrs={'itemprop': 'datePublished'}) or \
               soup.find('time', attrs={'class': 'published'})

    if date_tag:
        published = date_tag.get('content') or date_tag.get('datetime') or date_tag.get_text()
    metadata['published'] = published

    # Modified date
    modified = None
    modified_tag = soup.find('meta', attrs={'property': 'article:modified_time'}) or \
                   soup.find('meta', attrs={'name': 'last-modified'}) or \
                   soup.find('time', attrs={'itemprop': 'dateModified'})
    if modified_tag:
        modified = modified_tag.get('content') or modified_tag.get('datetime') or modified_tag.get_text()
    metadata['modified'] = modified

    # Canonical URL
    canonical = None
    canonical_tag = soup.find('link', attrs={'rel': 'canonical'}) or \
                    soup.find('meta', attrs={'property': 'og:url'})
    if canonical_tag:
        canonical = canonical_tag.get('href') or canonical_tag.get('content')
    metadata['canonical'] = canonical

    # Language
    lang = None
    html_tag = soup.find('html')
    if html_tag:
        lang = html_tag.get('lang')
    if not lang:
        lang_tag = soup.find('meta', attrs={'http-equiv': 'content-language'}) or \
                   soup.find('meta', attrs={'name': 'language'})
        if lang_tag:
            lang = lang_tag.get('content')
    metadata['language'] = lang

    # Open Graph data
    og_data = {}
    for og_tag in soup.find_all('meta', attrs={'property': lambda x: x and x.startswith('og:')}):
        prop = og_tag.get('property', '').replace('og:', '')
        content = og_tag.get('content')
        if prop and content:
            og_data[prop] = content
    metadata['og'] = og_data if og_data else None

    # Twitter Card data
    twitter_data = {}
    for tw_tag in soup.find_all('meta', attrs={'name': lambda x: x and x.startswith('twitter:')}):
        name = tw_tag.get('name', '').replace('twitter:', '')
        content = tw_tag.get('content')
        if name and content:
            twitter_data[name] = content
    metadata['twitter'] = twitter_data if twitter_data else None

    # Headers structure
    headers = {
        'h1': [h.get_text(strip=True) for h in soup.find_all('h1')],
        'h2': [h.get_text(strip=True) for h in soup.find_all('h2')],
        'h3': [h.get_text(strip=True) for h in soup.find_all('h3')],
    }
    metadata['headers'] = headers

    # Word count
    text = soup.get_text()
    words = [w for w in text.split() if len(w) > 2]
    metadata['wordCount'] = len(words)

    return metadata


def categorize_links(links, base_url):
    """Categorize links as internal or external"""
    base_domain = urlparse(base_url).netloc

    internal = []
    external = []

    for link in links:
        try:
            link_domain = urlparse(link).netloc
            if not link_domain:  # Relative URL
                internal.append(link)
            elif link_domain == base_domain:
                internal.append(link)
            else:
                external.append(link)
        except:
            continue

    return {
        'internal': internal,
        'external': external,
        'internalCount': len(internal),
        'externalCount': len(external)
    }


def extract_image_data(soup):
    """Extract detailed image information including alt text"""
    images = []
    for img in soup.find_all('img'):
        src = img.get('src')
        if src:
            images.append({
                'src': src,
                'alt': img.get('alt', ''),
                'title': img.get('title', '')
            })
    return images