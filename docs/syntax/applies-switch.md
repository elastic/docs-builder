# Applies switch

The applies-switch directive creates tabbed content where each tab displays an applies_to badge instead of a text title. This is useful for showing content that varies by deployment type, version, or other applicability criteria.

## Basic usage

::::::{tab-set}
:::::{tab-item} Output

::::{applies-switch}

:::{applies-item} stack:
Content for Stack
:::

:::{applies-item} serverless:
Content for Serverless
:::

::::

:::::
:::::{tab-item} Markdown

```markdown
::::{applies-switch}

:::{applies-item} stack:
Content for Stack
:::

:::{applies-item} serverless:
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

:::{applies-item} { ece:, ess: }
Content for ECE and ECH bla
:::

:::{applies-item} serverless:
Content for Serverless
:::

::::

:::::
:::::{tab-item} Markdown

```markdown
::::{applies-switch}

:::{applies-item} { ece:, ess: }
Content for ECE and ECH bla
:::

:::{applies-item} serverless:
Content for Serverless
:::

::::
```
:::::
::::::

## Automatic grouping

Applies switches are automatically grouped together by default. This means all applies switches on a page will sync with each other - when you select a version in one switch, all other switches will automatically switch to the same version.

Items with the same applies_to definition will sync together across all switches on the page. For example, if you have `stack: preview 9.1` in multiple switches, selecting it in one will select it in all others.

In the following example we have two applies switch sets that are automatically grouped together.
Hence, both switch sets will be in sync.

::::{applies-switch}
:::{applies-item} stack: ga 9.0
Other content for 9.0 version
:::
:::{applies-item} stack: ga 9.1
Other content for 9.1 version
:::
::::

::::{applies-switch}
:::{applies-item} stack: ga 9.0
Other content for 9.0 version
:::
:::{applies-item} stack: ga 9.1
Other content for 9.1 version
:::
::::

## Supported `applies_to` definitions

The `applies-item` directive accepts any valid applies_to definition that would work with the `{applies_to}` role.

See the [](applies.md) page for more details on valid `applies_to` definitions.

## When to use

Use applies switches when:

- Content varies significantly by deployment type, version, or other applicability criteria
- You want to show applies_to badges as tab titles instead of text
- You need to group related content that differs by applicability
- You want to provide a clear visual indication of what each content section applies to
