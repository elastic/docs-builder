---
navigation_title: Distributed documentation
---

# Distributed documentation

**Distributed documentation** is the architectural approach that allows repositories to build their own documentation independently.

## Purpose

The separation between building individual documentation sets and assembling them enables distributed builds, where:

* Each repository builds its own documentation independently
* Builds don't block each other
* Teams maintain full autonomy over their documentation
* Cross-repository links are validated without requiring synchronized builds

## How it works

### Link Index publication

Each time a documentation set is built successfully on its default integration branch, it produces and publishes a `links.json` file ([Link Index](link-index.md)) to a central location ([Link Service](link-service.md)).

This Link Index contains all the linkable resources in that repository at that specific commit.

### Example

For instance, [Elasticsearch's links.json](https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/elasticsearch/main/links.json) represents all linkable resources in the Elasticsearch repository's main branch.

## Benefits

This distributed approach provides several key advantages:

### Link validation

* **Outbound links** - Validate links to other repositories ahead of time, even during local `docs-builder` builds
* **Inbound links** - Know when changes to your documentation would break links from other repositories

### Resilient builds

* **Isolation** - Documentation errors in one repository won't affect builds of other repositories
* **Fallback mechanism** - When a repository has build failures on its integration branch, the assembler falls back to the last known good commit
* **Snapshot builds** - Assembled builds only use commits that successfully produced a Link Index

### Independent iteration

* Teams can iterate on their documentation independently
* No coordination required for routine updates
* Faster feedback loops for documentation changes

## Architecture components

The distributed documentation system relies on several key components:

* [Link Index](link-index.md) - Per-repository file of linkable resources
* [Link Service](link-service.md) - Central storage for Link Index files
* [Link Catalog](link-catalog.md) - Catalog of all available Link Index files
* [Outbound Cross-links](outbound-cross-links.md) - Links from one repository to another
* [Inbound Cross-links](inbound-cross-links.md) - Links from other repositories to yours

## Local development

Even during local development, `docs-builder` can access the [Link Service](link-service.md) to validate cross-repository links without requiring you to clone and build all related repositories.
