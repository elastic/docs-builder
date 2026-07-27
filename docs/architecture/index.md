---
navigation_title: Service architecture
---

# Service architecture

This section explains the services, infrastructure, and core concepts that power docs-builder's distributed documentation system. Understanding these components helps you work effectively with cross-repository linking, build isolation, and content assembly.

## Documentation model

### [Documentation set](documentation-set.md)

The fundamental unit of documentation — a single folder containing the docs for one repository. Each documentation set can be built, versioned, and maintained independently.

### [Assembled documentation](assembled-documentation.md)

How multiple documentation sets are combined into a unified site with global navigation. Used by [assembler builds](../documentation/assembler.md).

### [Distributed documentation](distributed-documentation.md)

The architectural approach that enables independent builds across repositories while maintaining link integrity. The foundation for all three build modes.

## Link infrastructure

The link infrastructure is what makes distributed builds possible. Each repository publishes a link index after successful builds, enabling cross-repo validation without synchronized builds.

### [Link service](link-service.md)

The central S3-backed storage where link index files are published and served.

### [Link index](link-index.md)

A JSON file (`links.json`) containing all linkable resources for a repository branch. Published to the link service after each successful build.

### [Link catalog](link-catalog.md)

A catalog listing all available link index files across all repositories and branches. Used by the assembler to coordinate builds.

### [Outbound cross-links](outbound-cross-links.md)

Links from your documentation to other documentation sets. Validated against published link index files.

### [Inbound cross-links](inbound-cross-links.md)

Links from other documentation sets to yours. Validated to prevent breaking changes when content moves.

## Navigation

### [Documentation set navigation](documentation-set-navigation.md)

How individual documentation sets organize content through TOC sections in `docset.yml` and `toc.yml`.

### [Global navigation](global-navigation.md)

How multiple documentation sets are organized together in assembled documentation through `navigation.yml`.

## How it all works together

1. Each repository builds its documentation set independently.
2. Successful builds publish a link index to the link service.
3. The link catalog tracks all available link index files.
4. Documentation builds validate cross-links against these link index files.
5. The assembler (or codex) combines documentation sets into a unified experience.
6. Teams work independently while maintaining link integrity across repositories.
