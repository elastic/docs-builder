# Syntax guide

Elastic Docs V3 uses [Markdown](https://commonmark.org) as its main content authoring format.

:::{admonition} New to Markdown?
[Learn Markdown in just 10 minutes](https://commonmark.org/help/).
:::

V3 fully supports [CommonMark](https://commonmark.org/), a strongly defined, standard-compliant Markdown specification. In many cases, plain Markdown syntax will be sufficient when authoring Elastic content. On top of this functionality, there are two main syntax extension points:

* [Directives](#directives)
* [GitHub-flavored markdown](#github-flavored-markdown)

## Directives

Directives extend CommonMark functionality. Directives have the following syntax:

```markdown
:::{EXTENSION} ARGUMENTS
:OPTION: VALUE

Nested content that will be parsed as markdown
:::
```

- `EXTENSION` is the name of the directive. Names are always wrapped in brackets `{ }`. For example: [`{note}`](admonitions.md#note).
- `ARGUMENT` an optional main argument. For example: [`:::{include} _snippets/include.md :::`](file_inclusion.md)
- `:OPTION: VALUE` is used to further customize a given directive.

Defining directives with `:::` allows the nested markdown syntax to be highlighted properly by editors and web viewers.



### Nesting Directives

Increase the number of leading semicolons to include nested directives.

In the following example, a `{note}` wraps a `{hint}`:

```markdown
::::{note} My note
:::{hint} My hint
Content displayed in the hint admonition
:::
Content displayed in the note admonition
::::
```

## Literal directives

All directive are indicated with semicolons except literal blocks. For these you need to use triple backticks.

* [Code blocks](code.md)
* [{applies-to} blocks](applies.md)

Since their contents **should not** be parsed as markdown they use backticks. This also ensures maximum interopability with existing markdown editors and previews.

Many Markdown editors support syntax highlighting for embedded code blocks. For compatibility with this feature, use triple backticks instead of triple colons for content that needs to be displayed literally:

````markdown
```js
const x = 1;
```
````

## GitHub Flavored Markdown

We support _some_ [GitHub Flavored Markdown (GFM) extensions](https://github.github.com/gfm/).

### Supported

* [](tables.md#basic-table)
* Strikethrough: ~~as seen here~~

### Not supported

* Task lists
* Auto links (http://www.elastic.co)
* Using a subset of HTML
