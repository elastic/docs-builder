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



**Just for testing: A few yaml files from David's Kibana settings PRs**
**(Sorry for the messyness)**




**~~~~~~~~~~~~~~~~ kibana-alert-action-settings.yml ~~~~~~~~~~~~~~~~**
:::{settings} /syntax/kibana-alert-action-settings.yml
:::

**~~~~~~~~~~~~~~~~ kibana-fleet-settings.yml ~~~~~~~~~~~~~~~~**
:::{settings} /syntax/kibana-fleet-settings.yml
:::


**~~~~~~~~~~~~~~~~ kibana-general-settings.yml ~~~~~~~~~~~~~~~~**
:::{settings} /syntax/kibana-general-settings.yml
:::


**~~~~~~~~~~~~~~~~ kibana-logging-settings.yml ~~~~~~~~~~~~~~~~**
:::{settings} /syntax/kibana-logging-settings.yml
:::

**~~~~~~~~~~~~~~~~ kibana-monitoring-settings.yml ~~~~~~~~~~~~~~~~**
:::{settings} /syntax/kibana-monitoring-settings.yml
:::

**~~~~~~~~~~~~~~~~ kibana-security-settings.yml ~~~~~~~~~~~~~~~~**
:::{settings} /syntax/kibana-security-settings.yml
:::