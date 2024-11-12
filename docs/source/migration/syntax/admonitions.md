---
title: Admonitions
---

This guide provides instructions for converting common AsciiDoc admonitions to MyST Markdown format.

## Admonition Mapping

| AsciiDoc     | MyST Markdown  |
|--------------|----------------|
| NOTE         | `{note}`       |
| TIP          | `{tip}`        |
| IMPORTANT    | `{important}`  |
| WARNING      | `{warning}`    |
| CAUTION      | `{caution}`    |
| DANGER       | `{danger}`     |

### Example Conversion

In AsciiDoc:
```text
[NOTE]
====
This is a note.
It can be multiple lines, but not multiple paragraphs.
====
```

In MyST Markdown:
```
:::{note}
This is a note.
It can be multiple lines, but not multiple paragraphs.
:::
```

:::{note}
This is a note.
It can be multiple lines, but not multiple paragraphs.
:::

## Admonition Paragraphs

For single-paragraph admonitions, convert them directly to MyST admonition syntax using the appropriate admonition type. Use triple colons `:::` to open and close the block.

### Example

In AsciiDoc:
```text
[WARNING]
====
This is a warning paragraph.
====
```

In MyST Markdown:
```
:::{warning}
This is a warning paragraph.
:::
```

:::{warning}
This is a warning paragraph.
:::

## Multi-Paragraph Admonitions

In AsciiDoc, multi-paragraph admonitions are formatted the same as single paragraphs. In MyST, you can still use the admonition type but separate paragraphs with blank lines.

### Example

In AsciiDoc:
```text
[IMPORTANT]
====
This is an important note.
It contains multiple paragraphs.

Make sure to read it carefully.
====
```

In MyST Markdown:
```
:::{important}
This is an important note.
It contains multiple paragraphs.

Make sure to read it carefully.
:::
```

:::{important}
This is an important note.
It contains multiple paragraphs.

Make sure to read it carefully.
:::

## Custom Titles for Admonitions

To give an admonition a custom title in MyST, use the `admonition` directive with a `class` attribute. This is useful if you want to style the block as one of the core admonition types but need a custom title.

### Example

In AsciiDoc:
```text
[NOTE]
.Title goes here
====
This note has a custom title.
====
```

In MyST Markdown:
```
:::{admonition} Title goes here
:class: note

This note has a custom title.
:::
```

:::{admonition} Title goes here
:class: note

This note has a custom title.
:::

## Collapsible Admonitions

In MyST Markdown, you can make an admonition collapsible by adding a `dropdown` class, provided by the `sphinx-togglebutton` extension.

### Example

```
:::{note}
:class: dropdown

This admonition can be collapsed, making it useful for longer notes or instructions.
:::
```

:::{note}
:class: dropdown

This admonition can be collapsed, making it useful for longer notes or instructions.
:::