# Buttons

Buttons provide styled link elements for calls to action in documentation. Use buttons to highlight important navigation points, downloads, or external resources.

:::{button}
[Getting Started](docs-content://get-started/introduction.md)
:::

## Basic button

A button wraps a standard Markdown link with button styling:

:::::::{tab-set}
::::::{tab-item} Output
:::{button}
[Syntax Guide](index.md)
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button}
[Get Started](/get-started)
:::
```
::::::
:::::::

## Button types

Two button variants are available:

- **Primary** (default): Filled blue background with white text, used for main calls to action.
- **Secondary**: Blue border with transparent background, used for secondary actions.

:::::::{tab-set}
::::::{tab-item} Output
::::{button-group}
:::{button}
[Quick Reference](quick-ref.md)
:::
:::{button}
:type: secondary
[Syntax Guide](index.md)
:::
::::
::::::

::::::{tab-item} Markdown
```markdown
::::{button-group}
:::{button}
[Primary Action](/primary)
:::
:::{button}
:type: secondary
[Secondary Action](/secondary)
:::
::::
```
::::::
:::::::

## Button groups

Use the `{button-group}` directive to display multiple buttons in a row:

:::::::{tab-set}
::::::{tab-item} Output
::::{button-group}
:::{button}
[Admonitions](admonitions.md)
:::
:::{button}
:type: secondary
[Dropdowns](dropdowns.md)
:::
::::
::::::

::::::{tab-item} Markdown
```markdown
::::{button-group}
:::{button}
[Elastic Fundamentals](/get-started)
:::
:::{button}
:type: secondary
[Upgrade Versions](/deploy-manage/upgrade)
:::
::::
```
::::::
:::::::

## Alignment

### Single button alignment

Control the horizontal alignment of standalone buttons with the `:align:` property:

:::::::{tab-set}
::::::{tab-item} Output
:::{button}
:align: left
[Links](links.md)
:::

:::{button}
:align: center
[Images](images.md)
:::

:::{button}
:align: right
[Tables](tables.md)
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button}
:align: left
[Left (default)](/example)
:::

:::{button}
:align: center
[Center](/example)
:::

:::{button}
:align: right
[Right](/example)
:::
```
::::::
:::::::

### Button group alignment

Button groups also support the `:align:` property:

:::::::{tab-set}
::::::{tab-item} Output
::::{button-group}
:align: center
:::{button}
[Code Blocks](code.md)
:::
:::{button}
:type: secondary
[Tabs](tabs.md)
:::
::::
::::::

::::::{tab-item} Markdown
```markdown
::::{button-group}
:align: center
:::{button}
[Centered Group](/example)
:::
:::{button}
:type: secondary
[Second Button](/example)
:::
::::
```
::::::
:::::::

## External links

External links (URLs outside elastic.co) automatically open in a new tab, just like regular links:

:::::::{tab-set}
::::::{tab-item} Output
:::{button}
[Visit GitHub](https://github.com/elastic)
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button}
[Visit GitHub](https://github.com/elastic)
:::
```
::::::
:::::::

External links include `target="_blank"` and `rel="noopener noreferrer"` attributes for security.

## Cross-repository links

Buttons support [cross-repository links](links.md#cross-repository-links) using the `scheme://path` syntax:

:::::::{tab-set}
::::::{tab-item} Output
:::{button}
[Getting Started Guide](docs-content://get-started/introduction.md)
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button}
[Getting Started Guide](docs-content://get-started/introduction.md)
:::
```
::::::
:::::::

Cross-links are resolved at build time to their target URLs in the documentation site.

## Properties reference

### Button properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| (content) | Yes | - | A Markdown link `[text](url)` that becomes the button. |
| `:type:` | No | `primary` | Button variant: `primary` (filled) or `secondary` (outlined). |
| `:align:` | No | `left` | Horizontal alignment for standalone buttons: `left`, `center`, or `right`. |

### Button group properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `:align:` | No | `left` | Horizontal alignment of the button group: `left`, `center`, or `right`. |
