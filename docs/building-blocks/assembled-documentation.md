---
navigation_title: Assembled Documentation
---

# Assembled Documentation

**Assembled documentation** is the product of building many [documentation sets](documentation-set.md) and weaving them into a global navigation to produce the fully assembled documentation site.

## How it works

The assembler:

1. Clones multiple documentation repositories
2. Builds each documentation set independently
3. Combines them according to a global navigation configuration
4. Produces a unified documentation website

## Benefits

By assembling multiple documentation sets together, you can:

* **Centralize navigation** - Present a unified experience across multiple repositories
* **Cross-link content** - Link between different documentation sets seamlessly
* **Version coordination** - Control which versions of different repositories appear together
* **Product-level organization** - Organize documentation by product rather than repository

## Configuration

Assembled documentation is configured through the site configuration, which defines:

* Which repositories to include
* What versions of each repository to build
* How to organize the global navigation
* URL structure and routing

See [Site Configuration](../configure/site/index.md) for complete details on configuring assembled documentation.

## Build process

The typical build process for assembled documentation:

1. **Clone** - Clone all configured repositories using `docs-builder assembler clone`
2. **Build** - Build all documentation sets using `docs-builder assembler build`
3. **Export** - Export the assembled site in various formats (HTML, Elasticsearch index, etc.)

## Related concepts

* [Documentation Set](documentation-set.md) - The individual units being assembled
* [Distributed Documentation](distributed-documentation.md) - How documentation sets work independently
* [Link Registry](link-registry.md) - How the assembler knows what to include
