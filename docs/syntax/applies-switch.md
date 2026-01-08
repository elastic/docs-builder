# Applies switch

The applies-switch directive creates tabbed content where each tab displays an applies_to badge instead of a text title. This is useful for showing content that varies by deployment type, version, or other applicability criteria.

## Basic usage

::::::{tab-set}
:::::{tab-item} Output

::::{applies-switch}

:::{applies-item} stack: ga 9.0+
Content for Stack
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

:::{applies-item} { ece: ga, ess: ga }
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

:::{applies-item} { ece: ga, ess: ga }
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

## Supported `applies_to` definitions

The `applies-item` directive accepts any valid applies_to definition that would work with the `{applies_to}` role.

See the [](applies.md) page for more details on valid `applies_to` definitions.

## When to use

Use applies switches when:

- Content varies significantly by deployment type, version, or other applicability criteria
- You want to show applies_to badges as tab titles instead of text
- You need to group related content that differs by applicability
- You want to provide a clear visual indication of what each content section applies to
