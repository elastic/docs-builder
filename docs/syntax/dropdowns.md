# Dropdowns

Dropdowns allow you to hide and reveal content on user interaction. By default, dropdowns are collapsed. This hides content until a user clicks the title of the collapsible block.

## Basic dropdown


:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Dropdown Title
Dropdown content
:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Dropdown Title
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
Dropdown content
:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Dropdown Title
:open:
Dropdown content
:::
```
::::

:::::

## Deeplinking

Dropdowns support deeplinking through anchor links. When you navigate to a URL with a hash that points to a dropdown or content within a dropdown, the dropdown will automatically open. When you manually open a dropdown that has a name/anchor, the URL will automatically update to reflect the current state.

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Deeplink Example
:name: deeplink-example

This dropdown can be opened by navigating to `#deeplink-example`.

When you open this dropdown manually by clicking the title, the URL will automatically update to show `#deeplink-example`.

#### Nested Content [#nested-content]

You can also link directly to content within dropdowns. This content has the anchor `#nested-content`.

:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Deeplink Example
:name: deeplink-example

This dropdown can be opened by navigating to `#deeplink-example`.

When you open this dropdown manually by clicking the title, the URL will automatically update to show `#deeplink-example`.

#### Nested Content [#nested-content]

You can also link directly to content within dropdowns. This content has the anchor `#nested-content`.

:::
::::

:::::
