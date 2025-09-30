---
navigation_title: Documentation Set
---

# Documentation Set

A **documentation set** is a single folder containing the documentation of a single repository. This is the fundamental unit of documentation in the docs-builder system.

## Minimum requirements

At a minimum, a documentation set folder must contain:

* `docset.yml` - The configuration file that defines the structure and metadata of the documentation set
* `index.md` - The entry point or landing page for the documentation set

## Purpose

Documentation sets allow each repository to maintain its own documentation independently. Each set can be:

* Built independently
* Versioned separately
* Maintained by different teams
* Published to its own schedule

## Structure

A typical documentation set might look like:

```
my-repo/
└── docs/
    ├── docset.yml
    ├── index.md
    ├── getting-started.md
    ├── configuration/
    │   ├── index.md
    │   └── advanced.md
    └── reference/
        └── api.md
```

## Configuration

The `docset.yml` file controls how the documentation set is structured and built. See [Content Set Configuration](../configure/content-set/index.md) for complete configuration details.

## Related concepts

* [Assembled Documentation](assembled-documentation.md) - How multiple documentation sets are combined
* [Link Index](link-index.md) - How documentation sets publish their linkable resources
