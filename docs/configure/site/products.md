# `products.yml`

The [`products.yml`](https://github.com/elastic/docs-builder/blob/main/config/products.yml) file specifies metadata regarding the projects in the organization that use the V3 docs system.

```yml
products:
  apm_agent_dotnet:
    display: 'APM Agent for .NET'
    versioning: 'apm_agent_dotnet'
  edot_collector:
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

| Substitution                    | Result |
|---------------------------------|---|
| `{{ product.apm_agent_dotnet }}` |{{ product.apm_agent_dotnet }}  |
| `{{ .apm_agent_ios }}`           | {{ .apm_agent_ios }} |

## See also

[](./versions.md)
[](./legacy-url-mappings.md)
