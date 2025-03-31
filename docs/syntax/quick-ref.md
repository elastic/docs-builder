---
navigation_title: "WIP Quick reference"
---

# WORK IN PROGRESS Syntax quick reference

:::{warning}
This page contains WIP tests, not real entries
:::

Quick guidance on Elastic Docs V3 syntax. 

## Example quick ref entry
Prose description of this syntax element (note: the heading should link to the full syntax guide entry)

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



## Headings
Title of a page or a section. To create a heading, add number signs `#` in front of a word or phrase. The number of signs corresponds to the heading level. 

**DOs**<br>
✅ Start every page with Heading 1<br>
✅ Use only one Heading 1 per page<br>
✅ Use a custom anchor link if you use the same heading text multiple times<br>

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

## Anchor links

Anchor links are generated based on the heading text. You will get a hyphened, lowercase, alphanumeric version of any string, with any diacritics removed, whitespace and dashes collapsed, and whitespace trimmed. 

**DOs**<br>
✅ Use always lower case<br>

**DON'Ts**<br>
❌ Put punctuation marks<br>

### Default anchor links

:::{dropdown} Markdown
```markdown
#### Hello-World
```
:::

:::{dropdown} Output

#### Hello-World

:::

### Custom anchor links

You can also specify a custom anchor link using the following syntax.

:::{dropdown} Markdown
```markdown
#### Heading [custom-anchor]
```
:::

:::{dropdown} Output

#### Heading [custom-anchor]

:::

## Admonitions

Use admonitions to draw attention to content that is different than the main body.
(Include examples: Markdown and Rendered)

**DOs**<br>
✅ Use :open: <bool> to collapse long content that takes too much space<br>

**DON'Ts**<br>
❌ Overload the page with too many admonitions<br>

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

:::{dropdown} Plain
```markdown
:::{admonition}
When none of the above apply.
```
:::{admonition}
When none of the above apply.
:::


## Applies to

Allows you to annotate a page or a section based on its applicability to a specific product or version.

**DOs**<br>
✅ To annotate a page, put `{applies_to}` in the YAML frontmatter<br>
✅ To annotate a section, put `{applies_to}` immediately before the heading<br>

**DON'Ts**<br>
❌ Use in admonitions yet.<br> 

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
  product: coming 9.5, discontinued 9.7
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




🚧🚧🚧