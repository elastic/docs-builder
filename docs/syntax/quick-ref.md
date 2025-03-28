---
navigation_title: "Quick reference"
---

# Syntax quick reference

Quick guidance on Elastic Docs V3 syntax. 


## Headings
Title of a page or a section. To create a heading, add number signs `#` in front of a word or phrase. The number of signs corresponds to the heading level. 

**DOs**
- ✅ Start every page with Heading 1
- ✅ Use only one Heading 1 per page
- ✅ Use a custom anchor link if you use the same heading text multiple times

**DON'Ts**
- ❌ Use headings in tabs or dropdowns
- ❌ Go deeper than Heading 4

::::{tab-set}

:::{tab-item} Output

# Heading 1

## Heading 2

### Heading 3

#### Heading 4
:::

:::{tab-item} Markdown

```markdown
# Heading 1
## Heading 2
### Heading 3
#### Heading 4
```
:::
::::


## Anchor links

Anchor links are generated based on the heading text. You will get a hyphened, lowercase, alphanumeric version of any string, with any diacritics removed, whitespace and dashes collapsed, and whitespace trimmed. 

**DOs**
- ✅ Use always lower case

**DON'Ts**
- ❌ Put punctuation marks

### Default anchor links

::::{tab-set}

:::{tab-item} Output

#### Hello-World

:::

:::{tab-item} Markdown

```markdown

#### Hello-World

```

:::

::::


### Custom anchor links

You can also specify a custom anchor link using the following syntax.

::::{tab-set}

:::{tab-item} Output

#### Heading [custom-anchor]

:::

:::{tab-item} Markdown

```markdown

#### Heading [custom-anchor]

```

:::

::::






🚧🚧🚧