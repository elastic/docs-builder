# Dropdowns

Dropdowns allow you to hide and reveal content on user interaction. By default, dropdowns are collapsed. This hides content until a user clicks the title of the collapsible block.

## Basic dropdown


:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Dropdown Title
:name: basic-dropdown
Dropdown content
:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Dropdown Title
:name: basic-dropdown
Dropdown content
:::
```
::::

:::::

## Open by default

You can specify that the dropdown content should be visible by default. Do this by specifying the `open` option. Users can collapse content by clicking on the dropdown title.

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Dropdown Title
:open:
:name: open-dropdown
Dropdown content
:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Dropdown Title
:open:
:name: open-dropdown
Dropdown content
:::
```
::::

:::::

## Deeplinking

Dropdowns support deeplinking via anchor links. When you navigate to a URL with a hash that points to a dropdown or content within a dropdown, the dropdown will automatically open.

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Deeplink Example
:name: deeplink-example

This dropdown can be opened by navigating to `#deeplink-example`.

#### Nested Content [#nested-content]

You can also link directly to content within dropdowns. This content has the anchor `#nested-content`.

:::

Test links:
- [Link to dropdown](#deeplink-example)
- [Link to nested content](#nested-content)

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Deeplink Example
:name: deeplink-example

This dropdown can be opened by navigating to `#deeplink-example`.

#### Nested Content [#nested-content]

You can also link directly to content within dropdowns. This content has the anchor `#nested-content`.

:::

Test links:
- [Link to dropdown](#deeplink-example)
- [Link to nested content](#nested-content)
```
::::

:::::
