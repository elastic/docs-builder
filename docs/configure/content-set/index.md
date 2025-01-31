---
navigation_title: Content set-level
---

# Content set-level configuration

Elastic documentation is spread across multiple repositories. Each repository can contain one or more content sets. A content set is a group of documentation pages whose navigation is defined by a single `docset.yml` file. `docs-builder` builds each content set in isolation, ensuring that changes in one repository don’t affect another.

A content set in `docs-builder` is equivalent to an AsciiDoc book. At this level, the system consists of:

| System property | Asciidoc | V3 |
| -------------------- | -------------------- | -------------------- |
| **Content source files** --> A whole bunch of markup files as well as any other assets used in the docs (for example, images, videos, and diagrams). | **Markup**: AsciiDoc files **Assets**: Images, videos, and diagrams | **Markup**: MD files **Assets**: Images, videos, and diagrams |
| **Information architecture** --> A way to specify the order in which these text-based files should appear in the information architecture of the book. | `index.asciidoc` file (this can be spread across several AsciiDoc files, but generally starts with the index file specified in the `conf.yaml` file)) | `docset.yml` and/or `toc.yml` file(s) |

## Learn more

* [File structure](./file-structure.md)
* [Navigation](./navigation.md)
* [Attributes](./attributes.md)