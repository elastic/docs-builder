# `products.yml`

The [`products.yml`](https://github.com/elastic/docs-builder/blob/main/config/products.yml) file specifies metadata regarding the projects in the organization that use the V3 docs system.

```yml
products:
  apm-agent-dotnet:
    display: 'APM .NET Agent'
    versioning: 'apm-agent-dotnet'
  edot-collector:
    display: 'Elastic Distribution of OpenTelemetry Collector'
    versioning: 'stack'
    repository: 'elastic-edot-collector'
  docs-builder:
    display: 'Elastic Docs Builder'
    repository: 'docs-builder'
    features:
      release-notes: true
#...
```

## Structure

`products`
:   A YAML mapping where each key is an Elastic product.
* `display`: A friendly name for the product.
* `versioning`: The versioning system used by the project. The value for this field must match one of the versioning systems defined in [`versions.yml`](https://github.com/elastic/docs-builder/blob/main/config/versions.yml). Optional for products that only participate in release notes.
* `repository`: The repository name for the product. It's optional and primarily intended for handling edge cases where there is a mismatch between the repository name and the product identifier.
* `features`: An optional mapping that controls which docs-builder subsystems the product participates in. When omitted, all features are enabled (backward compatible). When present, only the listed features are active. The available features are:
  * `public-reference`: The product can be referenced in `applies_to` blocks, page frontmatter `products`, and gets `{{ product.<id> }}` substitutions. This is what "being a documentation product" means today.
  * `release-notes`: The product participates in the changelog and release notes system.

:::{note}
Products without a `features` mapping behave exactly as before -- they participate in all subsystems. The `features` mapping is only needed for products that should participate in a subset of features, such as internal tools that need release notes but don't have public-facing documentation.
:::

## Substitutions

Writing `{{ product.<product-id> }}` renders the friendly name of the product in the documentation. Substitutions are generated only for products with the `public-reference` feature (or no explicit `features` mapping). For example:

| Substitution                    | Result |
|---------------------------------|---|
| `{{ product.apm-agent-dotnet }}` |{{ product.apm-agent-dotnet }}  |
| `{{ product.edot-collector }}`           | {{ product.edot-collector }} |

You can also use the shorthand notation `{{ .<product_id> }}`. For example:

| Substitution                    | Result |
|---------------------------------|---|
| `{{ .apm-agent-dotnet }}` |{{ .apm-agent-dotnet }}  |
| `{{ .edot-collector }}`           | {{ .edot-collector }} |


:::{note}
While the recommended separator is a hyphen (`-`) to promote cohesion, underscores (`_`) are also supported, and internally read as hyphens.
:::

## See also

[](./versions.md)
[](./legacy-url-mappings.md)
