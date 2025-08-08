# `versions.yml`

The [`versions.yml`](https://github.com/elastic/docs-builder/blob/main/config/versions.yml) file specifies which versions of each product should be recognized as the minimum (base) and the latest (current) in documentation builds.

This example sets the Elastic Stack base and current versions while also assigning them to a variable that can be accessed with `*stack`

```yml
versioning_systems:
  stack: &stack
    base: 9.0
    current: 9.0.4
```

Versions set in this file are surfaced to the user via `applies_to` tags.

:::{include} /contribute/_snippets/tag-processing.md
:::

See [](/contribute/cumulative-docs/index.md) for more information.