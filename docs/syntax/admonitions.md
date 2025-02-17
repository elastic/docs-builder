# Admonitions

Admonitions allow you to highlight important information with varying levels of priority. In software documentation, these blocks are used to emphasize risks, provide helpful advice, or share relevant but non-critical details.

## Basic admonitions

Admonitions can span multiple lines and support inline formatting.
Available admonition types include:

- [Note](#note)
- [Warning](#warning)
- [Tip](#tip)
- [Important](#important)
- [Plain](#plain)

### Note

A relevant piece of information with no serious repercussions if ignored.


:::::{tab-set}

::::{tab-item} Output

:::{note}
This is a note.
It can span multiple lines and supports inline formatting.
:::

::::

::::{tab-item} Markdown

```markdown
:::{note}
This is a note.
It can span multiple lines and supports inline formatting.
:::
```

::::

:::::

### Warning

You could permanently lose data or leak sensitive information.

:::::{tab-set}

::::{tab-item} Output

:::{warning}
This is a warning.
:::

::::

::::{tab-item} Markdown

```markdown
:::{warning}
This is a warning.
:::
```

::::

:::::

### Tip

Advice to help users make better choices when using a feature.

You could permanently lose data or leak sensitive information.

:::::{tab-set}

::::{tab-item} Output

:::{tip}
This is a tip.
:::

::::

::::{tab-item} Markdown

```markdown
:::{tip}
This is a tip.
:::
```

::::

:::::

### Important

Ignoring this information could impact performance or the stability of your system.

:::::{tab-set}

::::{tab-item} Output

:::{important}
This is an important notice.
:::

::::

::::{tab-item} Markdown

```markdown
:::{important}
This is an important notice.
:::
```

::::

:::::

### Plain

A plain admonition is a callout with no further styling. Useful to create a callout that does not quite fit the mold of the stylized admonitions.



:::::{tab-set}

::::{tab-item} Output

:::{admonition} This is my callout
It can *span* multiple lines and supports inline formatting.
:::

::::

::::{tab-item} Markdown

```markdown
:::{admonition} This is my callout
It can *span* multiple lines and supports inline formatting.
:::
```

::::

:::::

## Collapsible admonitions

:::{warning}
Collapsible admonitions are deprecated. Do not use them. Use [dropdowns](./dropdowns.md) instead.
:::

Use `:open: <bool>` to make an admonition collapsible.

:::::{tab-set}

::::{tab-item} Output

:::{note}
:open:

Longer content can be collapsed to take less space.

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
:::


::::

::::{tab-item} Markdown

```markdown
:::{note}
:open:

Longer content can be collapsed to take less space.

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
:::
```

::::

:::::
