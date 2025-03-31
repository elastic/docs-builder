---
navigation_title: "WIP Quick reference"
---

# WORK IN PROGRESS Syntax quick reference

:::{warning}
This page contains WIP tests, not real entries
:::

Quick guidance on Elastic Docs V3 syntax.

## Example quick ref entry
Prose description of this syntax element (include a link to the full syntax guide)

**DOs**<br>
✅ First _do_ -- not in a bulleted list; use the checkmark as a bullet character and use line breaks<br>
✅ Second _do_

**DON'Ts**<br>
❌ First _don't_<br>
❌ Second _don't_

:::{dropdown} Markdown
```markdown
some markdown, **strong**
```
:::

:::{dropdown} Output
some output, **strong**
:::

[Link to the full syntax ref entry for more details](index.md)
<br>
<br>
<br>

:::{tip}
 👇 Final drafts 👇 
:::

## Anchors

An anchor is automatically created for each [heading](#headings). Default anchors take the form of hyphenated, lowercase heading text, with spaces and special characters trimmed. You can also create a custom anchor by adding it in square brackets at the end of a heading. 

**DOs**<br>
✅ Create custom anchors for repeated headings like "Example request"<br>

**DON'Ts**<br>
❌ Include punctuation marks in custom anchors<br>
❌ Define custom anchors in text that is not a heading

### Default anchors

:::{dropdown} Markdown
```markdown
#### Hello world!
```
:::

:::{dropdown} Output

Auto-generated anchor `#hello-world`
:::

### Custom anchors

:::{dropdown} Markdown
```markdown
#### Heading [custom-anchor]
```
:::

:::{dropdown} Output

`#custom-anchor` that targets the H4 `Heading`
:::

[More syntax: Anchor links](links#same-page-links-anchors)

## Headings
Title of a page or a section. To create a heading, add number signs `#` at the beginning of the line (one `#` for each heading level). 

**DOs**<br>
✅ Start every page with a Heading 1<br>
✅ Use only one Heading 1 per page<br>
✅ Define [custom anchors](#custom-anchors) for repeated headings<br>

**DON'Ts**<br>
❌ Use headings in tabs or dropdowns<br>
❌ Go deeper than Heading 4

:::{dropdown} Markdown
```markdown
# Heading 1
## Heading 2
### Heading 3
#### Heading 4
```
:::

:::{dropdown} Output

# Heading 1
## Heading 2
### Heading 3
#### Heading 4

:::

[More syntax: Headings](headings.md)

## Substitutions

Key-value pairs that define variables. They help ensure consistency and enable short forms.  
You can define a substitution at the page level in a [front matter](frontmatter.md) block, or in `docset.yml` to apply it across the entire doc set. To use a substitution in your content, surround the key with curly brackets: `{{variable}}`<br>


**DOs** <br>
✅ Check the global `docset.yml` file for existing product and feature name substitutions<br>
✅ Use substitutions in code blocks by setting `subs=true`  <br>
✅ Define new page-specific substitutions as needed  

**DON'Ts**<br>
❌ Override existing `docset.yml` substitutions at the page level (causes build errors)<br>
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
:::{dropdown} Markdown

In `myfile.md`:

```
{{ech}} supports most standard Kibana settings.
```
:::

:::{dropdown} Output

Elastic Cloud Hosted supports most standard Kibana settings.
:::

[More syntax: Substitutions](./substitutions.md)

<br>
<br>
<br>
<br>
<br>
<br>
:::{tip} 
👆 Final drafts 👆  
:::

<br>
<br>
<br>
<br>
<br>
<br>

:::{warning} WIP
🚧🚧🚧 👇 not-final drafts 👇 🚧🚧🚧
:::

## Admonitions

Use admonitions to draw attention to content or to distinguish it from the main flow.

**DOs**<br>
✅ Use custom admonitions as needed

**DON'Ts**<br>
❌ Stack admonitions<br>
❌ Overload a page with too many admonitions<br>

:::{dropdown} Note
```markdown
:::{note}
Is relevant but can be ignored.
It can span multiple lines and supports inline formatting.
```
:::{note}
Is relevant but can be ignored.
It can span multiple lines and supports inline formatting.
:::

:::{dropdown} Warning
```markdown
:::{warning}
Warn the user against decisions they might regret.
```
:::{warning}
Warn the user against decisions they might regret.
:::

:::{dropdown} Tip
```markdown
:::{tip}
Helps the user make better choices.
```
:::{tip}
Helps the user make better choices.
:::

:::{dropdown} Important
```markdown
:::{important}
Could impact system performance or stability.
```
:::{important}
Could impact system performance or stability.
:::

:::{dropdown} Custom
```markdown
:::{admonition} Special note
Custom admonition with custom label.
```
:::{admonition} Special note
Custom admonition with custom label.
:::


## Applies to

Allows you to annotate a page or a section based on its applicability to a specific product or version.

**DOs**<br>
✅ To annotate a page, put `{applies_to}` in the YAML frontmatter<br>
✅ To annotate a section, put `{applies_to}` immediately before the heading<br>

**DON'Ts**<br>
❌ Use "coming in x.x" `applies_to` tags (don't pre-announce features)<br>
❌ Include `applies_to` tags in admonitions<br> 

:::{dropdown} Page annotations
```markdown
---
applies_to:
  stack: ga 9.1
  deployment:
    eck: ga 9.0
    ess: beta 9.1
    ece: discontinued 9.2.0
    self: unavailable 9.3.0
  serverless:
    security: ga 9.0.0
    elasticsearch: beta 9.1.0
    observability: discontinued 9.2.0
  product: discontinued 9.7
---
```
![annotations rendered](img/annotations.png)
:::

:::{dropdown} Section annotations
```markdown
#### Stack only
```yaml {applies_to}
stack: ga 9.1
```

#### Stack only
```yaml {applies_to}
stack: ga 9.1
```
:::

## Code block

Block element that displays multiple lines of code. Start and end a code block with a sequence of three backtick characters ```.

**DOs**<br>
✅ Add a language identifier to enable syntax highlighting<br>

**DON'Ts**<br>
❌ Use in admonitions yet<br> 

:::{dropdown} Markdown
```markdown
```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
```
```
:::

:::{dropdown} Output
```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
```
:::


## Code callouts

A code block can contain **explicit** and **magic** callouts. 

**DOs**<br>
✅ Use one callout format<br>
✅ Put comments right after the code block to be positioned as a callout<br>
✅ In case of an ordered list, follow the same number of items as in the code block<br>

**DON'Ts**<br>
❌ Combine explicit and magic callout<br> 

### Example of explicit callout
Add `<\d+>` to the end of a line to explicitly create a code callout.

:::{dropdown} Markdown
```markdown
```yaml
project:
  license:
    content: CC-BY-4.0 <1>
```

1. The license
```
:::

:::{dropdown} Output
```yaml
project:
  license:
    content: CC-BY-4.0 <1>
```

1. The license

:::

### Example of magic callout
Add comments with `//` or `#` to magically create callouts.

:::{dropdown} Markdown
```markdown
```csharp
var apiKey = new ApiKey("<API_KEY>"); // Set up the api key
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```
```
:::

:::{dropdown} Output
```csharp
var apiKey = new ApiKey("<API_KEY>"); // Set up the api key
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey);
```content: CC-BY-4.0
```

:::

## Comments

Use `%` to add single-line comments.

**DOs**<br>
✅ Add a space after the `%`<br>

**DON'Ts**<br>
❌ Use `#` or `//`<br>

:::{dropdown} Markdown
```markdown
% This is a comment
This is regular text
:::

:::{dropdown} Output
% This is a comment
This is regular text
:::

## Dropdowns

Dropdowns hide and reveal content on user interaction. By default, dropdowns are collapsed and content is hidden until you click the title of the collapsible block.

**DOs**<br>
✅ Use dropdowns for text, lists, images, code blocks, and tables<br>

**DON'Ts**<br>
❌ Use for very long paragraphs or entire sections<br>

:::{dropdown} Markdown
```markdown
:::{dropdown} Dropdown Title
Dropdown content
:::
:::

:::{dropdown} Output
:::{dropdown} Dropdown Title
Dropdown content
:::
:::

You can optionally specify the `open` option to keep the dropdown content visible by default.

```markdown
:::{dropdown} Dropdown Title
:open:
Dropdown content
:::
```







🚧🚧🚧