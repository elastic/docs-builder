---
navigation_title: Link catalog
---

# Link catalog

The **Link Catalog** is a single JSON file that serves as a catalog of all available [Link Index](link-index.md) files across all repositories.

## Purpose

The Link Catalog provides:

* **Discovery** - A single file to query for all available documentation across all repositories and branches.
* **Efficiency** - Avoid scanning the entire [Link Service](link-service.md) to find available Link Index files.
* **Assembler coordination** - The assembler uses this to determine which repositories and versions are available to build.

## Location

The Link Catalog is available at:

```
https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/link-index.json
```

## Structure

The Link Catalog contains:

* List of all organizations (e.g., `elastic`).
* Repositories within each organization (e.g., `elasticsearch`, `kibana`).
* Branches for each repository (e.g., `main`, `8.x`, `7.17`).
* Metadata about each Link Index:
  * Last updated timestamp.
  * Commit SHA that produced the Link Index.
  * ETAG of the Link Index file.
  * URL to the Link Index file.

## Maintenance

The Link Catalog is automatically maintained:

1. A repository's CI/CD pipeline publishes a new `links.json` to the Link Service
2. The S3 bucket triggers an SQS message
3. An AWS Lambda function listens to these SQS messages
4. The Lambda function updates the Link Catalog to include or update the entry for the new Link Index

This process ensures the catalog stays in sync with published Link Index files without manual intervention.

## Usage

### By the assembler

When running `docs-builder assembler clone` or `docs-builder assembler build`:

1. The assembler fetches the Link Catalog
2. It determines which repositories and versions to clone/build based on the [site configuration](../configure/site/index.md)
3. It uses the commit SHAs from the catalog to clone specific versions

### By documentation builds

During a documentation build:

1. `docs-builder` fetches the Link Catalog
2. It determines which Link Index files to download for cross-repository validation
3. It validates all cross-links against the appropriate Link Index files

## Benefits

* **Single source of truth** - One file to check for all available documentation.
* **Performance** - Fast lookup without scanning the entire Link Service.
* **Automation** - Maintained automatically via Lambda functions.
* **Resilience** - Represents only successful builds with valid Link Indexes.

## Related concepts

* [Link Service](link-service.md) - Where the Link Catalog is stored.
* [Link Index](link-index.md) - The files cataloged by the Link Catalog.
* [Assembled Documentation](assembled-documentation.md) - Uses the Link Catalog to coordinate builds.
