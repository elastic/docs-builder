# Syntax guide

Learn about the Markdown syntax used in {{dbuild}} documentation.

## Quick reference

Refer to the [quick reference](quick-ref.md) for a condensed syntax cheat sheet.

## How it works

{{dbuild}} uses [CommonMark](https://commonmark.org)-compliant Markdown extended with [MyST](https://mystmd.org/)-inspired directives and roles. We are not using MyST directly — {{dbuild}} has its own implementation of the directive and role extension points.

If you know [Markdown](https://commonmark.org), you already know most of what you need. If not, the CommonMark project offers a [10-minute tutorial](https://commonmark.org/help/).

When you need more than basic Markdown, you can use _directives_ to add features like callouts, tabs, diagrams, and more. To learn how directives work in general, including how to add options, arguments, and nest multiple directives, refer to [How directives work](directives.md). For a full list of available directives, refer to the sidebar.

## GitHub Flavored Markdown support

{{dbuild}} supports some GitHub Flavored Markdown extensions:

**Supported:**
- Tables (basic pipe syntax)
- Strikethrough with `~~text~~` (renders as ~~text~~)

**Not supported:**
- Automatic URL linking: https://www.elastic.co
  - Links must use standard Markdown syntax: [Elastic](https://www.elastic.co)
- Using a subset of HTML
