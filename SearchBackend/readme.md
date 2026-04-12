# Portfolio Site Backend

Backend for my [portfolio site](https://zacharymunshaw.dev), public because is demo code, I guess.

Hangfire, DDD,  SignalR, MediatR, EFCore, Serilog, Polly, Mapster
------
## Features
- ### .NET EntityFramework
  - 2 [postgres](https://github.com/postgres/postgres) Databases
    1. Search Engine database with [pgVector](https://github.com/pgvector/pgvector)
    2. Site DB


- ### AI Driven Search
  - Dense vector search on sentence meaning.
  - Sparse vector search on keywords.

- ### Data Collection
  - Sitemap traversal and extraction
  - Page content extraction

- ### Data Embedding