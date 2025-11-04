# Source Code for [my portfolio site](https://zacharymunshaw.dev/)
-----

It is still very much a work in progress but I'm using this as an opportunity to learn more about git and actually make something "in public" instead of deleting it once I'm done with it.

-----

## Backend
--

Backend for my [portfolio site](https://zacharymunshaw.dev), public because is demo code, I guess.

### Features
- #### .NET EntityFramework
  - 2 [postgres](https://github.com/postgres/postgres) Databases
    1. Search Engine database with [pgVector](https://github.com/pgvector/pgvector)
    2. Site DB


- #### AI Driven Search
  - Dense vectors for semantic meaning on chunks of words within a document.
  - Sparse vectors for keywords, finding "like" keywords.
  - Exact Keyword + sparse L2(taxi cab) distance + dense Cosine distance scoring system.
  - [PLANNED] Inclusion of a PageRank adjecent algorythm for page authority.

- #### Data Collection
  - [Go](https://go.dev/) and [Python](https://www.python.org/) based spider
  - Sitemap traversal and extraction
  - Page content extraction
  - Data Sanitization

------
- #### Data Embedding
  - Collected text from pages is converted into sparse and dense vectors
    - text is broken into words and converted to sprase embeddings using [Splade](https://huggingface.co/naver/splade-v3/discussions/3) for keyword searches.
    - then whole text is then split into large chunks and converted to dense embeddings using [IBM Granite Embedding](https://www.ibm.com/docs/en/watsonx/saas?topic=models-granite-embedding-278m-multilingual-model-card) for context searches.
