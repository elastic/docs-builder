# File inclusion

File inclusion is useful for
- including entire pages in a content set (usually done in the `docset.yml` file)
- breaking files into smaller units and including them where appropriate

If there are portions of content that are relevant to multiple pages, you can inject content from another file into any given MD file using the `include` directive.

### Syntax

```markdown
:::{include} _snippets/reusable-snippet.md
:::
```

:::{include} _snippets/reusable-snippet.md
:::

### Asciidoc syntax

```asciidoc
include::shared-monitor-config.asciidoc[]
```