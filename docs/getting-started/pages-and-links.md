---
navigation_title: Pages & links
---

# Pages and links

Every documentation set needs a `docset.yml` that declares which pages exist and how they're organized in the navigation.

## Adding a page to navigation

Pages are listed under the `toc:` key in your `docset.yml`:

```yaml
toc:
  - file: index.md
  - file: getting-started.md
  - file: configuration.md
```

Each `- file:` entry adds a page to the sidebar navigation in the order listed.

### Organizing into folders

Group related pages under a folder:

```yaml
toc:
  - file: index.md
  - folder: guides
    children:
      - file: index.md
      - file: quickstart.md
      - file: advanced.md
```

### Nested table of contents

For larger documentation sets, you can split navigation into separate `toc.yml` files:

```yaml
# docset.yml
toc:
  - file: index.md
  - toc: guides
```

```yaml
# guides/toc.yml
toc:
  - file: index.md
  - file: quickstart.md
```

See [navigation configuration](../documentation/isolated/navigation.md) for the complete reference.

## Linking to pages

### Within the same documentation set

Use relative Markdown links to reference other pages:

```markdown
See the [configuration guide](./configuration.md) for details.

Refer to the [advanced section](../guides/advanced.md#specific-heading).
```

Links are validated at build time — broken links produce errors.

### To other documentation sets (cross-links)

To link to pages in other repositories, use cross-link syntax:

```markdown
[Getting started with Elasticsearch](elasticsearch://reference/getting-started.md)
```

The format is `<repository>://<path-to-file>`. The repository name matches the GitHub repository name without the org prefix.

#### Declaring cross-link dependencies

Before using cross-links, declare the target repositories in your `docset.yml`:

```yaml
cross_links:
  - elasticsearch
  - kibana
  - docs-content
```

This tells {{dbuild}} to fetch the [link index](../documentation/distributed-builds.md) for each listed repository so it can validate your cross-links at build time — even during local development.

:::{tip}
Only list repositories you actually link to. Each entry adds a link-index fetch during builds.
:::

## What's next

- **[Serve and preview](./serve.md)** — preview your docs locally with hot reload
- **[Content set configuration](../documentation/isolated/configure/index.md)** — full docset.yml reference
- **[Links syntax](../syntax/links.md)** — complete link syntax including anchors and titles
