---
navigation_title: Configuration reference
---

# Configure Elastic Docs

It's first important to understand how we build Elastic Docs:

| System property | Asciidoc | V3 |
| -------------------- | -------------------- | -------------------- |
| **Docs build tool** --> An engine used to take the markup in the content sources and transform it into web pages. | Customized version of AsciiDoctor (lives in [**elastic/docs**](https://github.com/elastic/docs)) | Customized doc builder using open source tools (lives in [**elastic/docs-builder**](https://github.com/elastic/docs-builder)) |

When working with `docs-builder`, there are three levels at which you can configure Elastic documentation:

1. Site-level
2. Content set-level
3. Page-level

## Site-level configuration

At the site level, you can configure:

* Content sources: where files live
* Global navigation: how navigations are compiled and presented to users

[Site configuration](./site/index.md)

## Content-level configuration

At the content set level, you can configure:

* Content-set-level and sub-content-set-level navigation: how smaller groups of files are organized and presented to users
* Attributes: variables that will be substituted at build-time for pre-defined values

[Content-set configuration](./content-set/index.md)

## Page-level configuration

At the page level, you can configure:

* Frontmatter that influences on-page UX to benefit the user in some way

[Page configuration](./page.md)