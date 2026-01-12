// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module ``AuthoringTests``.``llm markdown``.``output tests``

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
    let ``converts to human readable format`` () =
        markdown |> convertsToNewLLM """This is an inline <applies-to>Elastic Stack: Planned</applies-to> element.
"""

type ``applies_to role with ga future version`` () =
    static let markdown = Setup.Document """
This is an inline {applies_to}`stack: ga 9.0` element.
"""

    [<Fact>]
    let ``shows planned text for unreleased version`` () =
        markdown |> convertsToNewLLM """This is an inline <applies-to>Elastic Stack: Planned</applies-to> element.
"""

type ``applies_to role with ga released version`` () =
    static let markdown = Setup.Document """
This is an inline {applies_to}`stack: ga 7.3` element.
"""

    [<Fact>]
    let ``shows ga text for released version`` () =
        markdown |> convertsToNewLLM """This is an inline <applies-to>Elastic Stack: Generally available since 7.3</applies-to> element.
"""

type ``applies_to role in sentence with serverless`` () =
    static let markdown = Setup.Document """
This feature is available on {applies_to}`serverless: ga` for all users.
"""

    [<Fact>]
    let ``shows serverless availability in sentence`` () =
        markdown |> convertsToNewLLM """This feature is available on <applies-to>Elastic Cloud Serverless: Generally available</applies-to> for all users.
"""

type ``applies_to role in sentence with preview`` () =
    static let markdown = Setup.Document """
The new API {applies_to}`stack: preview 7.5` provides enhanced functionality.
"""

    [<Fact>]
    let ``shows preview availability in sentence`` () =
        markdown |> convertsToNewLLM """The new API <applies-to>Elastic Stack: Preview since 7.5</applies-to> provides enhanced functionality.
"""

type ``applies_to role in sentence with deprecated`` () =
    static let markdown = Setup.Document """
This method {applies_to}`stack: deprecated 7.0` should not be used in new code.
"""

    [<Fact>]
    let ``shows deprecated availability in sentence`` () =
        markdown |> convertsToNewLLM """This method <applies-to>Elastic Stack: Deprecated since 7.0</applies-to> should not be used in new code.
"""

type ``applies_to inline role formats`` () =
    // Test cases: (input applies_to syntax, expected output text)
    static let testCases = [
        // GA with released version
        ("stack: ga 7.3", "Elastic Stack: Generally available since 7.3")
        ("stack: ga 8.0", "Elastic Stack: Generally available since 8.0")
        ("stack: ga 8.0+", "Elastic Stack: Generally available since 8.0")
        
        // GA with future version (shows Planned)
        ("stack: ga 9.0", "Elastic Stack: Planned")
        
        // Preview with released version
        ("stack: preview 7.5", "Elastic Stack: Preview since 7.5")
        
        // Preview with exact version
        ("stack: preview =7.0", "Elastic Stack: Preview in 7.0")
        
        // GA with range (both ends released)
        ("stack: ga 7.0-8.0", "Elastic Stack: Generally available from 7.0 to 8.0")
        
        // Beta with range
        ("stack: beta 7.0-7.5", "Elastic Stack: Beta from 7.0 to 7.5")
        
        // Preview with future version (shows Planned)
        ("stack: preview 9.1", "Elastic Stack: Planned")
        
        // Beta with released version
        ("stack: beta 7.0", "Elastic Stack: Beta since 7.0")
        
        // Deprecated with released version
        ("stack: deprecated 7.0", "Elastic Stack: Deprecated since 7.0")
        
        // Serverless GA (unversioned product - no version shown)
        ("serverless: ga", "Elastic Cloud Serverless: Generally available")
        
        // Elasticsearch preview (no version - uses base version)
        ("elasticsearch: preview", "Serverless Elasticsearch projects: Preview in 8.0+")
    ]
    
    [<Fact>]
    let ``renders all lifecycle types correctly`` () =
        for (input, expected) in testCases do
            let markdown = Setup.Document $"Test {{applies_to}}`{input}` here."
            let expectedOutput = $"Test <applies-to>{expected}</applies-to> here.\n"
            markdown |> convertsToNewLLM expectedOutput

type ``frontmatter applies_to in metadata`` () =
    static let generator = Setup.Generate [
        Index """---
applies_to:
  stack: ga 7.0
---
# Test Page

This is test content.
"""
    ]

    [<Fact>]
    let ``includes applies_to in LLM metadata output`` () =
        generator |> convertsToLlmWithMetadata """---
title: Test Page
applies_to:
  - Elastic Stack: Generally available since 7.0
---

# Test Page
This is test content.
"""

type ``applies_to code block directive`` () =
    static let markdown = Setup.Document """
```{applies_to}
stack: ga 7.0
serverless: ga
```
"""

    [<Fact>]
    let ``renders applies_to block with human-readable text`` () =
        markdown |> convertsToNewLLM """<applies-to>
  - Elastic Cloud Serverless: Generally available
  - Elastic Stack: Generally available since 7.0
</applies-to>
"""

type ``applies-switch directive`` () =
    static let markdown = Setup.Document """
::::{applies-switch}
:::{applies-item} stack: ga 7.0
Content for Elastic Stack users.
:::
:::{applies-item} serverless: ga
Content for Serverless users.
:::
::::
"""

    [<Fact>]
    let ``renders applies-switch with human-readable applies-to`` () =
        markdown |> convertsToNewLLM """
<applies-switch>
  <applies-item title="stack: ga 7.0" applies-to="Elastic Stack: Generally available since 7.0">
    Content for Elastic Stack users.
  </applies-item>

  <applies-item title="serverless: ga" applies-to="Elastic Cloud Serverless: Generally available">
    Content for Serverless users.
  </applies-item>
</applies-switch>
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

type ``admonition directive with applies_to`` () =
    static let markdown = Setup.Document """
:::{note}
:applies_to: stack: ga
This is a note admonition with applies_to information.
:::
:::{warning}
:applies_to: serverless: ga
This is a warning admonition with applies_to information.
:::
:::{tip}
:applies_to: elasticsearch: preview
This is a tip admonition with applies_to information.
:::
:::{important}
:applies_to: { stack: ga, serverless: ga }
This is an important admonition with applies_to information.
:::
:::{admonition} Custom Admonition
:applies_to: { stack: ga, serverless: ga, elasticsearch: preview }
This is a custom admonition with applies_to information.
:::
"""

    [<Fact>]
    let ``renders correctly with applies_to information`` () =
        markdown |> convertsToNewLLM """
<note applies-to="Elastic Stack: Generally available in 8.0+">
  This is a note admonition with applies_to information.
</note>

<warning applies-to="Elastic Cloud Serverless: Generally available">
  This is a warning admonition with applies_to information.
</warning>

<tip applies-to="Serverless Elasticsearch projects: Preview in 8.0+">
  This is a tip admonition with applies_to information.
</tip>

<important applies-to="Elastic Cloud Serverless: Generally available, Elastic Stack: Generally available in 8.0+">
  This is an important admonition with applies_to information.
</important>

<admonition title="Custom Admonition" applies-to="Serverless Elasticsearch projects: Preview in 8.0+, Elastic Stack: Generally available in 8.0+">
  This is a custom admonition with applies_to information.
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
        
type ``dropdown with applies_to`` () =
    static let markdown = Setup.Document """
:::{dropdown} Dropdown title
:applies_to: stack: ga 8.0
This is where the content for the dropdown goes.
:::
"""

    [<Fact>]
    let ``rendered correctly`` () =
        markdown |> convertsToNewLLM """
<dropdown title="Dropdown title" applies-to="Elastic Stack: Generally available since 8.0">
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

type ``substitutions`` () =
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

type ``substitution in codeblock`` () =
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

type ``diagram directive`` () =
    static let markdown = Setup.Document """
::::{diagram} mermaid
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
::::

::::{diagram} d2
x -> y: hello world
y -> z: nice to meet you
::::
"""

    [<Fact>]
    let ``renders diagram with type information`` () =
        markdown |> convertsToNewLLM """
<diagram type="mermaid">
  flowchart LR
      A[Start] --> B{Decision}
      B -->|Yes| C[Action 1]
      B -->|No| D[Action 2]
      C --> E[End]
      D --> E
</diagram>

<diagram type="d2">
  x -> y: hello world
  y -> z: nice to meet you
</diagram>
"""
        
type ``substitution in heading`` () =
    static let markdown = Setup.Document """---
sub:
  world: "World"
---

## Hello, {{world}}!
"""

    [<Fact>]
    let ``renders correctly`` () =
        markdown |> convertsToNewLLM """
## Hello, World!
"""

type ``settings directive`` () =
    static let generator = Setup.Generate [
        Index """
:::{settings} _settings/example-settings.yml
:::
"""
        File("_settings/example-settings.yml", """groups:
  - group: General settings
    settings:
      - setting: xpack.example.setting
        description: |
          This is a test setting with **bold** text and a [link](https://example.com).
      - setting: xpack.another.setting
        description: Another setting description.
  - group: Advanced settings
    settings:
      - setting: xpack.advanced.option
        description: An advanced option.
""")
    ]

    [<Fact>]
    let ``renders settings as markdown headings`` () =
        generator |> convertsToNewLLM """
## General settings

#### xpack.example.setting

This is a test setting with **bold** text and a [link](https://example.com).

#### xpack.another.setting

Another setting description.

## Advanced settings

#### xpack.advanced.option

An advanced option.
"""

type ``links in paragraphs`` () =
    static let markdown = Setup.Document """
This is a paragraph with a [link to docs](https://www.elastic.co/docs/deploy-manage/security) in it.
"""

    [<Fact>]
    let ``renders links without duplication`` () =
        markdown |> convertsToNewLLM """This is a paragraph with a [link to docs](https://www.elastic.co/docs/deploy-manage/security) in it.
"""

type ``links in tables`` () =
    static let markdown = Setup.Document """
| Feature | Availability |
|---------|--------------|
| [Security configurations](https://www.elastic.co/docs/deploy-manage/security) | Full control |
| [Authentication realms](https://www.elastic.co/docs/deploy-manage/users-roles) | Available |
"""

    [<Fact>]
    let ``renders links in table cells without duplication`` () =
        markdown |> convertsToNewLLM """
| Feature                                                                        | Availability |
|--------------------------------------------------------------------------------|--------------|
| [Security configurations](https://www.elastic.co/docs/deploy-manage/security)  | Full control |
| [Authentication realms](https://www.elastic.co/docs/deploy-manage/users-roles) | Available    |
"""

type ``multiple links in table cells`` () =
    static let markdown = Setup.Document """
| Feature | Links |
|---------|-------|
| Security | [Config](https://example.com/config) and [Auth](https://example.com/auth) |
"""

    [<Fact>]
    let ``renders multiple links in same cell without duplication`` () =
        markdown |> convertsToNewLLM """
| Feature  | Links                                                                     |
|----------|---------------------------------------------------------------------------|
| Security | [Config](https://example.com/config) and [Auth](https://example.com/auth) |
"""

type ``links with formatting in tables`` () =
    static let markdown = Setup.Document """
| Feature | Description |
|---------|-------------|
| [**Bold link**](https://example.com) | Description |
| [*Italic link*](https://example.com/italic) | Another |
"""

    [<Fact>]
    let ``renders formatted links in table cells correctly`` () =
        markdown |> convertsToNewLLM """
| Feature                                     | Description |
|---------------------------------------------|-------------|
| [**Bold link**](https://example.com)        | Description |
| [*Italic link*](https://example.com/italic) | Another     |
"""

type ``bold and italic in tables`` () =
    static let markdown = Setup.Document """
| Format | Example |
|--------|---------|
| Bold | This is **bold text** here |
| Italic | This is *italic text* here |
| Both | This is **bold** and *italic* |
"""

    [<Fact>]
    let ``renders bold and italic in table cells without duplication`` () =
        markdown |> convertsToNewLLM """
| Format | Example                       |
|--------|-------------------------------|
| Bold   | This is **bold text** here    |
| Italic | This is *italic text* here    |
| Both   | This is **bold** and *italic* |
"""

type ``code inline in tables`` () =
    static let markdown = Setup.Document """
| Command | Description |
|---------|-------------|
| `git status` | Shows status |
| `git commit` | Commits changes |
"""

    [<Fact>]
    let ``renders code inline in table cells correctly`` () =
        markdown |> convertsToNewLLM """
| Command      | Description     |
|--------------|-----------------|
| `git status` | Shows status    |
| `git commit` | Commits changes |
"""

type ``images in tables`` () =
    static let markdown = Setup.Document """
| Icon | Name |
|------|------|
| ![logo](https://example.com/logo.png) | Logo |
"""

    [<Fact>]
    let ``renders images in table cells without duplication`` () =
        markdown |> convertsToNewLLM """
| Icon                                  | Name |
|---------------------------------------|------|
| ![logo](https://example.com/logo.png) | Logo |
"""
