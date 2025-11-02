from bs4 import BeautifulSoup

def extract_main_content(html):
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