# Page cards

Page cards are full-width, clickable navigation rows that link to another page in the docs. Use them to build index-style landing pages where you want each destination to be visually prominent and immediately navigable.

:::{page-card} [Admonitions](admonitions.md)
Callout boxes for notes, warnings, tips, and other asides.
:::

## Basic usage

A page card takes a single Markdown link as its argument. The description body is optional.

:::::::{tab-set}
::::::{tab-item} Output
:::{page-card} [Code blocks](code.md)
Syntax-highlighted code with copy button, callouts, and console output support.
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{page-card} [Code blocks](code.md)
Syntax-highlighted code with copy button, callouts, and console output support.
:::
```
::::::
:::::::

## Without a description

:::::::{tab-set}
::::::{tab-item} Output
:::{page-card} [Tables](tables.md)
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{page-card} [Tables](tables.md)
:::
```
::::::
:::::::

## Stacking cards

Consecutive page cards stack into a list automatically — no container directive required.

:::::::{tab-set}
::::::{tab-item} Output
:::{page-card} [Admonitions](admonitions.md)
Callout boxes for notes, warnings, tips, and important asides.
:::

:::{page-card} [Tabs](tabs.md)
Organise related content into selectable tab panels.
:::

:::{page-card} [Stepper](stepper.md)
Step-by-step instructions with numbered visual progression.
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{page-card} [Admonitions](admonitions.md)
Callout boxes for notes, warnings, tips, and important asides.
:::

:::{page-card} [Tabs](tabs.md)
Organise related content into selectable tab panels.
:::

:::{page-card} [Stepper](stepper.md)
Step-by-step instructions with numbered visual progression.
:::
```
::::::
:::::::

## Link types

### Local links

The most common use — a relative path to another `.md` file in the same documentation set:

```markdown
:::{page-card} [Configuration](./configuration.md)
How to configure contexts and credentials.
:::
```

### Cross-repository links

Page cards support [cross-repository links](links.md#cross-repository-links) using the `scheme://path` syntax:

```markdown
:::{page-card} [Getting Started](docs-content://get-started/introduction.md)
Learn the basics of the Elastic Stack.
:::
```

### Absolute URLs are not allowed

Page cards are for in-docs navigation only. Absolute `http://` or `https://` URLs are rejected at build time:

```markdown
:::{page-card} [Elastic website](https://elastic.co)  ← build error
:::
```

Use a standard Markdown link or a [button](buttons.md) for external destinations.

## Reference

| Part | Required | Description |
|------|----------|-------------|
| Argument | Yes | A Markdown link `[Title](url)` — title becomes the card heading, url must be a local `.md` path or a crosslink. |
| Body | No | One or more lines of description text rendered below the title. |
