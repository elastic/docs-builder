# Applies Switch

The applies-switch directive creates tabbed content where each tab displays an applies_to badge instead of a text title. This is useful for showing content that varies by deployment type, version, or other applicability criteria.

## Basic Usage

### Example

#### Syntax

```markdown
:::::{applies-switch}

::::{applies-item} stack: preview 9.1
:::{tip}
This feature is in preview for Elastic Stack 9.1.
:::
::::

::::{applies-item} ess: preview 9.1
:::{note}
This feature is available for Elastic Cloud.
:::
::::

::::{applies-item} ece: removed
:::{warning}
This feature has been removed from Elastic Cloud Enterprise.
:::
::::

:::::
```

#### Result

:::::{applies-switch}

::::{applies-item} stack: preview 9.1
:::{tip}
This feature is in preview for Elastic Stack 9.1.
:::
::::

::::{applies-item} ess: preview 9.1
:::{note}
This feature is available for Elastic Cloud.
:::
::::

::::{applies-item} ece: removed
:::{warning}
This feature has been removed from Elastic Cloud Enterprise.
:::
::::

:::::

## Automatic Grouping

Applies switches are automatically grouped together by default. This means all applies switches on a page will sync with each other - when you select a version in one switch, all other switches will automatically switch to the same version.

Items with the same applies_to definition will sync together across all switches on the page. For example, if you have `stack: preview 9.1` in multiple switches, selecting it in one will select it in all others.

### Example

In the following example we have two applies switch sets that are automatically grouped together.
Hence, both switch sets will be in sync.

#### Syntax

```markdown
::::{applies-switch}
:::{applies-item} stack: ga 8.11
Content for GA version
:::

:::{applies-item} stack: preview 9.1
Content for preview version
:::

::::

::::{applies-switch}
:::{applies-item} stack: ga 8.11
Other content for GA version
:::

:::{applies-item} stack: preview 9.1
Other content for preview version
:::

::::
```

#### Result

##### Automatically Grouped Applies Switches

::::{applies-switch}
:::{applies-item} stack: ga 8.11
Content for GA version
:::

:::{applies-item} stack: preview 9.1
Content for preview version
:::

::::

::::{applies-switch}
:::{applies-item} stack: ga 8.11
Other content for GA version
:::

:::{applies-item} stack: preview 9.1
Other content for preview version
:::

::::

## Supported Applies To Definitions

The `applies-item` directive accepts any valid applies_to definition that would work with the `{applies_to}` role. This includes:

- **Stack versions**: `stack: ga 8.11`, `stack: preview 9.1`
- **Deployment types**: `ess: preview 9.1`, `ece: removed`, `eck: ga 8.11`
- **Product versions**: `product: preview 9.1`
- **Serverless projects**: `serverless: observability: preview 9.1`

## Best Practices

**DOs**<br>
✅ **Do:** Use clear, descriptive applies_to definitions<br>
✅ **Do:** Make sure all switch items have the same type of content and similar goals<br>
✅ **Do:** Keep switch content scannable and self-contained<br>
✅ **Do:** Include other block elements in switches, like [admonitions](admonitions.md)<br>
✅ **Do:** Use applies_to definitions that are meaningful to your users

**DON'Ts**<br>
❌ **Don't:** Nest applies switches<br>
❌ **Don't:** Split step-by-step procedures across switches<br>
❌ **Don't:** Use more than 6 switch items (use as few as possible)<br>
❌ **Don't:** Use applies switches in [dropdowns](dropdowns.md)<br>
❌ **Don't:** Use applies_to definitions that are too similar or confusing

## When to Use

Use applies switches when:

- Content varies significantly by deployment type, version, or other applicability criteria
- You want to show applies_to badges as tab titles instead of text
- You need to group related content that differs by applicability
- You want to provide a clear visual indication of what each content section applies to

## Comparison with Regular Tabs

| Feature | Regular Tabs | Applies Switch |
|---------|--------------|----------------|
| Tab titles | Text labels | Applies_to badges |
| Use case | General content organization | Content that varies by applicability |
| Visual indication | Text | Badge with version/deployment info |
| Best for | General content grouping | Version-specific or deployment-specific content |
