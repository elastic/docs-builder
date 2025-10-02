# Dropdowns

Dropdowns allow you to hide and reveal content on user interaction. By default, dropdowns are collapsed. This hides content until a user clicks the title of the collapsible block.

## Basic dropdown


:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Dropdown Title 1
Dropdown content
:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Dropdown Title 1
Dropdown content
:::
```
::::

:::::

## Open by default

You can specify that the dropdown content should be visible by default. Do this by specifying the `open` option. Users can collapse content by clicking on the dropdown title.

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Dropdown Title 2
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

<<<<<<< HEAD
## Deeplinking

Dropdowns support deeplinking through anchor links. When you navigate to a URL with a hash that points to a dropdown or content within a dropdown, the dropdown will automatically open. When you manually open a dropdown that has a name/anchor, the URL will automatically update to reflect the current state.
=======
## With applies_to badge

You can add an applies_to badge to the dropdown title by specifying the `:applies_to:` option. This displays a badge indicating which deployment types, versions, or other applicability criteria the dropdown content applies to.
>>>>>>> main

:::::{tab-set}

::::{tab-item} Output

<<<<<<< HEAD
:::{dropdown} Deeplink Example
:name: deeplink-example

This dropdown can be opened by navigating to `#deeplink-example`.

When you open this dropdown manually by clicking the title, the URL will automatically update to show `#deeplink-example`.

#### Nested Content [#nested-content]

You can also link directly to content within dropdowns. This content has the anchor `#nested-content`.

=======
:::{dropdown} Dropdown Title
:applies_to: stack: ga 9.0
Dropdown content for Stack GA 9.0
>>>>>>> main
:::

::::

::::{tab-item} Markdown
```markdown
<<<<<<< HEAD
:::{dropdown} Deeplink Example
:name: deeplink-example

This dropdown can be opened by navigating to `#deeplink-example`.

When you open this dropdown manually by clicking the title, the URL will automatically update to show `#deeplink-example`.

#### Nested Content [#nested-content]

You can also link directly to content within dropdowns. This content has the anchor `#nested-content`.

:::
=======
:::{dropdown} Dropdown Title
:applies_to: stack: ga 9.0
Dropdown content for Stack GA 9.0
:::
```
::::

:::::

## Multiple applies_to definitions

You can specify multiple `applies_to` definitions using YAML object notation with curly braces `{}`. This is useful when content applies to multiple deployment types or versions simultaneously.

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Dropdown Title
:applies_to: { ece:, ess: }
Dropdown content for ECE and ECH
:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} Dropdown Title
:applies_to: { ece:, ess: }
Dropdown content for ECE and ECH
:::
```
>>>>>>> main
::::

:::::
