# `products.yml`

The [`products.yml`](https://github.com/elastic/docs-builder/blob/main/config/products.yml) file specifies metadata regarding the projects in the organization leveraging v3.

```yml
products:
  apm-agent-dotnet:
    display: 'APM Agent for .NET'
    versioning: 'apm_agent_dotnet'
  edot-collector:
    display: 'Elastic Distribution of OpenTelemetry Collector'
    versioning: 'stack'
#...
```

## Structure

`products`
:   A YAML mapping where each key is an Elastic product.
* `display`: A friendly name for the product.
* `versioning`: The versioning system used by the project. The value for this field must match one of the versioning systems defined in [`versions.yml`](https://github.com/elastic/docs-builder/blob/main/config/versions.yml)



## Substitutions

The following substitutions are available:

| Substitution | Result |
| --- |---|
| `{{ product.apm-agent-dotnet }}` |{{ product.apm-agent-dotnet }}   |
| `{{ .apm-agent-ios }}` | {{ .apm-agent-ios }} |

## See also

[](./versions.md)
[](./legacy-url-mappings.md)
