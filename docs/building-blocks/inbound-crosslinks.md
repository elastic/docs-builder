---
navigation_title: Inbound Crosslinks
---

# Inbound Crosslinks

**Inbound crosslinks** are links from other documentation sets to yours. Understanding and validating inbound crosslinks helps prevent breaking links in other repositories when you make changes.

## Purpose

Inbound crosslink validation allows you to:

* **Detect breaking changes** - Know when renaming or deleting a file will break links from other repositories
* **Prevent regressions** - Avoid publishing changes that break documentation elsewhere
* **Coordinate changes** - Understand dependencies before making structural changes

## How it works

A regular [build](../cli/docset/build.md) of a documentation set won't validate inbound links automatically.

You have to use the [inbound-links validate-link-reference](../cli/links/inbound-links-validate-link-reference.md) after a build to validate all inbound links.

The reason for this is that validating all inbound links has to download all published [Link Index](link-index.md) files
for the current [Content Source](../configure/content-sources.md).

## Validation commands

### Validate all inbound links

Check all inbound links for all published [Link Index](link-index.md) files declared in the [Link Catalog](link-catalog.md)

```bash
docs-builder inbound-links validate-all
```

### Validate specific link reference

Validate a locally built `links.json` against all published Link Index files:

```bash
docs-builder inbound-links validate-link-reference 
docs-builder inbound-links validate-link-reference --file .artifacts/docs/html/links.json
```

### Validate with filters

Check inbound links from specific repositories or to specific resources:

```bash
docs-builder inbound-links validate --from elasticsearch --to kibana
```

## Common scenarios

### Moving a file

If you move a file that other repositories link to:

1. Create a redirect from the old path to the new path
2. Update your documentation's redirect configuration
3. Run inbound link validation to ensure the redirect works
4. Notify teams that maintain repositories with inbound links

### Deleting a file

Before deleting a file:

1. Run inbound link validation to see if other repositories link to it
2. If there are inbound links, coordinate with those teams first
3. Consider leaving a redirect to related content
4. Update the other repositories to remove or update their links

### Renaming headings

Heading anchors are part of the Link Index. If other repositories link to specific headings in your documentation:

1. Validate inbound links before renaming
2. Consider keeping old heading anchors if heavily linked
3. Document the change if coordination is needed

## Integration with CI/CD

Our preview CI job will run inbound link validation automatically.

## Best practices

### Set up redirects

When moving or renaming files, always set up redirects:

```yaml
# In your documentation's configuration
redirects:
  - from: /old-path/file.html
    to: /new-path/file.html
```

### Communicate changes

If you need to make a breaking change:

1. Run inbound link validation to identify affected repositories
2. File issues or notify maintainers of affected repositories
3. Coordinate the change timing
4. Provide redirect mappings or alternative URLs

### Validate before merging

Make inbound link validation part of your review process:

* Run validation locally before creating a PR
* Include validation in CI checks
* Review validation results before merging

## Related concepts

* [Outbound Crosslinks](outbound-crosslinks.md) - Links from your documentation to others
* [Link Index](link-index.md) - How your linkable resources are tracked
* [Link Service](link-service.md) - Where inbound link information is stored
* [Distributed Documentation](distributed-documentation.md) - The architecture enabling this validation
