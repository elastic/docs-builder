// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``AuthoringTests``.``plain text``.``output tests``

open Xunit
open authoring

type ``basic text formatting`` () =
    static let markdown = Setup.Document """
This is **bold** and *italic* text.
"""

    [<Fact>]
    let ``strips bold and italic markers`` () =
        markdown |> convertsToPlainText """This is bold and italic text."""

type ``headings`` () =
    static let markdown = Setup.Document """
## Heading 2
### Heading 3
"""

    [<Fact>]
    let ``converts headings to plain text with bullet separator`` () =
        markdown |> convertsToPlainText """Heading 2 • Heading 3"""

type ``inline code`` () =
    static let markdown = Setup.Document """
This is `inline code` in a sentence.
"""

    [<Fact>]
    let ``strips backticks from inline code`` () =
        markdown |> convertsToPlainText """This is inline code in a sentence."""

type ``code blocks`` () =
    static let markdown = Setup.Document """
```python
def hello():
    print("Hello, world!")
```
"""

    [<Fact>]
    let ``renders code content with newlines collapsed to spaces`` () =
        markdown |> convertsToPlainText """def hello(): print("Hello, world!")"""

type ``links`` () =
    static let markdown = Setup.Document """
This is a [link to docs](https://www.elastic.co/docs) in a sentence.
"""

    [<Fact>]
    let ``outputs link text only without URL`` () =
        markdown |> convertsToPlainText """This is a link to docs in a sentence."""

type ``images`` () =
    static let markdown = Setup.Document """
![Alt text for image](https://example.com/image.png)
"""

    [<Fact>]
    let ``outputs image alt text only`` () =
        markdown |> convertsToPlainText """Alt text for image"""

type ``unordered lists`` () =
    static let markdown = Setup.Document """
- Item 1
- Item 2
- Item 3
"""

    [<Fact>]
    let ``renders list items with bullet separators`` () =
        markdown |> convertsToPlainText """Item 1 • Item 2 • Item 3"""

type ``ordered lists`` () =
    static let markdown = Setup.Document """
1. First item
2. Second item
3. Third item
"""

    [<Fact>]
    let ``renders list items with bullet separators`` () =
        markdown |> convertsToPlainText """First item • Second item • Third item"""

type ``nested lists`` () =
    static let markdown = Setup.Document """
- Item 1
  - Nested item 1
  - Nested item 2
- Item 2
"""

    [<Fact>]
    let ``renders nested list items as single line`` () =
        markdown |> convertsToPlainText """Item 1 • Nested item 1 • Nested item 2 • Item 2"""

type ``tables`` () =
    static let markdown = Setup.Document """
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |
| Cell 3   | Cell 4   |
"""

    [<Fact>]
    let ``renders tables as bullet-separated header-value pairs`` () =
        markdown |> convertsToPlainText """Header 1: Cell 1 • Header 2: Cell 2 • Header 1: Cell 3 • Header 2: Cell 4"""

type ``blockquotes`` () =
    static let markdown = Setup.Document """
> This is a quoted text
> that spans multiple lines.
"""

    [<Fact>]
    let ``strips blockquote markers`` () =
        markdown |> convertsToPlainText """This is a quoted text that spans multiple lines."""

type ``admonition directives`` () =
    static let markdown = Setup.Document """
:::{note}
This is a note admonition.
:::
"""

    [<Fact>]
    let ``renders admonition content as plain text`` () =
        markdown |> convertsToPlainText """This is a note admonition."""

type ``admonition with title`` () =
    static let markdown = Setup.Document """
:::{admonition} Custom Title
This is the admonition content.
:::
"""

    [<Fact>]
    let ``includes title and content with bullet separator`` () =
        markdown |> convertsToPlainText """Custom Title • This is the admonition content."""

type ``warning directive`` () =
    static let markdown = Setup.Document """
:::{warning}
This is a warning message.
:::
"""

    [<Fact>]
    let ``renders warning content as plain text`` () =
        markdown |> convertsToPlainText """This is a warning message."""

type ``tip directive`` () =
    static let markdown = Setup.Document """
:::{tip}
This is a helpful tip.
:::
"""

    [<Fact>]
    let ``renders tip content as plain text`` () =
        markdown |> convertsToPlainText """This is a helpful tip."""

type ``image directive`` () =
    static let markdown = Setup.Document """
```{image} /path/to/image.png
:alt: Descriptive alt text
```
"""

    [<Fact>]
    let ``outputs alt text only`` () =
        markdown |> convertsToPlainText """Descriptive alt text"""

type ``dropdown directive`` () =
    static let markdown = Setup.Document """
:::{dropdown} Dropdown Title
This is dropdown content.
:::
"""

    [<Fact>]
    let ``renders dropdown title and content with bullet separator`` () =
        markdown |> convertsToPlainText """Dropdown Title • This is dropdown content."""

type ``tabs directive`` () =
    static let markdown = Setup.Document """
::::{tab-set}

:::{tab-item} Tab 1
Content for tab 1.
:::

:::{tab-item} Tab 2
Content for tab 2.
:::

::::
"""

    [<Fact>]
    let ``renders all tab content with bullet separators`` () =
        markdown |> convertsToPlainText """Tab 1 • Content for tab 1. • Tab 2 • Content for tab 2."""

type ``definition list`` () =
    static let markdown = Setup.Document """
`Term 1`
:   Definition for term 1.

`Term 2`
:   Definition for term 2.
"""

    [<Fact>]
    let ``renders terms and definitions with bullet separators`` () =
        markdown |> convertsToPlainText """Term 1 • Definition for term 1. • Term 2 • Definition for term 2."""

type ``substitutions`` () =
    static let markdown = Setup.Document """---
sub:
  product-name: "Elasticsearch"
---

Welcome to {{product-name}}!
"""

    [<Fact>]
    let ``resolves substitutions`` () =
        markdown |> convertsToPlainText """Welcome to Elasticsearch!"""

type ``kbd role`` () =
    static let markdown = Setup.Document """
Press {kbd}`Ctrl+C` to copy.
"""

    [<Fact>]
    let ``renders keyboard shortcuts as readable text`` () =
        // Character keys preserve their case from input
        markdown |> convertsToPlainText """Press Ctrl + c to copy."""

type ``applies_to role`` () =
    static let markdown = Setup.Document """
This feature {applies_to}`stack: ga 7.0` is available.
"""

    [<Fact>]
    let ``renders applies_to as readable text`` () =
        markdown |> convertsToPlainText """This feature (Elastic Stack: Generally available since 7.0) is available."""

type ``applies_to block directive`` () =
    static let markdown = Setup.Document """
```{applies_to}
stack: ga 7.0
```
"""

    [<Fact>]
    let ``renders applies_to block as readable text`` () =
        markdown |> convertsToPlainText """(Elastic Stack: Generally available since 7.0)"""

type ``comments`` () =
    static let markdown = Setup.Document """
This text is visible.

% This is a comment

This text is also visible.
"""

    [<Fact>]
    let ``excludes comments from output`` () =
        markdown |> convertsToPlainText """This text is visible. • This text is also visible."""

type ``thematic break`` () =
    static let markdown = Setup.Document """
First section.

---

Second section.
"""

    [<Fact>]
    let ``renders thematic break as bullet separator`` () =
        markdown |> convertsToPlainText """First section. • Second section."""

type ``complex document`` () =
    static let markdown = Setup.Document """
This is a paragraph with **bold** and *italic* text.

```python
def hello():
    print("Hello!")
```

- List item 1
- List item 2

| Name | Value |
|------|-------|
| foo  | bar   |
"""

    [<Fact>]
    let ``renders complex document as single line`` () =
        markdown |> convertsToPlainText """This is a paragraph with bold and italic text. • def hello(): print("Hello!") • List item 1 • List item 2 • Name: foo • Value: bar"""

type ``include directive`` () =
    static let generator = Setup.Generate [
        Index """
```{include} _snippets/included.md
```
"""
        Markdown "_snippets/included.md" """
This is included content.
"""
    ]

    [<Fact>]
    let ``inlines included content`` () =
        generator |> convertsToPlainText """This is included content."""

type ``math block`` () =
    static let markdown = Setup.Document """
```{math}
E = mc^2
```
"""

    [<Fact>]
    let ``renders math content as text`` () =
        markdown |> convertsToPlainText """E = mc^2"""

type ``diagram directive`` () =
    static let markdown = Setup.Document """
::::{diagram} mermaid
flowchart LR
    A --> B
::::
"""

    [<Fact>]
    let ``skips diagram content`` () =
        // Diagrams are visual, not searchable text
        markdown |> convertsToPlainText """"""
