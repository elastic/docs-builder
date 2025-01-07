---
title: Site configuration
navigation_title: Site
---

Start by understanding how the new V3 system works at the site level compared to how our custom AsciiDoctor system works. The system consists of:


| System property | Asciidoc | V3 |
| -------------------- | -------------------- | -------------------- |
| **Content sources** --> Collections of markup files containing doc content. These are split up across many docs repos. | _Books_ | _Content sets_ |
| **Content configuration** --> A way to specify where to find those content sources, and in what order they should be added to the site. | Configuration file ([`conf.yml`](https://github.com/elastic/docs/blob/master/conf.yaml) in elastic/docs) | Config file location TBD |
| **Cross-site values** --> Key-value pairs that should be substituted across all sources as web pages are built. | Shared attributes ([`shared/`](https://github.com/elastic/docs/tree/master/shared) in elastic/docs) | Shared attrs file TBD |
| **Docs build tool** --> An engine used to take the markup in the content sources and transform it into web pages. | Customized version of AsciiDoctor (lives in [**elastic/docs**](https://github.com/elastic/docs)) | Customized doc builder using open source tools (lives in [**elastic/docs-builder**](https://github.com/elastic/docs-builder)) |

Where these pieces live and what format they are in varies between the two systems, but they generally achieve the same goal.

## Asciidoc

![site-level config in the asciidoc system](./img/site-level-asciidoctor.png)

## V3

DIAGRAM NEEDED