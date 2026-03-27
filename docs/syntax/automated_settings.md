# Automated settings reference

Elastic Docs V3 can build a Markdown settings reference from a YAML source file.

The `{settings}` directive is generic. Although the largest current examples come from Kibana, the directive can be used by any documentation repository that wants to render structured settings from YAML.

### Syntax

```markdown
::::{settings} /syntax/settings-with-applies-example.yml
::::
```

### Schema

The schema below reflects the structure currently supported by docs-builder. For the original settings-gen schema that inspired this format, see [the Kibana schema reference](https://github.com/elastic/kibana/tree/main/docs/settings-gen#schema).

```yaml
product: REQUIRED
collection: REQUIRED
# id: OPTIONAL
# page_description: OPTIONAL multiline Markdown
# note: OPTIONAL multiline Markdown or string

groups:
  - group: REQUIRED
    # id: OPTIONAL
    # description: OPTIONAL multiline Markdown
    # note: OPTIONAL multiline Markdown or string
    # example: OPTIONAL multiline Markdown

    settings:
      - setting: REQUIRED
        description: |
          REQUIRED
          Multiline Markdown.
        # id: OPTIONAL
        # applies_to: OPTIONAL docs-builder applicability metadata
        #
        # Supports docs-builder applies_to syntax, for example:
        #
        # applies_to:
        #   stack: ga 9.2
        #   ech: ga
        #   self: ga
        #
        # note: OPTIONAL
        # tip: OPTIONAL
        # warning: OPTIONAL
        # important: OPTIONAL
        # deprecation_details: OPTIONAL
        # datatype: OPTIONAL
        # default: OPTIONAL
        # options:
        #   - option: OPTIONAL
        #     description: OPTIONAL
        # example: OPTIONAL multiline Markdown
        # settings: OPTIONAL nested settings list
        #   Child settings inherit applies_to from the parent unless overridden.
        #   - setting: "[n].url"
        #     description: |
        #       REQUIRED
```

### Example

See `/syntax/settings-with-applies-example.yml` for a full, schema-compliant sample.

It demonstrates:

- Group `description`, `note`, and `example`.
- Setting `id`, `datatype`, `default`, and `options`.
- `note`, `tip`, `warning`, `important`, and `deprecation_details`.
- Nested `settings`.
- `applies_to` inheritance and override behavior.
- Top-level `page_description`.

### Result

_Everything below this line is auto-generated._

::::{settings} /syntax/settings-with-applies-example.yml
::::

For large Kibana-exported YAML samples used in local stress tests, see [Kibana settings YAML samples](../testing/kibana-settings-yaml-samples.md).
