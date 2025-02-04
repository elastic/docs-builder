# Dropdowns

Dropdowns allow you to hide and reveal content on user interaction. By default, dropdowns are collapsed. This hides content until a user clicks the title of the collapsible block.

## Syntax

```markdown
:::{dropdown} Dropdown Title
Dropdown content
:::
```

:::{dropdown} Dropdown Title
Dropdown content
:::

### Open by default

You can specify that the dropdown content should be visible by default. Do this by specifying the `open` option. Users can collapse content by clicking on the dropdown title.

```markdown
:::{dropdown} Dropdown Title
:open:
Dropdown content
:::
```

:::{dropdown} Dropdown Title
:open:
Dropdown content
:::
