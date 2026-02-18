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
    let ``converts headings to plain text without hash symbols`` () =
        markdown |> convertsToPlainText """Heading 2

Heading 3"""

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
    let ``renders code content without fences`` () =
        markdown |> convertsToPlainText """def hello():
    print("Hello, world!")"""

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
    let ``renders list items without bullets`` () =
        markdown |> convertsToPlainText """Item 1
Item 2
Item 3"""

type ``ordered lists`` () =
    static let markdown = Setup.Document """
1. First item
2. Second item
3. Third item
"""

    [<Fact>]
    let ``renders list items without numbers`` () =
        markdown |> convertsToPlainText """First item
Second item
Third item"""

type ``nested lists`` () =
    static let markdown = Setup.Document """
- Item 1
  - Nested item 1
  - Nested item 2
- Item 2
"""

    [<Fact>]
    let ``renders nested list items as plain text`` () =
        markdown |> convertsToPlainText """Item 1
Nested item 1
Nested item 2
Item 2"""

type ``tables`` () =
    static let markdown = Setup.Document """
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |
| Cell 3   | Cell 4   |
"""

    [<Fact>]
    let ``renders tables as header-value pairs`` () =
        markdown |> convertsToPlainText """Header 1: Cell 1
Header 2: Cell 2
Header 1: Cell 3
Header 2: Cell 4"""

type ``blockquotes`` () =
    static let markdown = Setup.Document """
> This is a quoted text
> that spans multiple lines.
"""

    [<Fact>]
    let ``strips blockquote markers`` () =
        // Soft line breaks become spaces in plain text output
        markdown |> convertsToPlainText """This is a quoted text that spans multiple lines."""

type ``admonition directives`` () =
    static let markdown = Setup.Document """
:::{note}
This is a note admonition.
:::
"""

    [<Fact>]
    let ``renders admonition content without XML tags`` () =
        markdown |> convertsToPlainText """This is a note admonition."""

type ``admonition with title`` () =
    static let markdown = Setup.Document """
:::{admonition} Custom Title
This is the admonition content.
:::
"""

    [<Fact>]
    let ``includes title and content`` () =
        markdown |> convertsToPlainText """Custom Title

This is the admonition content."""

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
    let ``renders dropdown title and content`` () =
        markdown |> convertsToPlainText """Dropdown Title

This is dropdown content."""

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
    let ``renders all tab content`` () =
        markdown |> convertsToPlainText """Tab 1

Content for tab 1.

Tab 2

Content for tab 2."""

type ``definition list`` () =
    static let markdown = Setup.Document """
`Term 1`
:   Definition for term 1.

`Term 2`
:   Definition for term 2.
"""

    [<Fact>]
    let ``renders terms and definitions as plain text`` () =
        markdown |> convertsToPlainText """Term 1
Definition for term 1.

Term 2
Definition for term 2."""

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
        markdown |> convertsToPlainText """This text is visible.

This text is also visible."""

type ``thematic break`` () =
    static let markdown = Setup.Document """
First section.

---

Second section.
"""

    [<Fact>]
    let ``renders thematic break as blank line`` () =
        markdown |> convertsToPlainText """First section.


Second section."""

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
    let ``renders complex document as clean text`` () =
        markdown |> convertsToPlainText """This is a paragraph with bold and italic text.

def hello():
    print("Hello!")

List item 1
List item 2

Name: foo
Value: bar"""

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

type ``mermaid code block`` () =
    static let markdown = Setup.Document """
```mermaid
flowchart LR
    A --> B
```
"""

    [<Fact>]
    let ``renders mermaid as code content`` () =
        // Mermaid code blocks render like any code block
        markdown |> convertsToPlainText """flowchart LR
A --> B"""

type ``csv-include directive`` () =
    static let generator = Setup.Generate [
        Index """
:::{csv-include} data/users.csv
:::
"""
        File("data/users.csv", """Name,Age,City
John Doe,30,New York
Jane Smith,25,Los Angeles""")
    ]

    [<Fact>]
    let ``renders csv as header-value pairs`` () =
        generator |> convertsToPlainText """Name: John Doe
Age: 30
City: New York
Name: Jane Smith
Age: 25
City: Los Angeles"""

type ``csv-include directive with caption`` () =
    static let generator = Setup.Generate [
        Index """
:::{csv-include} data/products.csv
:caption: Product List
:::
"""
        File("data/products.csv", """Product,Price
Widget,9.99
Gadget,19.99""")
    ]

    [<Fact>]
    let ``includes caption in output`` () =
        generator |> convertsToPlainText """Product List
Product: Widget
Price: 9.99
Product: Gadget
Price: 19.99"""

type ``realistic documentation page`` () =
    static let markdown = Setup.Document """
## Getting Started with Elasticsearch

Elasticsearch is a distributed search and analytics engine. This guide helps you get started quickly.

:::{note}
Make sure you have Java 17 or later installed before proceeding.
:::

### Installation

You can install Elasticsearch using several methods:

- **Download directly** from the [official website](https://elastic.co/downloads)
- Use **package managers** like `apt` or `yum`
- Run with **Docker** for containerized environments

### Quick Start

After installation, start the service:

```bash
./bin/elasticsearch
```

Verify it's running by visiting `http://localhost:9200` in your browser.

### Basic Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `cluster.name` | elasticsearch | Name of your cluster |
| `node.name` | auto-generated | Unique node identifier |

:::{tip}
Press {kbd}`Ctrl+C` to stop the server gracefully.
:::

### Next Steps

1. Create your first index
2. Index some documents
3. Run your first search query

For more details, see the **Configuration Guide** and **API Reference**.
"""

    [<Fact>]
    let ``renders realistic page with preserved newlines`` () =
        markdown |> convertsToPlainText """Getting Started with Elasticsearch

Elasticsearch is a distributed search and analytics engine. This guide helps you get started quickly.

Make sure you have Java 17 or later installed before proceeding.

Installation

You can install Elasticsearch using several methods:

Download directly from the official website
Use package managers like apt or yum
Run with Docker for containerized environments

Quick Start

After installation, start the service:

./bin/elasticsearch

Verify it's running by visiting http://localhost:9200 in your browser.

Basic Configuration

Setting: cluster.name
Default: elasticsearch
Description: Name of your cluster
Setting: node.name
Default: auto-generated
Description: Unique node identifier

Press Ctrl + c to stop the server gracefully.

Next Steps

Create your first index
Index some documents
Run your first search query

For more details, see the Configuration Guide and API Reference."""
