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

    A setting is considered available for a deployment type if its `applies_to` metadata lists that deployment with a lifecycle other than `removed` or `unavailable`. Flat keys such as `ech: ga` and a nested `deployment:` map both work. If a setting has `applies_to` metadata but no entry for the requested deployment, it is treated as unavailable and hidden.

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
        # Same keys as applies.md. The authoring contract is different.
        # See "applies_to in settings YAML" on this page.
        #
        # applies_to:
        #   stack: ga 9.2
        #   ech: ga
        #   ece: ga
        #   eck: ga
        #   self: ga
        #   serverless: ga
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
        #   Child settings inherit applies_to when they omit the field.
        #   A child applies_to map replaces the parent. It does not merge keys.
        #   - setting: "[n].url"
        #     description: |
        #       REQUIRED
```

### applies_to in settings YAML [settings-yaml]

The keys are the same as [Applies to](applies.md). The authoring contract is different, because each setting renders a **Supported on** line.

In body Markdown, name only the keys that apply to the content. In settings YAML, list every deployment key so that line is complete and the `:deployment:` filter is explicit.

| Key | What it means here | Write |
|---|---|---|
| `stack` | Lifecycle and version of the setting | `ga`, `preview 9.2`, or a history such as `preview 9.0-9.2, ga 9.3+`. No version means all versions. |
| `ech`, `ece`, `eck`, `self` | Supported on that deployment, or not | Always list all four. `ga` if supported. `unavailable` if not. Never a version. Never `preview`, `experimental`, `deprecated`, or `removed`. |
| `serverless` | Supported on serverless, or not | Always list it. `ga` if supported. `unavailable` if not. Never a version. |

`ga` on a deployment key is a support flag. It does not mean the setting is generally available. If `stack` is `preview` and the setting exists on Elastic Cloud Hosted, write `ech: ga`.

`unavailable` is not rendered as a badge. It hides the setting from `:deployment:`. Omitting a deployment key also hides it from that filter. Still write `unavailable` so the Supported on line is complete.

Child settings inherit the parent's `applies_to` when they omit the field. If a child sets `applies_to`, that map replaces the parent. It does not merge keys.

To scope a `note`, `tip`, `warning`, or `important` to a version or deployment, put `:applies_to:` on the first line of that field. docs-builder wraps the field in an admonition. Do not add a `:::{note}` wrapper.

Preferred map (supported everywhere):

```yaml
applies_to:
  stack: ga 9.2
  ech: ga
  ece: ga
  eck: ga
  self: ga
  serverless: ga
```

Technical preview that is still supported on every deployment:

```yaml
applies_to:
  stack: preview 9.2
  ech: ga
  ece: ga
  eck: ga
  self: ga
  serverless: ga
```

Self-managed only:

```yaml
applies_to:
  stack: ga 9.2
  ech: unavailable
  ece: unavailable
  eck: unavailable
  self: ga
  serverless: unavailable
```

### Example

See `/syntax/settings-with-applies-example.yml` for a full, schema-compliant sample.

It demonstrates:

- Group `description`, `note`, and `example`.
- Setting `id`, `datatype`, `default`, and `options`.
- `note`, `tip`, `warning`, `important`, and `deprecation_details`.
- Nested `settings`.
- A complete `applies_to` map, including `unavailable` keys and `stack: preview` with `ech: ga`.
- `applies_to` inheritance when a child omits the field, and replacement when it sets its own map.
- Inline `{applies_to}` badges inside a setting `description` (for example, to label per-version defaults in a bulleted list).
- A gated `warning` with `:applies_to:` on the first line.
- Top-level `page_description`.

### Result

_Everything below this line is auto-generated._

::::{settings} /syntax/settings-with-applies-example.yml
::::

For large Kibana-exported YAML samples used in local stress tests, see `kibana-settings-yaml-samples.md` in the `docs-tests/` docset at the repository root (outside the main `docs/` folder).
