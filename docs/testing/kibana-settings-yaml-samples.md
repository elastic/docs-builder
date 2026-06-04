# Kibana settings YAML samples (local fixtures)

These `{settings}` inclusions mirror files used to stress-test automated settings rendering.

Some descriptions use links and anchors that target the real Kibana reference pages, not this local aggregate page; the builder rewrites or validates many of them, but diagnostics can still reference those external paths.

## kibana-alert-action-settings.yml

:::{settings} /testing/kibana-alert-action-settings.yml
:::

## kibana-fleet-settings.yml

:::{settings} /testing/kibana-fleet-settings.yml
:::

## kibana-general-settings.yml

:::{settings} /testing/kibana-general-settings.yml
:::

## kibana-logging-settings.yml

:::{settings} /testing/kibana-logging-settings.yml
:::

## kibana-monitoring-settings.yml

:::{settings} /testing/kibana-monitoring-settings.yml
:::

## kibana-security-settings.yml

:::{settings} /testing/kibana-security-settings.yml
:::

## Deployment filter preview

The `:deployment:` option filters settings to only those available for the specified deployment type.
Settings with no `applies_to` are always shown. Settings with `applies_to` that do not explicitly list
the deployment are treated as unavailable.

Accepted values: `ech`, `ece`, `eck`, `self`.

### kibana-general-settings.yml — ECH only

:::{settings} /testing/kibana-general-settings.yml
:deployment: ech
:::

### kibana-general-settings.yml — self-managed only

:::{settings} /testing/kibana-general-settings.yml
:deployment: self
:::

### kibana-security-settings.yml — ECH only

:::{settings} /testing/kibana-security-settings.yml
:deployment: ech
:::
