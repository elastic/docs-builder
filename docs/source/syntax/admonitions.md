---
title: Admonitions
---

Admonitions allow you to highlight important information with varying levels of priority. In software documentation, these blocks are used to emphasize risks, provide helpful advice, or share relevant but non-critical details.

```{attention}
Asciidoc and V3 currently support different admonition types. See [#106](https://github.com/elastic/docs-builder/issues/106) for details.
```

## Basic admonitions

Admonitions can span multiple lines and support inline formatting.

`````{tab-set}

````{tab-item} MD Syntax

### Available admonition types

- `note`
- `caution`
- `tip`
- `attention`

### Syntax

**Note**

A relevant piece of information with no serious repercussions if ignored.

```markdown
:::{note}
This is a note.
It can span multiple lines and supports inline formatting.
:::
```

:::{note}
This is a note.
:::

**Caution**

You could permanently lose data or leak sensitive information.

```markdown
:::{caution}
This is a caution.
:::
```

```{caution}
This is a caution.
```

**Tip**

Advice to help users make better choices when using a feature.

```markdown
:::{tip}
This is a tip.
:::
```

```{tip}
This is a tip.
```

**Attention**

Ignoring this information could impact performance or the stability of your system.

```markdown
:::{attention}
This is an attention.
:::
```

```{attention}
This is an attention.
```


````

````{tab-item} Asciidoc Syntax

| **Asciidoc Type**     | **When to use it**                                                        |
|--------------|-----------------------------------------------------------------------------------|
| **Warning**  | You could permanently lose data or leak sensitive information.                   |
| **Important**| Ignoring the information could impact performance or the stability of your system.|
| **Note**     | A relevant piece of information with no serious repercussions if ignored.        |
| **Tip**      | Advice to help you make better choices when using a feature.                     |


**Inline Admonition:**
```none
NOTE: This is a note.
It can be multiple lines, but not multiple paragraphs.
```

**Block Admonition:**

```none
[WARNING]
=======
This is a warning.

It can contain multiple paragraphs.
=======
:::
```

`````

## Collapsible admonitions

You can use `:open: <bool>` to make an admonition collapsible.

```none
:::{note}
:open:

Longer content can be collapsed to take less space.

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
:::
```

```{note}
:open:

Longer content can be collapsed to take less space.

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
```
