---
navigation_title: Link index
---

# Link index

A **Link Index** is a JSON file (`links.json`) that contains all the linkable resources for a specific repository branch.

## Purpose

The Link Index enables:

* **Cross-repository linking**: Other documentation sets can link to your content.
* **Link validation**: Validate that links to your content are correct.
* **Inbound link detection**: Know when other repositories link to your content.
* **Distributed builds**: Build documentation independently while maintaining link integrity.

## Structure

Each repository branch gets its own Link Index file in the [Link Service](link-service.md), organized by:

* **Organization**: e.g., `elastic`.
* **Repository**: e.g., `elasticsearch`.
* **Branch**: e.g., `main`, `8.x`, `7.17`.

## Example

View [Elasticsearch's main branch Link Index](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/elasticsearch/main/links.json) to see a real example.

The Link Index contains:

* All documentation pages in the repository.
* Headings within those pages.
* Anchors and linkable elements.
* Version information.
* Metadata about the build.

## Generation

The Link Index is automatically generated during a documentation build:

1. `docs-builder` builds the documentation set
2. During the build, all linkable resources are tracked
3. After a successful build, a `links.json` file is written to `.artifacts/docs/html/links.json`
4. CI/CD publishes this file to the [Link Service](link-service.md)

## Usage

### Resolving outbound cross-links

When you use a cross-link like `elasticsearch://reference/api/search.md`, `docs-builder`:

1. Fetches the Elasticsearch Link Index from the Link Service
2. Looks up the path in the index
3. Validates the link exists
4. Resolves it to the correct URL

### Validating inbound cross-links

When building your documentation, `docs-builder` can:

1. Fetch your repository's Link Index from previous builds
2. Compare with your current local changes
3. Detect if you've moved or deleted files that other repositories link to
4. Warn about breaking changes

## File location

During a build, the Link Index is written to:

```
.artifacts/docs/html/links.json
```

After publishing, it's available at:

```
https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{org}/{repo}/{branch}/links.json
```

## Related concepts

* [Link Service](link-service.md) - Where Link Index files are stored.
* [Link Catalog](link-catalog.md) - Catalog of all Link Index files.
* [Outbound Cross-links](outbound-cross-links.md) - Links that use the Link Index.
* [Inbound Cross-links](inbound-cross-links.md) - Links to resources in the Link Index.
