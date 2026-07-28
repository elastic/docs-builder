# Applies switch

The applies-switch directive creates tabbed content where each tab displays an applies_to badge instead of a text title. This is useful for showing content that varies by deployment type, version, or other applicability criteria.

## Basic usage

::::::{tab-set}
:::::{tab-item} Output

::::{applies-switch}

:::{applies-item} stack: ga 9.0+
Content for Stack
:::

:::{applies-item} stack: experimental 9.1+
Content for experimental Stack features
:::

:::{applies-item} serverless: ga
Content for Serverless
:::

::::

:::::
:::::{tab-item} Markdown

```markdown
::::{applies-switch}

:::{applies-item} stack: ga 9.0+
Content for Stack
:::

:::{applies-item} stack: experimental 9.1+
Content for experimental Stack features
:::

:::{applies-item} serverless: ga
Content for Serverless
:::

::::
```
:::::
::::::

## Multiple `applies_to` definitions

You can specify multiple `applies_to` definitions in a single `applies-item` using YAML object notation with curly braces `{}`.
This is useful when content applies to multiple deployment types or versions simultaneously.

::::::{tab-set}
:::::{tab-item} Output

::::{applies-switch}

:::{applies-item} { ece: ga 4.0+, ess: ga }
Content for ECE and ECH
:::

:::{applies-item} serverless: ga
Content for Serverless
:::

::::

:::::
:::::{tab-item} Markdown

```markdown
::::{applies-switch}

:::{applies-item} { ece: ga 4.0+, ess: ga }
Content for ECE and ECH
:::

:::{applies-item} serverless: ga
Content for Serverless
:::

::::
```
:::::
::::::

## Automatic grouping

All applies switches on a page automatically sync together. When you select an applies_to definition in one switch, all other switches will switch to the same applies_to definition.

The format of the applies_to definition doesn't matter - `stack: ga 9.1+`, `{ "stack": "ga 9.1+" }`, and `{ stack: "ga 9.1+" }` all identify the same content and will sync together.

In the following example, both switch sets are automatically grouped and will stay in sync.

::::::{tab-set}
:::::{tab-item} Output

::::{applies-switch}
:::{applies-item} { "stack": "ga 9.1+" }
Content for versions 9.1 and newer
:::
:::{applies-item} { "stack": "preview =9.0" }
Content for version 9.0
:::
::::

::::{applies-switch}
:::{applies-item} stack: ga 9.1+
Other content for versions 9.1 and newer
:::
:::{applies-item} stack: preview =9.0
Other content for version 9.0
:::
::::

:::::
:::::{tab-item} Markdown

```markdown
::::{applies-switch}
:::{applies-item} { "stack": "ga 9.1+" }
Content for versions 9.1 and newer
:::
:::{applies-item} { "stack": "preview =9.0" }
Content for version 9.0
:::
::::

::::{applies-switch}
:::{applies-item} stack: ga 9.1+
Other content for versions 9.1 and newer
:::
:::{applies-item} stack: preview =9.0
Other content for version 9.0
:::
::::
```
:::::
::::::

## Dropdown appearance

Add `:appearance: dropdown` to render the switch as a compact dropdown instead of tabs. This works well for version-specific code examples: readers select the version they run, and the code block, including its callouts, updates to match.

Because each `applies-item` contains both the code block and its callout list, the callouts always match the selected version.

The dropdown appearance requires every `applies-item` to start with a code block: the selector chip attaches to the code block's top edge. A switch with other leading content falls back to the tabs appearance and docs-builder emits a warning.

Instead of badges, the selector shows a compact text form of each `applies_to` definition: Elastic Stack entries show only the version (`9.1+`, `=9.0`, `9.1-9.3`), other entries keep their product or deployment name (`Serverless`, `ECH 8.0+`), and the lifecycle appears in parentheses when it is not GA, for example `9.0 (preview)`. Hover the selector to see the full definition.

The appearance only changes the presentation. Switches with the same `applies_to` definitions stay in sync through the same [automatic grouping](#automatic-grouping) regardless of their appearance: selecting a version in a dropdown switch also updates tab switches on the page, and the other way around.

::::::{tab-set}
:::::{tab-item} Output

::::{applies-switch}
:appearance: dropdown

:::{applies-item} { serverless: ga, stack: ga 9.1+ }
:selected:
```console
PUT api/dashboards/dashboard/my-dashboard
{
  "attributes": {
    "title": "My dashboard", <1>
    "panels": [
      {
        "type": "metric",
        "config": {
          "metrics": [ { "field": "system.cpu.usage" } ] <2>
        }
      }
    ]
  }
}
```

1. The dashboard title, displayed in the dashboard listing.
2. In this version, the metric chart accepts multiple metrics.

:::

:::{applies-item} stack: preview =9.0
```console
PUT api/dashboards/dashboard/my-dashboard
{
  "attributes": {
    "title": "My dashboard", <1>
    "panels": [
      {
        "type": "metric",
        "config": {
          "metric": { "field": "system.cpu.usage" } <2>
        }
      }
    ]
  }
}
```

1. The dashboard title, displayed in the dashboard listing.
2. In this version, the metric chart accepts a single metric.

:::

::::

:::::
:::::{tab-item} Markdown

`````markdown
::::{applies-switch}
:appearance: dropdown

:::{applies-item} { serverless: ga, stack: ga 9.1+ }
:selected:
```console
PUT api/dashboards/dashboard/my-dashboard
{
  "attributes": {
    "title": "My dashboard", <1>
    ...
  }
}
```

1. The dashboard title, displayed in the dashboard listing.

:::

:::{applies-item} stack: preview =9.0
```console
PUT api/dashboards/dashboard/my-dashboard
{
  "attributes": { ... }
}
```

1. The dashboard title, displayed in the dashboard listing.

:::

::::
`````
:::::
::::::

## Default selection

By default, the first `applies-item` is selected. Add the `:selected:` option to an item to select a different one, for example to default to the newest version when older versions come first in the source. If multiple items have `:selected:`, only the first one is honored and docs-builder emits a warning.

::::::{tab-set}
:::::{tab-item} Output

::::{applies-switch}
:appearance: dropdown

:::{applies-item} stack: preview =9.0
```console
GET api/dashboards/dashboard
```
:::

:::{applies-item} stack: ga 9.1+
:selected:
```console
GET api/dashboards/dashboard?page=1
```
:::

::::

:::::
:::::{tab-item} Markdown

````markdown
::::{applies-switch}
:appearance: dropdown

:::{applies-item} stack: preview =9.0
```console
GET api/dashboards/dashboard
```
:::

:::{applies-item} stack: ga 9.1+
:selected:
```console
GET api/dashboards/dashboard?page=1
```
:::

::::
````
:::::
::::::

## Supported `applies_to` definitions

The `applies-item` directive accepts any valid applies_to definition that would work with the `{applies_to}` role.

See the [](applies.md) page for more details on valid `applies_to` definitions.

## When to use

Use applies switches when:

- Content varies significantly by deployment type, version, or other applicability criteria
- You want to show applies_to badges as tab titles instead of text
- You need to group related content that differs by applicability
- You want to provide a clear visual indication of what each content section applies to
- You want to offer version-specific code examples without duplicating the surrounding prose: use the dropdown appearance
