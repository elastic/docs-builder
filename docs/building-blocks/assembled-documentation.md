---
navigation_title: Assembled documentation
---

# Assembled documentation

Assembled documentation is the product of building many [documentation sets](documentation-set.md) and weaving them into a global navigation to produce the fully assembled documentation site.

## How it works

The assembler:

1. Clones multiple documentation repositories.
2. Reads the [configuration files](../configure/site/index.md) and builds [a global navigation](../configure/site/navigation.md).
3. Builds each documentation set independently using the global configuration and navigation to inform path prefixes.
4. Produces a unified documentation website.

## Configuration

Assembled documentation is configured through the site configuration, which defines:

* [assembler.yml](../configure/site/index.md): Which repositories to include and [their branching strategy](../contribute/branching-strategy.md)
* [navigation.yml](../configure/site/index.md): Navigation and url prefixes for TOC's.
* [versions.yml](../configure/site/versions.md): Defines the various versioning schemes of products/solutions being documented
* [products.yml](../configure/site/products.md): Defines the product catalog (id, name) and ties it to a specific versioning scheme

Refer to [Site Configuration](../configure/site/index.md) for details on configuring assembled documentation.

:::{important}
The `docs-builder` command makes no assumptions about how repositories, products, solutions and versions tie into each other.
:::

## Build process

The typical build process for assembled documentation consists of three steps:

1. Clone all configured repositories using `docs-builder assembler clone`.
2. Build all documentation sets using `docs-builder assembler build`.
3. Serve the documentation on http://localhost:4000 using `docs-builder assembler serve`.

This uses the embedded configuration files inside the `docs-builder` binary. To build a specific configuration:

1. Fetch the latest config to `$(pwd)/config` `docs-builder assembler config init --local`.
2. Clone all configured repositories using `docs-builder assembler clone --local`.
3. Build all documentation sets using `docs-builder assembler build --local`.
4. Serve the documentation on http://localhost:4000 using `docs-builder assembler serve`.

## Related concepts

* [Documentation set](documentation-set.md): The individual units being assembled.
* [Distributed documentation](distributed-documentation.md): How documentation sets work independently.
* [Link catalog](link-catalog.md): How the assembler knows what to include.
