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

## With applies_to badge

You can add an applies_to badge to the dropdown title by specifying the `:applies_to:` option. This displays a badge indicating which deployment types, versions, or other applicability criteria the dropdown content applies to.

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Dropdown Title
:applies_to: stack: ga 9.0
Dropdown content for Stack GA 9.0
:::

::::

::::{tab-item} Markdown
```markdown
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
::::

:::::

## Anchors and deep linking

Dropdowns automatically generate anchors from their titles, allowing you to link directly to them. The anchor is created by converting the title to lowercase and replacing spaces with hyphens (slugify).

For example, a dropdown with title "Installation Guide" will have the anchor `#installation-guide`.

### Custom anchors

You can specify a custom anchor using the `:name:` option:

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} My Dropdown
:name: custom-anchor
Content that can be linked to via #custom-anchor
:::

::::

::::{tab-item} Markdown
```markdown
:::{dropdown} My Dropdown
:name: custom-anchor
Content that can be linked to via #custom-anchor
:::
```
::::

:::::

### Duplicate anchors

If multiple elements (dropdowns or headings) in the same document have the same anchor, the build will emit a hint warning. While this doesn't fail the build, it may cause linking issues. Ensure each dropdown has a unique title or use the `:name:` option to specify unique anchors.
