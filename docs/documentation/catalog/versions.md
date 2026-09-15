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

:::{include} /data/release-notes/_snippets/tag-processing.md
:::

See [Write cumulative documentation](https://www.elastic.co/docs/contribute-docs/how-to/cumulative-docs) for more information.

## Version substitutions

Like `products.yml` (which provides `{{product.<id>}}` substitutions), `versions.yml` exposes version numbers as substitution variables so you can reference current or next versions without hardcoding.

Use `{{version.<system> | <mutation>}}` to display version numbers. [Mutations](/syntax/substitutions.md#mutations) let you derive related versions from the current value:

| Substitution | Example output | Description |
|---|---|---|
| `{{version.stack \| M.x}}` | `9.x` | Major with `.x` wildcard |
| `{{version.stack \| M.M}}` | `9.0` | Major.minor |
| `{{version.stack \| M}}` | `9` | Major only |
| `{{version.stack \| M+1}}` | `10` | Next major |
| `{{version.stack \| M+1 \| M.M}}` | `10.0` | Next major as major.minor |
| `{{version.stack \| M.M+1}}` | `9.1` | Next minor |

This is useful for documentation that references version-specific behavior without needing updates on every release:

```markdown
This feature is available in {{version.stack | M.M}} and later.
Upgrade to {{version.stack | M+1 | M.M}} for the next major release.
```

For the full list of available mutations, see [substitution mutations](/syntax/substitutions.md#mutations).