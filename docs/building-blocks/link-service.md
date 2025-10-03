---
navigation_title: Link service
---

# Link service

The **Link Service** is the central location that stores:

* All [Link Index](link-index.md) files for all the repositories and branches that are published.
* The [Link Catalog](link-catalog.md), a single JSON file that contains references to all the `Link Index` files.

We only have one link service today for all public documentation.

* https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/

## Architecture

The Link Service is implemented as:

* **Storage**: An S3 bucket.
* **CDN**: CloudFront fronting the S3 bucket for fast global access.
* **Access**: Publicly accessible for read operations.

## Purpose

The Link Service enables:

* **Distributed validation**: Any documentation build can validate cross-repository links without cloning all repositories.
* **Link discovery**: Find what resources are available in other repositories.
* **Build resilience**: Assembler builds can reference the last known good state of each repository.
* **Decentralized publishing**: Each repository publishes its own Link Index independently.

## URL structure

Link Index files are organized by repository and branch:

```
https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{org}/{repo}/{branch}/links.json
```

For example:
* [Elasticsearch main branch](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/elasticsearch/main/links.json).
* [Kibana main branch](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/kibana/main/links.json).

## Publishing process

When a documentation build completes successfully on a default integration branch:

1. The build generates a `links.json` file
2. The CI/CD pipeline publishes the file to the Link Service
3. An AWS Lambda function triggers on the S3 event
4. The Lambda updates the [Link Catalog](link-catalog.md) to include the new Link Index

## Access during builds

During both local and CI builds, `docs-builder`:

* Fetches relevant Link Index files from the Link Service.
* Validates outbound cross-links against these indexes.
* Validates that inbound cross-links won't be broken by local changes.

## Related concepts

* [Link Index](link-index.md) - The files stored in the Link Service.
* [Link Catalog](link-catalog.md) - The catalog of all Link Index files.
* [Distributed Documentation](distributed-documentation.md) - Why the Link Service exists.
