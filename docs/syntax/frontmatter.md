# Frontmatter

Every Markdown file referenced in the TOC may optionally define a frontmatter block.
Frontmatter is YAML-formatted metadata about a page, at the beginning of each file
and wrapped by `---` lines.

```yaml
---
navigation_title: This is the navigation title <1>
description: This is a description of the page <2>
applies_to: <3>
  serverless: all
---
```
1. [`navigation_title`](#navigation-title)
2. [`description`](#description)
3. [`applies_to`](#applies-to)

## Navigation Title
See [](./titles.md)

## Description

Use the `description` frontmatter to set the description meta tag for a page. 
This helps search engines and social media.
It also sets the `og:description` and `twitter:description` meta tags.

The `description` frontmatter is a string, recommended to be around 150 characters. If you don't set a `description`, 
it will be generated from the first few paragraphs of the page until it reaches 150 characters.

## Applies to
See [](./applies.md)
