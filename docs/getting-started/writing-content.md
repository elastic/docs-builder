---
navigation_title: Writing content
---

# Writing content

{{dbuild}} uses [CommonMark](https://commonmark.org)-compliant Markdown. If you know Markdown, you already know most of what you need. If not, the CommonMark project offers a [10-minute tutorial](https://commonmark.org/help/).

## The basics

Standard Markdown works as you'd expect:

### Text formatting

| Syntax | Result |
|---|---|
| `**bold**` | **bold** |
| `_italic_` | _italic_ |
| `` `code` `` | `code` |
| `~~strikethrough~~` | ~~strikethrough~~ |
| `H~2~O` | H~2~O |
| `4^th^` | 4^th^ |

### Headings

```markdown
# Page title (h1)
## Section (h2)
### Subsection (h3)
#### Deep heading (h4)
```

Every page must start with exactly one `#` heading. This becomes the page title.

### Links

```markdown
[Link text](https://example.com)
[Another page](./other-page.md)
```

See [links](../syntax/links.md) for the full link syntax.

### Lists

```markdown
- Unordered item
- Another item
  - Nested item

1. Ordered item
2. Another item
```

### Images

```markdown
![Alt text](images/screenshot.png)
```

### Code blocks

````markdown
```python
def hello():
    print("Hello, docs!")
```
````

Code blocks support [syntax highlighting, callout annotations, and console examples](../syntax/code.md).

### Quotations

```markdown
> We know what we are, but know not what we may be.
```

### Thematic breaks

```markdown
***
```

### GitHub Flavored Markdown

{{dbuild}} supports some [GitHub Flavored Markdown](https://github.github.com/gfm/) extensions — tables (pipe syntax) and strikethrough (`~~text~~`) — but not automatic URL linking or inline HTML. See the [full GFM support details](../syntax/index.md#github-flavored-markdown-support).

## Directives and roles

Beyond standard Markdown, {{dbuild}} uses [MyST](https://mystmd.org/)-inspired extension points — **directives** and **roles** — to add features like callouts, tabs, diagrams, and more. These are not MyST itself; {{dbuild}} has its own implementation of the directive and role syntax.

### Directive syntax

Directives extend Markdown with block-level features:

```markdown
:::{note}
This is a callout box that stands out from regular text.
:::
```

How it works:
- `:::` opens and closes the directive block
- `{note}` is the directive type (always in curly braces)
- Content inside is regular Markdown

Directives can take options:

```markdown
:::{image} screenshot.png
:alt: Dashboard overview
:width: 600px
:::
```

And can be nested by adding more colons to the outer directive:

```markdown
::::{note}
Outer content

:::{tip}
Inner content
:::

::::
```

For the full list of available directives and detailed syntax, see the [syntax guide](../syntax/index.md).

## What's next

- **[Pages and links](./pages-and-links.md)** — add pages to navigation and link between docs
- **[Serve and preview](./serve.md)** — preview your docs locally with hot reload
- **[Syntax reference](../syntax/index.md)** — full syntax documentation
- **[Quick reference](../syntax/quick-ref.md)** — condensed cheat sheet
