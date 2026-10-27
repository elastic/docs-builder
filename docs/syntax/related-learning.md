# Related learning

The `{related-learning}` directive adds a **Related learning** section that links to named destinations in the global catalog (training modules, labs, and similar). Titles and URLs live in [`related-learning.yml`](/documentation/catalog/related-learning.md); the page only picks catalog IDs.

The heading is part of the directive, so it appears in **On this page**.

:::{related-learning} apm-with-elastic
:::

## Basic usage

List one or more catalog IDs as the directive argument. Display order matches the argument.

:::::::{tab-set}
::::::{tab-item} Output
:::{related-learning} apm-with-elastic
:::
::::::

::::::{tab-item} Markdown
```markdown
:::{related-learning} apm-with-elastic
:::
```
::::::
:::::::

## Multiple IDs

Separate IDs with commas. Order is the order of the list.

```markdown
:::{related-learning} index-basics, data-types-and-mappings
:::
```

## Custom heading

`:heading:` is optional. The default is `Related learning`. A custom heading also changes the **On this page** slug (the default heading uses the slug `related-learning-heading`).

```markdown
:::{related-learning} elastic-agent
:heading: Learn Elastic Agent
:::
```

## Argument and options

Argument
:   Required. Comma-separated catalog IDs. Unknown IDs fail the build. Duplicate IDs emit a warning and are skipped.

`:heading:`
:   Optional. Text for the section heading. Default: `Related learning`.

Links open in a new tab. Add new destinations in a docs-builder change to `related-learning.yml`; content repositories pick up new IDs on the next docs-builder version.
