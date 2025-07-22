// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``llm markdown``.``output tests``

open Swensen.Unquote
open Xunit
open authoring

type ``basic text formatting`` () =
    static let markdown = Setup.Document """
This is **bold** and *italic* text.
"""

    [<Fact>]
    let ``converts to standard markdown`` () =
        markdown |> convertsToNewLLM """This is **bold** and *italic* text.
"""

type ``headings`` () =
    static let markdown = Setup.Document """
## Heading 2
### Heading 3
"""

    [<Fact>]
    let ``converts to standard markdown headings`` () =
        markdown |> convertsToNewLLM """
## Heading 2


### Heading 3
"""

type ``code blocks`` () =
    static let markdown = Setup.Document """
```python
def hello():
    print("Hello, world!")
    
    
```
"""

    [<Fact>]
    let ``renders code blocks`` () =
        markdown |> convertsToNewLLM """
```python
def hello():
    print("Hello, world!")
```
"""

type ``enhanced code blocks`` () =
    static let markdown = Setup.Document """
```python title="Hello World Example"
def hello():
    print("Hello, world!") # <1>
```

1. This is a callout
"""

    [<Fact>]
    let ``converts to code block with optional caption comment`` () =
        markdown |> convertsToNewLLM """
```python
def hello():
    print("Hello, world!")
```
"""

type ``lists`` () =
    static let markdown = Setup.Document """
- Item 1
- Item 2
  - Nested item 1
  - Nested item 2
    - Nested Nested item 1
    - Nested Nested item 2
- Item 3

1. Ordered item 1
2. Ordered item 2
   1. Nested ordered item 1
   2. Nested ordered item 2
      - Nested unordered item 1
      - Nested unordered item 2
3. Ordered item 3
"""

    [<Fact>]
    let ``converts to standard markdown lists`` () =
        markdown |> convertsToNewLLM """
- Item 1
- Item 2
  - Nested item 1
  - Nested item 2
    - Nested Nested item 1
    - Nested Nested item 2
- Item 3

1. Ordered item 1
2. Ordered item 2
   1. Nested ordered item 1
   2. Nested ordered item 2
      - Nested unordered item 1
      - Nested unordered item 2
3. Ordered item 3
"""

type ``tables`` () =
    static let markdown = Setup.Document """
| Header 1 | Header 2 |
|---|---|
| Cell 1 | Cell 2 |
| Cell 3 | Cell 4 |
"""

    [<Fact>]
    let ``converts to standard markdown tables`` () =
        markdown |> convertsToNewLLM """
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |
| Cell 3   | Cell 4   |
"""

type ``applies_to role`` () =
    static let markdown = Setup.Document """
This is an inline {applies_to}`stack: preview 9.1` element.
"""

    [<Fact>]
    let ``converts to plain text with optional comment`` () =
        markdown |> convertsToNewLLM """
        This is an inline `stack: preview 9.1` element.
        """

type ``admonition directive`` () =
    static let markdown = Setup.Document """
:::{note}
This is a note admonition.
:::
:::{warning}
This is a warning admonition.
:::
:::{tip}
This is a tip admonition.
:::
:::{important}
This is a tip admonition.
:::
:::{admonition} This is my callout
It can *span* multiple lines and supports inline formatting.

Here is a list:
- Item 1
- Item 2
:::
"""

    [<Fact>]
    let ``renders correctly`` () =
        markdown |> convertsToNewLLM """
<note>
  This is a note admonition.
</note>

<warning>
  This is a warning admonition.
</warning>

<tip>
  This is a tip admonition.
</tip>

<important>
  This is a tip admonition.
</important>

<admonition title="This is my callout">
  It can *span* multiple lines and supports inline formatting.Here is a list:
  - Item 1
  - Item 2
</admonition>
"""

type ``image directive`` () =
    static let markdown = Setup.Document """
```{image} /path/to/image.png
:alt: Alt text
:width: 300px
```
"""

    [<Fact>]
    let ``converts to standard markdown image`` () =
        markdown |> convertsToNewLLM """
![Alt text](https://www.elastic.co/path/to/image.png)
"""

type ``include directive`` () =
    static let generator = Setup.Generate [
        Index """
```{include} _snippets/my-include.md
```
"""
        Markdown "_snippets/my-include.md" """
- List item
  - Nested list item
"""
    ]

    [<Fact>]
    let ``handles include directives appropriately`` () =
        generator
        |> convertsToNewLLM """
- List item
  - Nested list item
"""

type ``multiple elements`` () =
    static let markdown = Setup.Document """
This is a paragraph with **bold** and *italic* text.

```python
def hello():
    print("Hello, world!")
```

- List item 1
- List item 2

| Header 1 | Header 2 |
|---|---|
| Cell 1 | Cell 2 |
"""

    [<Fact>]
    let ``converts complex document to clean markdown`` () =
        markdown |> convertsToNewLLM """
This is a paragraph with **bold** and *italic* text.
```python
def hello():
    print("Hello, world!")
```

- List item 1
- List item 2


| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |
"""


type ``directive in list should be indented correctly`` () =
    static let markdown = Setup.Document """
* List item 1
  ```python
  def hello():
      print("Hello, world!")
  ```
* List item 2
  :::{tip}
    - Nested list item 1
    - Nested list item 2
  ::: 
  
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
- List item 1  
  ```python
  def hello():
      print("Hello, world!")
```
- List item 2  
  <tip>

  - Nested list item 1
  - Nested list item 2
  </tip>
"""

type ``tabs`` () =
    static let markdown = Setup.Document """
::::{tab-set}

:::{tab-item} Tab #1 title
This is where the content for tab #1 goes.
:::

:::{tab-item} Tab #2 title
This is where the content for tab #2 goes.
:::

::::
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
<tab-set>

  <tab-item title="Tab #1 title">
    This is where the content for tab #1 goes.
  </tab-item>
 
  <tab-item title="Tab #2 title">
    This is where the content for tab #2 goes.
  </tab-item>
</tab-set>
"""


type ``comments`` () =
    static let markdown = Setup.Document """
This text is visible

% This is a comment

<!--
This is also a comment
-->

This text is also visible
"""

    [<Fact>]
    let ``rendered correctly`` () =
        // Get the actual LLM output string for additional assertions
        let results = markdown.Value
        let defaultFile = results.MarkdownResults |> Seq.find (fun r -> r.File.RelativePath = "index.md")
        let actualLLM = toLlmMarkdown defaultFile
        
        // Test that visible content is present and formatted correctly
        test <@ actualLLM = "This text is visible\nThis text is also visible" @>
        
        // Test that comments are not present in the output
        test <@ not (actualLLM.Contains("This is a comment")) @>
        test <@ not (actualLLM.Contains("This is also a comment")) @>

type ``dropdown`` () =
    static let markdown = Setup.Document """
:::{dropdown} Dropdown title
This is where the content for the dropdown goes.
:::
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
<dropdown title="Dropdown title">
  This is where the content for the dropdown goes.
</dropdown>
"""
        
type ``definition list`` () =
    static let markdown = Setup.Document """
`First Term`
:   This is the definition of the first term.
    - This a list in a definition
    - This a list in a definition

`Second Term`
:    This is one definition of the second term.
     ```javascript
     console.log("Hello, world!");
     ```
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
<definitions>
  <definition term="First Term">
    This is the definition of the first term.
    - This a list in a definition
    - This a list in a definition
  </definition>
  <definition term="Second Term">
    This is one definition of the second term.
    ```javascript
    console.log("Hello, world!");
    ```
  </definition>
</definitions>
"""
type ``image`` () =
    static let markdown = Setup.Document """
![elasticsearch](images/64x64_Color_elasticsearch-logo-color-64px.png "elasticsearch =50%")
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
![elasticsearch](https://www.elastic.co/images/64x64_Color_elasticsearch-logo-color-64px.png "elasticsearch")
"""

type ``kbd role`` () =
    static let markdown = Setup.Document """
{kbd}`cmd+enter`
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
<kbd>Cmd</kbd> + <kbd>Enter</kbd>
"""

type ``codeblock in list`` () =
    static let markdown = Setup.Document """
- List item 1  
  ```python
  def hello():
      print("Hello, world!")
  ```
- List item 2
  1. Nested list item
     ```python
     def hello():
         print("Hello, world!")
     ```
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
- List item 1  
  ```python
  def hello():
      print("Hello, world!")
  ```
- List item 2
  1. Nested list item
     ```python
     def hello():
         print("Hello, world!")
     ```
"""

type ``substitions`` () =
    static let markdown = Setup.Document """---
sub:
  hello-world: "Hello World!"
---

Hello, this is a substitution: {{hello-world}}
This is not a substitution: {{not-found}}
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
Hello, this is a substitution: Hello World!
This is not a substitution: {{not-found}}
"""

type ``substition in codeblock`` () =
    static let markdown = Setup.Document """---
sub:
  hello-world: "Hello World!"
---

```plaintext
Hello, this is a substitution: {{hello-world}}
```

```plaintext subs=true
Hello, this is a substitution: {{hello-world}}
```
"""

    [<Fact>]
    let ``substitution in codeblock is only replaced when subs=true`` () =
        markdown |> convertsToNewLLM """
```plaintext
Hello, this is a substitution: {{hello-world}}
```

```plaintext
Hello, this is a substitution: Hello World!
```
"""
