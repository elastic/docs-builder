# Buttons

Buttons provide styled link elements for calls to action in documentation. Use buttons to highlight important navigation points, downloads, or external resources.

:::{button} This is an example
:link: docs-content://get-started/introduction.md
:::

## Basic button

A button requires the button text as an argument and a `:link:` property:

:::::::{tab-set}
::::::{tab-item} Output
:::{button} Get Started
:link: #
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button} Get Started
:link: /get-started
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
:::{button} Primary Button
:link: #
:type: primary
:::
:::{button} Secondary Button
:link: #
:type: secondary
:::
::::
::::::

::::::{tab-item} Markdown
```markdown
::::{button-group}
:::{button} Primary Button
:link: /primary
:type: primary
:::
:::{button} Secondary Button
:link: /secondary
:type: secondary
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
:::{button} Elastic Fundamentals
:link: #
:type: primary
:::
:::{button} Upgrade Versions
:link: #
:type: secondary
:::
::::
::::::

::::::{tab-item} Markdown
```markdown
::::{button-group}
:::{button} Elastic Fundamentals
:link: /get-started
:type: primary
:::
:::{button} Upgrade Versions
:link: /deploy-manage/upgrade
:type: secondary
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
:::{button} Left (default)
:link: #
:align: left
:::

:::{button} Center
:link: #
:align: center
:::

:::{button} Right
:link: #
:align: right
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button} Left (default)
:link: /example
:align: left
:::

:::{button} Center
:link: /example
:align: center
:::

:::{button} Right
:link: /example
:align: right
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
:::{button} Centered Group
:link: #
:::
:::{button} Second Button
:link: #
:type: secondary
:::
::::
::::::

::::::{tab-item} Markdown
```markdown
::::{button-group}
:align: center
:::{button} Centered Group
:link: /example
:::
:::{button} Second Button
:link: /example
:type: secondary
:::
::::
```
::::::
:::::::

## External links

External links (URLs outside elastic.co) automatically open in a new tab. You can also explicitly mark a link as external:

:::::::{tab-set}
::::::{tab-item} Output
:::{button} Visit GitHub
:link: https://github.com/elastic
:external:
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button} Visit GitHub
:link: https://github.com/elastic
:external:
:::
```
::::::
:::::::

External links include `target="_blank"` and `rel="noopener noreferrer"` attributes for security.

## Cross-repository links

Buttons support [cross-repository links](links.md#cross-repository-links) using the `scheme://path` syntax:

:::::::{tab-set}
::::::{tab-item} Output
:::{button} Getting Started Guide
:link: docs-content://get-started/introduction.md
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{button} Getting Started Guide
:link: docs-content://get-started/introduction.md
:::
```
::::::
:::::::

Cross-links are resolved at build time to their target URLs in the documentation site.

## Properties reference

### Button properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| (argument) | Yes | - | The button text to display. |
| `:link:` | Yes | - | The URL the button links to. Supports internal paths, external URLs, and cross-repository links (e.g., `kibana://api/index.md`). |
| `:type:` | No | `primary` | Button variant: `primary` (filled) or `secondary` (outlined). |
| `:align:` | No | `left` | Horizontal alignment for standalone buttons: `left`, `center`, or `right`. |
| `:external:` | No | auto | If set, the link opens in a new tab. Auto-detected for non-elastic.co URLs. Cross-links are not treated as external by default. |

### Button group properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `:align:` | No | `left` | Horizontal alignment of the button group: `left`, `center`, or `right`. |

