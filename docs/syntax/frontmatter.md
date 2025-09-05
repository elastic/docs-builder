# Frontmatter

Every Markdown file referenced in the TOC may optionally define a frontmatter block.
Frontmatter is YAML-formatted metadata about a page, at the beginning of each file
and wrapped by `---` lines.

In the frontmatter block, you can define the following fields:

```yaml
---
navigation_title: This is the navigation title <1>
description: This is a description of the page <2>
applies_to: <3>
  serverless: all
products: <4>
  - id: apm-agent
  - id: edot-sdk
---
```

1. [`navigation_title`](#navigation-title)
2. [`description`](#description)
3. [`applies_to`](#applies-to)
4. [`products`](#products)

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

## Products

The products frontmatter is a list of products that the page relates to.

:::{include} /_snippets/products-list.md
:::

`products` can also be defined in the [`docset.yml` file](/configure/content-set/navigation.md#products).
If you define `products` in a page's Markdown file and the `docset.yml` file also includes `products`, docs-builder will combine the two lists.
You can _not_ override doc set level `products` at the page level.

:::{tip}
`products` is distinct from `applies_to`, which is used to indicate feature availability and applicability.

% Is this true?
For example, while a page might contain content that is _applicable_ to Elastic Cloud Serverless projects,
it might not be _about_ Elastic Cloud Serverless. In that case, you would include the `serverless` key in `applies_to`,
but _not_ include the `cloud-serverless` ID in `products`:

```yaml
products:
  - id: apm
applies_to:
  stack: ga
  serverless:
    observability: ga
```
:::