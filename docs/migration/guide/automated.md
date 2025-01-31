# Migrate Automated Content to V3

If you have automated documentation in Asciidoc (or any other format) that you need to migrate to Elastic docs V3, this guide walks you through the essentials. Elastic docs V3 (powered by `docs-builder`) allows engineering teams to keep code and reference docs closely integrated for easier updates and greater accuracy.

## Minimum Viable Setup

You can build docs with `docs-builder` using just two files:

1. **`docset.yml`** — Configures the docset for `docs-builder`.
2. **`index.md`** — A Markdown file that will be converted to HTML.

Once you have these files, follow the [Contribute Locally guide](../../contribute/locally.md) to get your docs building.

## Syntax

Elastic docs V3 fully supports [CommonMark](https://commonmark.org/) Markdown syntax. In addition, we support:

* Custom directives — our main extension point over markdown (learn more [here](../../syntax/md-extensions.md#directives))
* A few GitHub flavored markdown extensions (see the list [here](../../syntax/md-extensions.md#github-flavored-markdown))

In most cases, plain Markdown covers basic needs, and directives add extra functionality as needed.

- At a minimum, each page must have an H1 heading:
    ```markdown
    # I'm a heading
    ```
- You can optionally include [frontmatter](../../syntax/frontmatter.md) for additional metadata.

For more details on custom directives, see the [Syntax Guide](../../syntax/index.md).

## Navigation

Your `docset.yml` file configures how `docs-builder` handles navigation. Below is a minimal example:

```yaml
project: 'PROJECT_NAME'

toc:
  - file: index.md
```

For more information, see [Navigation](../../configure/content-set/navigation.md).

## Next steps

That’s all you need to get started migrating automated docs to V3. As you add more pages or custom features, refer to the linked resources for details on configuring your docset, refining navigation, and leveraging the full power of V3 directives.