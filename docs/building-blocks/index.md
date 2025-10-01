---
navigation_title: Building Blocks
---

# Building Blocks

This section explains the core concepts and building blocks that make up the docs-builder architecture. Understanding these concepts will help you work effectively with distributed documentation and cross-repository linking.

## Core concepts

### [Documentation Set](documentation-set.md)

The fundamental unit of documentation - a single folder containing the documentation for one repository. Each documentation set can be built, versioned, and maintained independently.

### [Assembled Documentation](assembled-documentation.md)

The product of combining multiple documentation sets into a unified documentation website with global navigation. This enables a seamless user experience across multiple repositories.

### [Distributed Documentation](distributed-documentation.md)

The architectural approach that allows documentation sets to be built independently while maintaining link integrity across repositories. This enables teams to work autonomously without blocking each other.

## Link management infrastructure

### [Link Service](link-service.md)

The central S3-backed service where Link Index files are published and stored. This enables distributed validation and build resilience.

### [Link Index](link-index.md)

A JSON file (`links.json`) containing all linkable resources for a specific repository branch. Published to the Link Service after each successful build.

### [Link Catalog](link-catalog.md)

A catalog file listing all available Link Index files across all repositories and branches. Used by the assembler to coordinate builds and by documentation builds for validation.

## Cross-repository linking

### [Outbound Crosslinks](outbound-crosslinks.md)

Links from your documentation to other documentation sets. Validated against published Link Index files to ensure they're correct.

### [Inbound Crosslinks](inbound-crosslinks.md)

Links from other documentation sets to yours. Validated to prevent breaking changes when you move or delete content.

## Navigation

### [Documentation Set Navigation](documentation-set-navigation.md)

How individual documentation sets organize their content through TOC sections in `docset.yml` and `toc.yml` files. Each repository controls its own internal navigation structure, including file and folder organization.

### [Global Navigation](global-navigation.md)

How multiple documentation sets are organized together in assembled documentation through `navigation.yml`. Uses crosslink syntax to reference TOC sections from different repositories and enables cross-repository content organization.

## How it all works together

1. Each repository builds its documentation set independently
2. Successful builds publish a Link Index to the Link Service
3. The Link Catalog catalogs all available Link Index files
4. Documentation builds validate crosslinks using these Link Index files
5. The assembler combines documentation sets using the Link Catalog
6. Teams can work independently while maintaining link integrity across repositories
