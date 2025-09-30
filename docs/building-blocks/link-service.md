---
navigation_title: Link Service
---

# Link Service

The **Link Service** is the central location where all [Link Index](link-index.md) files are published and stored.

## Architecture

The Link Service is implemented as:

* **Storage** - An S3 bucket containing all Link Index files
* **CDN** - CloudFront fronting the S3 bucket for fast global access
* **Access** - Publicly accessible for read operations

## Purpose

The Link Service enables:

* **Distributed validation** - Any documentation build can validate cross-repository links without cloning all repositories
* **Link discovery** - Find what resources are available in other repositories
* **Build resilience** - Assembler builds can reference the last known good state of each repository
* **Decentralized publishing** - Each repository publishes its own Link Index independently

## URL structure

Link Index files are organized by repository and branch:

```
https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{org}/{repo}/{branch}/links.json
```

For example:
* [Elasticsearch main branch](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/elasticsearch/main/links.json)
* [Kibana main branch](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/kibana/main/links.json)

## Publishing process

When a documentation build completes successfully on a default integration branch:

1. The build generates a `links.json` file
2. The CI/CD pipeline publishes the file to the Link Service
3. An AWS Lambda function triggers on the S3 event
4. The Lambda updates the [Link Registry](link-registry.md) to include the new Link Index

## Access during builds

During both local and CI builds, `docs-builder`:

* Fetches relevant Link Index files from the Link Service
* Validates outbound crosslinks against these indexes
* Validates that inbound crosslinks won't be broken by local changes

## Related concepts

* [Link Index](link-index.md) - The files stored in the Link Service
* [Link Registry](link-registry.md) - The catalog of all Link Index files
* [Distributed Documentation](distributed-documentation.md) - Why the Link Service exists
