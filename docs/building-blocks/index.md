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

### Documentation Set Navigation

A documentation set is responsible for defining how files are organized in the navigation. This is done by defining a `toc` section in the `docset.yml` file.

```yaml
toc:
  - file: index.md
  - folder: contribute
    children:
      - file: index.md
      - file: locally.md
        children:
          - file: page.md
```

If the `toc` section becomes too unwieldy folders can define a dedicated `toc.yml` file to organize their files and link them in their parent `toc.yml` or `docset.yml` file. 

```yaml
toc:
  - file: index.md
  - folder: contribute
    children:
      - file: index.md
      - file: locally.md
        children:
          - file: page.md
  - toc: development
```

Where `development/toc.yml` is defined as:

```yaml
toc:
  - file: index.md 
  - toc: link-validation
```

:::{note}
The folder name `development` is not repeated in the `toc.yml` file this allows for easier renames of the folder itself.
:::

Read more details in the reference for [docset.yml](../configure/content-set/index.md)

### Global Navigation

The global navigation is defined in the [`navigation.yml`](../configure/site/navigation.md) file. It follows a very similar 
`toc` configuration structure to the documentation set navigation. 

It comes, however, with the following restrictions:

* It may only link to `toc.yml` or `docset.yml` files

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

Some syntactic notes:

* The toc uses a similar [cross-link syntax to links](../syntax/links.md)
* The `./docset.yml` or `/toc.yml` suffix is implied, assembler will find the correct file for you.
* The narrative repository `elastic/docs-content` is 'special' so omitting `scheme://` implies `docs-content://`.

These `toc` sections can be reorganized independently of their position in their origin documentation set navigation.
This allows sections from different repositories to be grouped together in the global navigation.

All `toc` sections must be linked in `navigation.yml`.
Dangling `toc` sections are **not** allowed and the assembler build will report an error if it finds any. 

Read more details in the reference for [navigation.yml](../configure/site/navigation.md)

## How it all works together

1. Each repository builds its documentation set independently
2. Successful builds publish a Link Index to the Link Service
3. The Link Catalog catalogs all available Link Index files
4. Documentation builds validate crosslinks using these Link Index files
5. The assembler combines documentation sets using the Link Catalog
6. Teams can work independently while maintaining link integrity across repositories
