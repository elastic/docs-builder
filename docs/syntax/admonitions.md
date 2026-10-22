# Admonitions

Admonitions allow you to highlight important information with varying levels of priority. In software documentation, these blocks are used to emphasize risks, provide helpful advice, or share relevant but non-critical details.

Use them sparingly. Admonitions interrupt the reader to highlight something they shouldn't miss, and the more you use them, the less any one of them stands out. Aim for no more than one per section, don't stack them, and keep information that readers need to complete a task in the body text. For more writing guidance, refer to the [Elastic style guide](https://www.elastic.co/docs/contribute-docs/style-guide/formatting#admonitions).

## Basic admonitions

Admonitions can span multiple lines and support inline formatting.

:::::{tab-set}

::::{tab-item} Output

:::{admonition} Multiple paragraphs
Separate paragraphs with a blank line, just like regular Markdown.

Adjacent lines without a blank line between them render as a single paragraph.
:::

::::

::::{tab-item} Markdown

```markdown
:::{admonition} Multiple paragraphs
Separate paragraphs with a blank line, just like regular Markdown.

Adjacent lines without a blank line between them render as a single paragraph.
:::
```

::::

:::::

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
This is a plain callout.

It can *span* multiple lines and supports inline formatting.
:::

::::

::::{tab-item} Markdown

```markdown
:::{admonition} This is my callout
This is a plain callout.

It can *span* multiple lines and supports inline formatting.
:::
```

::::

:::::

## Applies to information

:::{include} _snippets/applies-to-admonitions.md
:::
