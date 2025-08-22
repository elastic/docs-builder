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

Dropdowns support deeplinking via anchor links. When you navigate to a URL with a hash that points to a dropdown or content within a dropdown, the dropdown will automatically open. When you manually open a dropdown that has a name/anchor, the URL will automatically update to reflect the current state.

### Features

- **Automatic opening**: Navigate to `#dropdown-name` and the dropdown opens automatically
- **URL updates**: Open a dropdown manually and the URL updates to show the anchor
- **Nested content**: Link directly to headings or content within dropdowns
- **Browser navigation**: Proper back/forward button support

:::::{tab-set}

::::{tab-item} Output

:::{dropdown} Deeplink Example
:name: deeplink-example

This dropdown can be opened by navigating to `#deeplink-example`.

When you open this dropdown manually by clicking the title, the URL will automatically update to show `#deeplink-example`.

#### Nested Content [#nested-content]

You can also link directly to content within dropdowns. This content has the anchor `#nested-content`.

:::

**Test the features:**
- [Link to dropdown](#deeplink-example) - Opens the dropdown and updates URL
- [Link to nested content](#nested-content) - Opens dropdown and scrolls to nested content
- Try opening/closing the dropdown manually and watch the URL change

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

**Test the features:**
- [Link to dropdown](#deeplink-example) - Opens the dropdown and updates URL
- [Link to nested content](#nested-content) - Opens dropdown and scrolls to nested content
- Try opening/closing the dropdown manually and watch the URL change
```
::::

:::::

### Use Cases

Deeplinking is particularly useful for:

- **FAQ sections**: Allow users to share links to specific questions
- **Documentation**: Link directly to explanations that might be collapsed by default  
- **Troubleshooting guides**: Share direct links to specific solutions
- **API documentation**: Link to specific endpoint details within collapsed sections

The URL behaves just like clicking on a heading with an anchor - it updates automatically when you interact with the content.
