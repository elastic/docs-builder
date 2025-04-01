# Comments

Elastic docs V3 currently only supports single-line comments.

## Single line comments

Use `%` to add single-line comments.

```markdown
% This is a comment
```

Make sure to add a space after the `%`.

## Block comments

Use `<!---` and `-->` to add multi-line block comments.

```markdown
- There is a commented section below.
<!---
This section should not appear -
Neither should this line.
-->
- And there is a commented section above.
```
