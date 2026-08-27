# `related-learning.yml`

The [`related-learning.yml`](https://github.com/elastic/docs-builder/blob/main/config/related-learning.yml) file is a global catalog of learning destinations (training modules, labs, and similar). Pages opt in with the [`{related-learning}`](/syntax/related-learning.md) directive by catalog ID.

This catalog ships with {{dbuild}} and is available in both isolated and assembler builds. Content repositories pick up catalog changes on the next {{dbuild}} version.

## Example

```yml
links:
  apm-with-elastic:
    title: APM with Elastic
    url: https://www.elastic.co/training/apm-with-elastic
  elastic-agent:
    title: Elastic Agent
    url: https://www.elastic.co/training/elastic-agent
```

## Structure

`links`
:   A YAML mapping where each key is a stable catalog ID (typically the training URL slug).
* `title`: The link text shown on the page. Required.
* `url`: An absolute `http` or `https` URL. Required.

Pages are not listed in this file. Authors place `{related-learning}` on the page and pass catalog IDs as the directive argument.

## Starter entries

The catalog currently includes these training modules:

* `apm-with-elastic`
* `elastic-agent`
* `index-basics`
* `data-types-and-mappings`

To add a module, add an entry in a docs-builder pull request. Then use its ID from `{related-learning}` in the content repository.
