# Automated settings reference

Elastic Docs V3 can build a Markdown settings reference from a YAML source file.

The `{settings}` directive is generic. Although the largest current examples come from Kibana, the directive can be used by any documentation repository that wants to render structured settings from YAML.

### Syntax

```markdown
::::{settings} /syntax/settings-with-applies-example.yml
::::
```

#### Options

`:deployment: <value>`
:   Filters the rendered settings to only those available for the specified deployment type. When omitted, all settings are shown regardless of deployment.

    Valid values: `ech` (Elastic Cloud Hosted), `ece` (Elastic Cloud Enterprise), `eck` (Elastic Cloud on Kubernetes), `self` (self-managed).

    A setting is considered available for a deployment type if its `applies_to.deployment` block explicitly lists that deployment with a non-removed lifecycle. If a setting has `applies_to` metadata but no entry for the requested deployment, it is treated as unavailable and hidden.

    Settings with no `applies_to` metadata at all are always shown, regardless of the filter.

    ```markdown
    ::::{settings} /syntax/settings-with-applies-example.yml
    :deployment: ech
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
- Inline `{applies_to}` badges inside a setting `description` (for example, to label per-version defaults in a bulleted list).
- Top-level `page_description`.

### Result

_Everything below this line is auto-generated._

::::{settings} /syntax/settings-with-applies-example.yml
::::

For large Kibana-exported YAML samples used in local stress tests, see [Kibana settings YAML samples](../testing/kibana-settings-yaml-samples.md).
