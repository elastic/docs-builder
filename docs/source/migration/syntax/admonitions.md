
---
title: Admonition Blocks
---

Admonition blocks allow you to highlight important information with varying levels of priority. In software documentation, these blocks are used to emphasize risks, provide helpful advice, or share relevant but non-critical details.

Admonitions are critical for:
- Preventing data loss or security issues.
- Improving system performance and stability.
- Offering helpful tips for better product usage.

---

## Types of Admonitions

| **Type**     | **When to use it**                                                                 |
|--------------|-----------------------------------------------------------------------------------|
| **Warning**  | You could permanently lose data or leak sensitive information.                   |
| **Important**| Ignoring the information could impact performance or the stability of your system.|
| **Note**     | A relevant piece of information with no serious repercussions if ignored.        |
| **Tip**      | Advice to help you make better choices when using a feature.                     |

---

`````{tab-set}

````{tab-item} Asciidoc Syntax

To create admonitions in Asciidoc, you can use inline or block syntax.

**Inline Admonition:**
```
NOTE: This is a note.
It can be multiple lines, but not multiple paragraphs.
```

**Block Admonition:**
```
[WARNING]
=======
This is a warning.

It can contain multiple paragraphs.
=======
```
````

````{tab-item} MD Syntax

The new build system uses MyST Markdown directives for admonitions.

**Basic Syntax:**

```{note}
This is a note.
It can span multiple lines and supports inline formatting.
```

**Available Types:**
- `note`
- `caution`
- `tip`
- `attention`
````
`````

## Related Issues

-
