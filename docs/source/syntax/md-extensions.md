---
title_navigation: Markdown Syntax
---

# Markdown Syntax Extensions

## Syntax

The new documentation fully supports [CommonMark](https://commonmark.org/) Markdown syntax. 

On top of this widely accepted feature set we have various extensions points. 

## Directives 

Directives are our main extension point over markdown and follows the following syntax


```markdown
:::{EXTENSION} ARGUMENTS
:OPTION: VALUE

Nested content that will be parsed as markdown
:::
```

- `EXTENSION` is the name of the directive e.g [`note`](admonitions.md#note)
- `ARGUMENT` some directives take a main argumnt e.g [`:::{include} _snippets/include.md`](file_inclusion.md)
- `OPTION` and `VALUE` can be used to further customize a given directive.

The usage of `:::` allows the nested markdown to be syntax highlighted properly by editors and web viewers.

Our (directives) are always wrapped in brackets `{ }`.

### Nesting Directives

You can increase the leading semicolons to include nested directives. Here the `{note}` wraps a `{hint}`.

```markdown
::::{note} My note
:::{hint} My hint
Content displayed in the hint admonition
:::
Content displayed in the note admonition
::::
```

## Literal directives

For best editor compatability it is recommended to use triple backticks for content that needs to be displayed literally

````markdown
```js
const x = 1;
```
````

Many markdown editors support syntax highlighting for embedded code blocks.

## Github Flavored Markdown

We support some of GitHub flavor markdown extensions these are highlighted in green here: https://github.github.com/gfm/

Supported:

* GFM Tables [](tables.md#github-flavored-markdown-gfm-table)
* Strikethrough ~~as seen here~~

Not supported:

* Task lists
* Auto links (http://www.elastic.co)
* Using a subset of html 

