# Legacy URL mappings

This [`legacy-url-mappings.yml`](https://github.com/elastic/docs-builder/blob/main/config/legacy-url-mappings.yml) file manages legacy URL patterns for Elastic documentation, mapping the path of each legacy build URL to a list of documentation versions. It ensures that users can easily find previous versions of our documentation.

This example maps documentation that references `elastic.co/guide/en/elasticsearch/reference/ to Elastic Stack versioned URL paths:

```yml
en/elasticsearch/reference/:
  product: elastic-stack
  legacy_versions: *stack
```

## Structure

`stack anchor`
:   Defines a reusable list of version strings for "stack" projects, e.g., [ '9.0+', '8.18', ... ].

`mappings`
:   A YAML mapping where each key is a legacy documentation URL path (like `en/apm/agent/java/`), and each value is a mapping with:
* `product`: Specifies the product or project type (e.g., `elastic-stack`, `eck`, `ece`, `self-managed`, etc.). Products must be defined in [`products.yml`](https://github.com/elastic/docs-builder/blob/main/config/products.yml). See [products.yml](./products.md) for more information.
* `legacy_versions`: A list of version strings that correspond to the available asciidoc pages.

