# Automated settings reference

Elastic Docs V3 supports the ability to build a markdown settings reference from a YAML source file.

### Syntax

```markdown
:::{settings} /syntax/settings-with-applies-example.yml
:::
```

### Example

See `/syntax/settings-with-applies-example.yml` for a full, schema-compliant sample.

It demonstrates:

- Group `description` and `example`.
- Setting `id`, `datatype`, `default`, and `options`.
- `note`, `tip`, `warning`, `important`, and `deprecation_details`.
- Nested `settings`.
- `applies_to` inheritance and override behavior.

### Result

_Everything below this line is auto-generated._

:::{settings} /syntax/settings-with-applies-example.yml
:::