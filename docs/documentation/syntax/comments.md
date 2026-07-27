# Comments

## Single line comments

Use `%` to add single-line comments.

```markdown
% This is a comment
```

Make sure to add a space after the `%`.

## Multiline comments

Use `<!--` and `-->` to add multiple line comment blocks.

```markdown
- There is a commented section below.
<!--
This section should not appear -
Neither should this line.
-->
- And there is a commented section above.
```

The closing `-->` can appear anywhere on a line, not just at the beginning. This is useful when commenting out blocks that end with other syntax:

```markdown
Content before the comment.

<!-- :::{note}
This note is commented out.
::: -->

Content after the comment.
```

:::{note}
Content after `-->` on the same line is not supported. Always place content that should be rendered on a separate line after the closing `-->`.
:::
