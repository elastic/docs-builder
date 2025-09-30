---
navigation_title: Outbound Crosslinks
---

# Outbound Crosslinks

**Outbound crosslinks** are links from your documentation set to other documentation sets in different repositories.

## Purpose

Outbound crosslinks allow you to:

* Link to documentation in other repositories
* Maintain those links even as the target repository evolves
* Validate links during local builds
* Get warnings if target content is moved or deleted

## Syntax

If both repositories publish to the same [Link Service](link-service.md), they can link to each other using the crosslink syntax:

```markdown
[Link text](repository-name://path/to/file.md)
```

For example:

```markdown
See the [Search API documentation](elasticsearch://reference/api/search.md)
```

## How it works

When `docs-builder` encounters a crosslink:

1. **Parse** - Extracts the repository name and path from the link
2. **Fetch** - Downloads the target repository's [Link Index](link-index.md) from the Link Service
3. **Resolve** - Looks up the path in the Link Index to get the actual URL
4. **Validate** - Verifies the link exists and generates a warning if not
5. **Transform** - Replaces the crosslink with the resolved URL in the output

## Validation

During a build, `docs-builder`:

* **Validates immediately** - Checks all outbound crosslinks against published Link Index files
* **Reports errors** - Warns about broken links before you publish
* **Suggests fixes** - If a file was moved, the Link Index may include redirect information

### Local validation

Even during local development, you can validate outbound crosslinks:

```bash
docs-builder --path ./docs
```

This will:
* Fetch Link Index files from the Link Service
* Validate all crosslinks in your local documentation
* Report any broken links

## Configuration

To enable crosslinks to a repository, add it to your `docset.yml`:

```yaml
cross_links:
  - elasticsearch
  - kibana
  - fleet
```

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

The crosslink syntax is resilient to:
* URL structure changes
* File moves (if redirects are configured)
* Version differences

### Link to headings

You can link to specific headings within a page:

```markdown
[Query DSL](elasticsearch://reference/query-dsl.md#match-query)
```

### Specify versions

For assembled documentation, the assembler handles version mapping. For local builds, crosslinks resolve to the default branch of the target repository.

## Related concepts

* [Inbound Crosslinks](inbound-crosslinks.md) - Links from other repositories to yours
* [Link Index](link-index.md) - How crosslinks are resolved
* [Links syntax](../syntax/links.md) - Complete link syntax documentation
