# Legacy URL mappings

This [`legacy-url-mappings.yml`](https://github.com/elastic/docs-builder/blob/main/config/legacy-url-mappings.yml) file manages legacy URL patterns for Elastic documentation, mapping the path of each legacy build URL to a list of documentation versions. It ensures that users can easily find previous versions of our documentation.

This example maps documentation that references `elastic.co/guide/en/elasticsearch/reference/ to Elastic Stack versioned URL paths:

```yml
en/elasticsearch/reference/: *stack
```

## Structure

`stack anchor`
:   Defines a reusable list of version strings for "stack" projects, e.g., [ '9.0+', '8.18', ... ].

`mappings`
:   A YAML mapping where each key is a legacy documentation URL path (like `en/apm/agent/java/`) and the value is a list of asciidoc versions that exist for that path.

:::{important}
The first version in the `mappings` list is treated as the "current" version in documentation version dropdown.
:::

## Example entry

