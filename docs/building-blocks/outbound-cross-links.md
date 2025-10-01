---
navigation_title: Outbound cross-links
---

# Outbound cross-links

**Outbound cross-links** are links from your documentation set to other documentation sets in different repositories.

## Purpose

Outbound cross-links allow you to:

* Link to documentation in other repositories
* Maintain those links even as the target repository evolves
* Validate links during local builds
* Get warnings if target content is moved or deleted

## Syntax

If both repositories publish to the same [Link Service](link-service.md), they can link to each other using the cross-link syntax:

```markdown
[Link text](repository-name://path/to/file.md)
```

For example:

```markdown
See the [Search API documentation](elasticsearch://reference/api/search.md)
```

## How it works

You have to explicitly opt in to another repository's `Link Index` by adding it to your `docset.yml` file:

```yaml
cross_links:
  - docs-content
```


When `docs-builder` encounters a cross-link:

1. **Parse** - Extracts the repository name and path from the link
3. **Resolve** - Looks up the path in the locally cached [Link Index](link-index.md) to get the actual URL
4. **Validate** - Verifies the link exists and generates an error if not
5. **Transform** - Replaces the cross-link with the fully resolved URL in the output

## Validation

During a build, `docs-builder`:

* **Validates immediately** - Checks all outbound cross-links against locally fetched [Link Index](link-index.md) files
* **Reports errors** - Reports errors about broken links before you publish

## Configuration

To enable cross-links to a repository, add it to your `docset.yml`:

```yaml
cross_links:
  - elasticsearch
  - kibana
  - fleet
```

This instructs `docs-builder` to fetch the `Link Index` from the [Link Service](link-service.md) during the build process which are then cached locally.
`docs-builder` will validate locally cached `Link Index` files against the remote `Link Index` files on each build fetching updates as needed.

Now you can create cross-links e.g `elasticsearch://path/to/file.md`

The explicit opt-in prevents each repository build having the fetch all the links for all the repositories in the [`Link Catalog`](link-catalog.md) of which there may be many.

## Best practices

### Link to files, not URLs

**Good:**
```markdown
[Search API](elasticsearch://reference/api/search.md)
```

**Bad:**
```markdown
[Search API](https://www.elastic.co/guide/en/elasticsearch/reference/current/search.html)
```

The cross-link syntax is resilient to:
* URL structure changes
* File moves (if redirects are configured)
* Version differences

### Link to headings

You can link to specific headings within a page:

```markdown
[Query DSL](elasticsearch://reference/query-dsl.md#match-query)
```

## Related concepts

* [Inbound Cross-links](inbound-cross-links.md) - Links from other repositories to yours
* [Link Index](link-index.md) - How cross-links are resolved
* [Links syntax](../syntax/links.md) - Complete link syntax documentation
