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

## Enable static search

Isolated builds can include a Pagefind index that runs entirely in the browser. Enable it in `docset.yml`:

```yaml
features:
  static-search: true
```

The generated site needs to be served over HTTP, such as from S3 and CloudFront or a local static server:

```sh
python3 -m http.server --directory .artifacts/docs/html
```

Static search does not require a search API, but it does not work in the on-demand `docs-builder serve` mode or when pages are opened directly with `file://`.

## Learn more

* [File structure](./file-structure.md).
* [Navigation](./navigation.md).
* [Attributes](./attributes.md).
* [Extensions](./extensions.md).
* [API Explorer](../../schema-support/api-explorer/index.md).
* [CTA](./cta.md).