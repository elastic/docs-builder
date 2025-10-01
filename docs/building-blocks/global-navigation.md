---
navigation_title: Global Navigation
---

# Global Navigation

**Global navigation** defines how multiple documentation sets are organized and presented together in [assembled documentation](assembled-documentation.md). It creates a unified navigation structure across all repositories.

## Purpose

Global navigation enables:

* **Unified experience** - Present documentation from multiple repositories as a cohesive whole
* **Flexible organization** - Arrange documentation by product, feature, or audience rather than by repository
* **Independent evolution** - Reorganize global structure without changing documentation sets
* **Cross-repository grouping** - Combine related content from different repositories

## Configuration

Global navigation is defined in the `navigation.yml` file, which is part of the [site configuration](../configure/site/index.md). It follows a very similar `toc` configuration structure to [documentation set navigation](documentation-set-navigation.md).

## Key differences from documentation set navigation

Global navigation has specific restrictions:

* **It may only link to `toc.yml` or `docset.yml` files** - You cannot reference individual markdown files
* **Uses crosslink syntax** - References to other repositories use the `repository://` syntax
* **Requires all TOC sections** - Dangling TOC sections are not allowed

## Basic structure

```yaml
toc:
  - toc: get-started
  - toc: elasticsearch-net://
  - toc: extend
    children:
      - toc: kibana://extend
        path_prefix: extend/kibana
      - toc: logstash://extend
        path_prefix: extend/logstash
      - toc: beats://extend
```

## Syntax notes

### Crosslink syntax

The TOC uses a similar [cross-link syntax to links](../syntax/links.md):

```yaml
- toc: elasticsearch-net://       # References entire repository
- toc: kibana://extend            # References 'extend' TOC section from kibana
```

### Implied suffixes

The `./docset.yml` or `/toc.yml` suffix is implied - the assembler will find the correct file for you:

### Special repository handling

The narrative repository `elastic/docs-content` is 'special', so omitting `scheme://` implies `docs-content://`:

```yaml
- toc: get-started                 # Implies docs-content://get-started
- toc: elasticsearch://setup       # Explicitly from elasticsearch repository
```

## Path prefixes

You must explicitly provide a URL path prefix when including a `toc`.

```yaml
- toc: extend
  children:
    - toc: kibana://extend
      path_prefix: extend/kibana      # Override default path
    - toc: logstash://extend
      path_prefix: extend/logstash
    - toc: beats://extend
      path_prefix: extend/beats
```

This allows you to:
* Group content from different repositories under a common path
* Avoid URL conflicts when combining repositories
* Create product-specific URL structures

## Reorganization independence

These `toc` sections can be reorganized independently of their position in their origin documentation set navigation. This allows sections from different repositories to be grouped together in the global navigation.

### Example: Cross-repository organization

You can create a unified section that combines content from multiple repositories:

```yaml
toc:
  - toc: monitoring
    children:
      - toc: elasticsearch://monitoring
        path_prefix: monitoring/elasticsearch
      - toc: kibana://monitoring
        path_prefix: monitoring/kibana
      - toc: beats://monitoring
        path_prefix: monitoring/beats
```

Even though each repository defines its own `monitoring` section, the global navigation presents them as a cohesive monitoring guide.

## Dangling TOC sections

All `toc` sections must be linked in `navigation.yml`.

**Dangling `toc` sections are not allowed** and the assembler build will report an error if it finds any.

This ensures:
* No content is accidentally excluded from the site
* Navigation references are always valid
* Documentation coverage is complete
* Every TOC section defined in a `docset.yml` appears somewhere in the global navigation

### Example of validation

If a repository defines:

```yaml
# my-repo/docs/docset.yml
toc:
  - file: index.md
  - toc: getting-started
  - toc: advanced
```

Then `navigation.yml` must reference both `getting-started` and `advanced`:

```yaml
# navigation.yml
toc:
  - toc: my-repo://getting-started
  - toc: my-repo://advanced
```

If either is missing, the build will fail with an error about dangling TOC sections.

## Validation

When building assembled documentation, `docs-builder` validates:

* All referenced TOC sections exist
* No TOC sections are dangling (unreferenced)
* Path prefixes don't conflict
* Crosslink references resolve correctly

Validation errors will cause the assembler build to fail.

## Navigation metadata

You can customize how sections appear in global navigation:

### Nested organization

Create nested navigation structures:

```yaml
toc:
  - toc: getting-started
    children:
      - toc: elasticsearch://quickstart
      - toc: kibana://quickstart
  - toc: reference
    children:
      - toc: elasticsearch://apis
      - toc: elasticsearch://settings
```

## Build process integration

During an assembler build:

1. `docs-builder` reads `navigation.yml`
2. It resolves all TOC section references across repositories
3. It validates that all sections are accounted for (no dangling sections)
4. It builds each documentation set with knowledge of its global path prefix
5. It generates the final site with unified navigation

## Related concepts

* [Documentation Set Navigation](documentation-set-navigation.md) - How individual repositories organize their content
* [Assembled Documentation](assembled-documentation.md) - The build process that uses global navigation
* [Site Configuration](../configure/site/index.md) - Complete site configuration reference
* [Navigation Configuration](../configure/site/navigation.md) - Detailed navigation.yml reference
* [Cross-link syntax](../syntax/links.md) - Understanding the repository:// syntax
