---
navigation_title: Content set-level
---

# Content set-level configuration

Elastic documentation is spread across multiple repositories. Each repository can contain one or more content sets. A content set is a group of documentation pages whose navigation is defined by a single `docset.yml` file. `docs-builder` builds each content set in isolation, ensuring that changes in one repository don’t affect another.

A content set in `docs-builder` is equivalent to an AsciiDoc book. At this level, the system consists of:

| System property                                                                                                                                        | Asciidoc                                                                                                                                              | V3                                                            |
|--------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------|
| **Content source files** --> A whole bunch of markup files as well as any other assets used in the docs (for example, images, videos, and diagrams).   | **Markup**: AsciiDoc files **Assets**: Images, videos, and diagrams                                                                                   | **Markup**: MD files **Assets**: Images, videos, and diagrams |
| **Information architecture** --> A way to specify the order in which these text-based files should appear in the information architecture of the book. | `index.asciidoc` file (this can be spread across several AsciiDoc files, but generally starts with the index file specified in the `conf.yaml` file)) | `docset.yml` and/or `toc.yml` file(s)                         |

## Reference

* [`docset.yml` reference](./docset-reference.md) — all top-level configuration keys
* [TOC reference](./toc-reference.md) — syntax for `toc` array entries
* [Attributes](./attributes.md) — `subs` substitutions
* [Extensions](./extensions.md) — optional content-set extensions
* [API Explorer](./api-explorer.md) — `api` OpenAPI configuration
* [CTA](./cta.md) — right-gutter call-to-action templates

## Layout

* [File structure](./file-structure.md) — how directories map to URLs
* [Navigation layout](./navigation.md) — structuring the `toc` tree

## Learn more

* [Documentation set navigation](../../building-blocks/documentation-set-navigation.md) — patterns, trade-offs, and worked examples
