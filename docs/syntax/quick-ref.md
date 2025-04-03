---
navigation_title: "WIP Quick reference"
---

# WORK IN PROGRESS Syntax quick reference

:::{warning}
This page is still in progress. 
:::

Quick guidance on Elastic Docs V3 syntax.


## Admonitions

Use admonitions to caution users, or to provide helpful tips or extra information.

**DOs**<br>
✅ Use custom admonitions as needed

**DON'Ts**<br>
❌ Stack admonitions<br>
❌ Overload a page with too many admonitions<br>

### Types

:::{dropdown} Warning
```markdown
:::{warning}
Users could permanently lose data or leak sensitive information.
```
:::{warning}
Users could permanently lose data or leak sensitive information.
:::

:::{dropdown} Important
```markdown
:::{important}
Less dire than a warning. Users might encounter issues with performance or stability.
```
:::{important}
Less dire than a warning. Users might encounter issues with performance or stability.
:::

:::{dropdown} Note
```markdown
:::{note}
Supplemental information that provides context or clarification.
```
:::{note}
Supplemental information that provides context or clarification.
:::

:::{dropdown} Tip
```markdown
:::{tip}
Advice that helps users work more efficiently or make better choices.
```
:::{tip}
Advice that helps users work more efficiently or make better choices.
:::

:::{dropdown} Custom
```markdown
:::{admonition} Special note
Custom admonition with custom label.
```
:::{admonition} Special note
Custom admonition with custom label.
:::


## Anchors

A default anchor is automatically created for each [heading](#headings), in the form `#heading-text` (hyphenated, lowercase, special characters and spaces trimmed). To create a custom anchor, add it in square brackets at the end of a heading: `[my-better-anchor]` 

**DOs**<br>
✅ Create custom anchors for repeated structural headings like "Example request"<br>

**DON'Ts**<br>
❌ Include punctuation marks in custom anchors<br>
❌ Define custom anchors in text that is not a heading

:::{dropdown} Default anchor
:open:
```markdown
#### Hello world!
<!-- Auto-generated default anchor: #hello-world -->
```
:::


:::{dropdown} Custom anchor
```markdown
#### Hello world! [get-started]
```
:::


[More details: Links →](links.md#same-page-links-anchors)


## Comments

Use `%` to add single-line comments.

**DOs**<br>
✅ Add a space after the `%`<br>

**DON'Ts**<br>
❌ Use `#` or `//`<br>

:::{dropdown} Markdown
:open:
```markdown
% This is a comment
This is regular text
:::

:::{dropdown} Output
% This is a comment
This is regular text
:::

## Dropdowns

Collapsible blocks for hiding and showing content. 

**DOs**<br>
✅ Use dropdowns for text, lists, images, code blocks, and tables<br>
✅ Add `:open:` to auto-expand a dropdown by default

**DON'Ts**<br>
❌ Use dropdowns for very long paragraphs or entire sections<br>


**Output**
:::{dropdown} Title or label
:open:
Collapsible content
:::

**Markdown**

```markdown
    :::{dropdown} Title or label
    :open:
    Collapsible content
    :::
```

[More details: Dropdowns →](dropdowns.md)

## Headings
Title of a page or a section. To create a heading, add number signs `#` at the beginning of the line (one `#` for each heading level). 

**DOs**<br>
✅ Start every page with a Heading 1<br>
✅ Use only one Heading 1 per page<br>
✅ Define custom anchors for repeated headings<br>

**DON'Ts**<br>
❌ Use headings in tabs or dropdowns<br>
❌ Go deeper than Heading 4

:::{dropdown} Output
:open:
# Heading 1
## Heading 2
### Heading 3
#### Heading 4

:::

:::{dropdown} Markdown
```markdown
# Heading 1
## Heading 2
### Heading 3
#### Heading 4
```
:::



[More details: Headings →](headings.md)

## Substitutions (subs)
Key-value pairs that define variables. They help ensure consistency and enable short forms. To use a sub, surround the key with curly brackets: `{{variable}}`<br>

**DOs** <br>
✅ Check the global `docset.yml` file for existing product and feature name subs<br>
✅ Use substitutions in code blocks by setting `subs=true`  <br>
✅ Define new page-specific substitutions as needed  

**DON'Ts**<br>
❌ Override a `docset.yml` sub by defining a page-level sub with the same key (causes build errors)<br>
❌ Use substitutions for common words that don't need to be standardized  

### Define a substitution

:::{dropdown} Yaml
In `docset.yml`:

```
subs:
  ccs: "cross-cluster search"
  ech: "Elastic Cloud Hosted"
```
:::


### Use a substitution

This example uses the `docset.yml` defined [above](#define-a-substitution).

:::{dropdown} Markdown

In `myfile.md`:

```
{{ech}} supports most standard Kibana settings.
```
:::

:::{dropdown} Output

Elastic Cloud Hosted supports most standard Kibana settings.
:::

[More details: Substitutions →](./substitutions.md)

% TODO: link to our global docset.yml?

## Applies to

:::{admonition} WIP
🚧 more coming soon 🚧
:::

Tags that identify the deployments and flavors (stack/serverless) that a piece of content "applies to." Think of `applies_to` tags as technical context identifiers that help users determine whether content is right for their deployments and configuration.

:::{admonition} Tip
General content that is not deployment-specific should _not_ have any `applies_to` tags. They're meant to be limiting, not exhaustive / tk something
:::

**DOs**<br>
✅ Define a set of page-level `applies_to` tags in a front matter block<br>
✅ Add `{applies_to}` after a heading to indicate that section's contexts<br>
✅ Indicate versions (`major.minor` with an optional `[.patch]`)  and release phases like `beta`

**DON'Ts**<br>
❌ Add `applies_to` tags to general, broadly applicable content<br>
❌ Use `applies_to` tags as metadata or to represent "aboutness" -- focus on helping users make decisions 
❌ Include `applies_to` tags in admonitions<br>
❌ Use `Coming (x.x.x)` tags, except in special cases (don't pre-announce features)<br>

### Page-level tags

:::{dropdown} Output
:open:
🚧 **TODO replace this image to match markdown** 🚧

![annotations rendered](img/annotations.png)
:::

:::{dropdown} Markdown

This example includes version and release phase facets, which aren't always needed. In many cases, `stack:` and `serverless:` are enough.

```yaml
---
applies_to:
  stack: 9.0
  deployment:
    ece: preview
    eck: beta 9.0.1
    ess: 
    self: 9.0 
  serverless:
    elasticsearch:  
    observability: deprecated 
    security: 
  product: 
---
```
:::

### Section tag

:::{dropdown} Output
:open:
#### Stack-only content
```{applies_to}
stack:
```
:::

:::{dropdown} Markdown
````markdown
# Stack-only content
```{applies_to}
stack: 
```
````
:::

[More details: Applies to →](applies.md)

## Code blocks

Multi-line blocks for code, commands, configuration, and similar content. Use three backticks ` ``` ` on separate lines to start and end the block. For syntax highlighting, add a language identifier after the opening backticks.

**DOs**<br>
✅ Include code blocks within lists or other block elements as needed<br>
✅ Add language identifiers like `yaml`, `json`, `bash`

**DON'Ts**<br>
❌ Place code blocks in admonitions<br>
❌ Use inline code formatting (single backticks) for multi-line content<br>

:::{dropdown} Example
:open:
```yaml
server.host: "0.0.0.0"
elasticsearch.hosts: ["http://localhost:9200"]
```
:::

:::{dropdown} Markdown
```markdown
    ```yaml
    server.host: "0.0.0.0"
    elasticsearch.hosts: ["http://localhost:9200"]
    ```
```
:::


## Code callouts

Inline annotations that highlight or explain specific lines in a code block. 

**DOs**<br>
✅ Keep callout/comment text short and specific<br>
✅ Use only one type of callout per code block (don't mix [explicit](#explicit-callout) and [magic](#magic-callout))<br>

**DON'Ts**<br>
❌ Overuse callouts -- aim for scannability<br>

### Explicit callout
Add `<1>`, `<2>`, ... to the end of a line to explicitly create a code callout.

:::{dropdown} Example: Explicit callout
:open:
```json
{
  "match": {
    "message": "search text" <1>
  }
}
```
1. Searches the `message` field for the phrase "search text"

**Markdown**
```
    ```json
    {
      "match": {
        "message": "search text" <1>
      }
    }
    ```
    1. Searches the `message` field for the phrase "search text"
```
1. 🚧 there's a bug in the rendering of this
:::


### Magic callout
Add comments with `//` or `#` to magically create callouts.

:::{dropdown} Example: Magic comment-based callout
```json
{
  "match": {
    "message": "search text" // Searches the message field
  }
}
```

**Markdown**

(TODO replace with image format to prevent rendering? create issue)

```markdown
    ```json
    {
      "match": {
        "message": "search text" // Searches the message field
      }
    }
    ```
```
:::



🚧🚧🚧

## Quick ref entry template
Prose description of this syntax element 

**DOs**<br>
✅ First _do_ -- not in a bulleted list; use the checkmark as a bullet character and use line breaks<br>
✅ Second _do_

**DON'Ts**<br>
❌ First _don't_<br>
❌ Second _don't_

Dropdowns: In most cases, use dropdowns labeled Output (open by default) and Markdown. But use different labels and a progressive sequence (etc.) as needed

:::{dropdown} Output
(open by default)

:open:
some output, **strong**
:::

:::{dropdown} Markdown
```markdown
some markdown, **strong**
```
:::

[More details →](index.md)
