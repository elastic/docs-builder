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
#...
```

## Structure

`products`
:   A YAML mapping where each key is an Elastic product.
* `display`: A friendly name for the product.
* `versioning`: The versioning system used by the project. The value for this field must match one of the versioning systems defined in [`versions.yml`](https://github.com/elastic/docs-builder/blob/main/config/versions.yml)
* `repository`: The repository name for the product. It's optional and primarily intended for handling edge cases where there is a mismatch between the repository name and the product identifier.



## Substitutions

Writing `{{ product.<product-id> }}` renders the friendly name of the product in the documentation. For example:

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
