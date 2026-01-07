# Applies to

Starting with Elastic Stack 9.0, ECE 4.0, and ECK 3.0, documentation follows a [cumulative approach](https://www.elastic.co/docs/contribute-docs/how-to/cumulative-docs): instead of creating separate pages for each product and release, we update a single page with product- and version-specific details over time.

To support this, source files use a tagging system to indicate:

* Which Elastic products and deployment models the content applies to.
* When a feature changes state relative to the base version.

This is what the `applies_to` metadata is for. It can be used at the [page](#page-level), [section](#section-level), or [inline](#inline-level) level to specify applicability with precision.

:::{note}
For detailed guidance, refer to [Write cumulative documentation](https://www.elastic.co/docs/contribute-docs/how-to/cumulative-docs).
:::

## Syntax reference

The `applies_to` metadata supports an [exhaustive list of keys](#key-value-reference). When you write or edit documentation, only specify the keys that apply to that content.

Each key accepts values with the following syntax:

```
<key>: <lifecycle> [version], <lifecycle> [version], ...
```

Where:

- The lifecycle is mandatory.
- The version is optional.

### Page level

Page level annotations are added in the YAML frontmatter, starting with the `applies_to` key and following the [key-value reference](#key-value-reference). For example:

```yaml
---
applies_to:
  stack: ga
  deployment:
    ece: ga
---
```

:::{important}
All documentation pages must include an `applies_to` tag in the YAML frontmatter.
:::

### Section level

A header can be followed by an `{applies_to}` directive which contextualizes the applicability of the section further.

Section-level `{applies_to}` directives require triple backticks because their content is literal. Refer to [](directives.md#exception-literal-blocks) for more information.

````markdown
```{applies_to}
stack: ga 9.1
```
````

To play even better with Markdown editors the following is also supported:

````markdown
```yaml {applies_to}
stack: ga 9.1
```
````

This allows the YAML inside the `{applies_to}` directive to be fully highlighted.

:::{note}
Section-level `{applies_to}` directives must be preceded by a heading directly.
:::

### Inline level

You can add inline applies annotations to any line using the following syntax:

```markdown
This can live inline {applies_to}`section: <life-cycle> [version]`
```

A specialized `{preview}` role exists to quickly mark something as a technical preview. It takes a required version number as an argument.

```markdown
Property {preview}`<version>`
:   definition body
```

### On specific components

Several components have built-in support for `applies_to` and allow to surface version information in an optimized way:

- [applies-switch](applies-switch.md), a component similar to tabs but with specific support to show version badges as tab titles
- [admonitions](admonitions.md)
- [dropdowns](dropdowns.md)

Refer to these component pages to learn about the required `applies_to` syntax.

## Version syntax

Versions can be specified using several formats to indicate different applicability scenarios.

### Formats

| Format | Syntax | Example | Badge Display | Description |
|:-------|:-------|:--------|:--------------|:------------|
| **Greater than or equal to** (default) | `x.x+` `x.x` `x.x.x+` `x.x.x` | `ga 9.1` or `ga 9.1+` | `9.1+` | Applies from this version onwards |
| **Range** (inclusive) | `x.x-y.y` `x.x.x-y.y.y` | `preview 9.0-9.2` | `9.0-9.2` or `9.0+`* | Applies within the specified range |
| **Exact version** | `=x.x` `=x.x.x` | `beta =9.1` | `9.1` | Applies only to this specific version |

\* Range display depends on release status of the second version.

**Important notes:**

- Versions are always displayed as **Major.Minor** (e.g., `9.1`) in badges, regardless of whether you specify patch versions in the source.
- Each version statement corresponds to the **latest patch** of the specified minor version (e.g., `9.1` represents 9.1.0, 9.1.1, 9.1.6, etc.).
- When critical patch-level differences exist, use plain text descriptions alongside the badge rather than specifying patch versions.

### Rendered examples

The following table shows how different version syntaxes render:

| Rendered | Raw input | Notes |
|----------|-----------|-------|
| {applies_to}`stack: ga 9.1` | `` {applies_to}`stack: ga 9.1` `` | Implicit `+` (default behavior) |
| {applies_to}`stack: ga 9.1+` | `` {applies_to}`stack: ga 9.1+` `` | Explicit `+` |
| {applies_to}`stack: preview 9.0+` | `` {applies_to}`stack: preview 9.0+` `` | Preview with version |
| {applies_to}`stack: preview 9.0-9.2` | `` {applies_to}`stack: preview 9.0-9.2` `` | Range display when both ends are released |
| {applies_to}`stack: beta 9.1-9.3` | `` {applies_to}`stack: beta 9.1-9.3` `` | Converts to `+` if the end version is unreleased |
| {applies_to}`stack: beta =9.1` | `` {applies_to}`stack: beta =9.1` `` | Exact version (no `+` symbol) |
| {applies_to}`stack: deprecated =9.0` | `` {applies_to}`stack: deprecated =9.0` `` | Deprecated exact version |
| {applies_to}`stack: ga 9.2+, beta 9.0-9.1` | `` {applies_to}`stack: ga 9.2+, beta 9.0-9.1` `` | Multiple lifecycles (highest priority shown) |
| {applies_to}`stack: ga 9.3, beta 9.1+` | `` {applies_to}`stack: ga 9.3, beta 9.1+` `` | Shows Beta when GA is unreleased (2+ lifecycles) |
| {applies_to}`serverless: ga` | `` {applies_to}`serverless: ga` `` | No version badge for unversioned products |

### Validation rules

The build process enforces the following validation rules:

- **One version per lifecycle**: Each lifecycle (GA, Preview, Beta, etc.) can only have one version declaration.
  - ✅ `stack: ga 9.2+, beta 9.0-9.1`
  - ❌ `stack: ga 9.2, ga 9.3`
- **One "greater than" per key**: Only one lifecycle per product key can use the `+` (greater than or equal to) syntax.
  - ✅ `stack: ga 9.2+, beta 9.0-9.1`
  - ❌ `stack: ga 9.2+, beta 9.0+`
- **Valid range order**: In ranges, the first version must be less than or equal to the second version.
  - ✅ `stack: preview 9.0-9.2`
  - ❌ `stack: preview 9.2-9.0`
- **No version overlaps**: Versions for the same key cannot overlap (ranges are inclusive).
  - ✅ `stack: ga 9.2+, beta 9.0-9.1`
  - ❌ `stack: ga 9.2+, beta 9.0-9.2`

## Key-value reference

Use the following key-value reference to find the appropriate key and value for your applicability statements.

:::::{tab-set}

::::{tab-item} Keys

:::{include} /_snippets/applies_to-key.md
:::

::::

::::{tab-item} Lifecycles

:::{include} /_snippets/applies_to-lifecycle.md
:::

::::

::::{tab-item} Versions

:::{include} /_snippets/applies_to-version.md
:::

::::
:::::

## Examples

### By scope

:::::{tab-set}

::::{tab-item} Page level

:::{include} _snippets/page-level-applies-examples.md
:::

::::

::::{tab-item} Section level

:::{include} _snippets/section-level-applies-examples.md
:::

::::

::::{tab-item} Inline level

:::{include} _snippets/inline-level-applies-examples.md
:::

::::

:::::

### Versioned vs unversioned products

:::::{tab-set}

::::{tab-item} Versioned products

Versioned products require a `version` tag to be used with the `lifecycle` tag:

```yaml
applies_to:
  stack: preview 9.1, ga 9.4
  deployment:
    ece: deprecated 9.2, removed 9.8
```

:::{include} _snippets/versioned-lifecycle.md
:::

::::

::::{tab-item} Unversioned products

Unversioned products use `lifecycle` tags without a version:

```yaml
applies_to:
  serverless:
    elasticsearch: beta
    observability: removed
```

:::{include} _snippets/unversioned-lifecycle.md
:::

::::

::::{tab-item} Multiple lifecycle states

:::{include} _snippets/multiple-lifecycle-states.md
:::

::::

:::::

### Inline examples by product

:::::{tab-set}

::::{tab-item} Stack

| `applies_to` | Result |
|--------------|--------|
| `` {applies_to}`stack: ` `` | {applies_to}`stack: ` |
| `` {applies_to}`stack: preview` `` | {applies_to}`stack: preview` |
| `` {applies_to}`stack: preview 8.18` `` | {applies_to}`stack: preview 8.18` |
| `` {applies_to}`stack: preview 9.0` `` | {applies_to}`stack: preview 9.0` |
| `` {applies_to}`stack: preview 9.1` `` | {applies_to}`stack: preview 9.1` |
| `` {applies_to}`stack: ga` `` | {applies_to}`stack: ga` |
| `` {applies_to}`stack: ga 8.18` `` | {applies_to}`stack: ga 8.18` |
| `` {applies_to}`stack: ga 9.0` `` | {applies_to}`stack: ga 9.0` |
| `` {applies_to}`stack: ga 9.1` `` | {applies_to}`stack: ga 9.1` |
| `` {applies_to}`stack: beta` `` | {applies_to}`stack: beta` |
| `` {applies_to}`stack: beta 9.0` `` | {applies_to}`stack: beta 9.0` |
| `` {applies_to}`stack: deprecated` `` | {applies_to}`stack: deprecated` |
| `` {applies_to}`stack: deprecated 9.0` `` | {applies_to}`stack: deprecated 9.0` |
| `` {applies_to}`stack: removed` `` | {applies_to}`stack: removed` |
| `` {applies_to}`stack: removed 9.0` `` | {applies_to}`stack: removed 9.0` |

::::

::::{tab-item} Serverless

| `applies_to` | Result |
|--------------|--------|
| `` {applies_to}`serverless: ` `` | {applies_to}`serverless: ` |
| `` {applies_to}`serverless: preview` `` | {applies_to}`serverless: preview` |
| `` {applies_to}`serverless: ga` `` | {applies_to}`serverless: ga` |
| `` {applies_to}`serverless: beta` `` | {applies_to}`serverless: beta` |
| `` {applies_to}`serverless: deprecated` `` | {applies_to}`serverless: deprecated` |
| `` {applies_to}`serverless: removed` `` | {applies_to}`serverless: removed` |

::::

:::::

### Block example

```{applies_to}
stack: preview 9.1+
serverless: ga

apm_agent_dotnet: ga 1.0+
apm_agent_java: beta 1.0+
edot_dotnet: preview 1.0+
edot_python:
edot_node: ga 1.0+
elasticsearch: preview 9.0+
security: removed 9.0
observability: deprecated 9.0+
```

### In-text example

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas ut libero diam. Mauris sed eleifend erat, sit amet auctor odio. Donec ac placerat nunc. {applies_to}`stack: preview` Aenean scelerisque viverra lectus nec dignissim.

- {applies_to}`elasticsearch: preview` Lorem ipsum dolor sit amet, consectetur adipiscing elit.
- {applies_to}`observability: preview` Lorem ipsum dolor sit amet, consectetur adipiscing elit.
- {applies_to}`security: preview` Lorem ipsum dolor sit amet, consectetur adipiscing elit.

## Structured model

![Applies To Model](images/applies.png)

The previous model is projected to the following structured YAML.

:::::{dropdown} Applies to model

```yaml
---
applies_to:
  stack:
  deployment:
    eck:
    ess:
    ece:
    self:
  serverless:
    security:
    elasticsearch:
    observability:
  product:
    ecctl:
    curator:
    apm_agent_dotnet:
    apm_agent_go:
    apm_agent_java:
    apm_agent_node:
    apm_agent_php:
    apm_agent_python:
    apm_agent_ruby:
    apm_agent_rum:
    edot_collector:
    edot_ios:
    edot_android:
    edot_dotnet:
    edot_java:
    edot_node:
    edot_php:
    edot_python:
    edot_cf_aws:
    edot_cf_azure:
---
```
:::::

## Badge rendering reference

This section provides detailed rules for how badges are rendered based on lifecycle, version, and release status. Use this as a reference when you need to understand the exact rendering behavior.

### Rendering order

`applies_to` badges are displayed in a consistent order regardless of how they appear in your source files:

1. **Stack** - Elastic Stack
2. **Serverless** - Elastic Cloud Serverless offerings
3. **Deployment** - Deployment options (ECH, ECK, ECE, Self-managed)
4. **ProductApplicability** - Specialized tools and agents (ECCTL, Curator, EDOT, APM Agents)
5. **Product (generic)** - Generic product applicability

Within the ProductApplicability category, EDOT and APM Agent items are sorted alphabetically for better scanning.

:::{note}
Inline applies annotations are rendered in the order they appear in the source file.
:::

### Badge rendering rules

:::::{dropdown} No version declared (Serverless)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | – | – | `{product}` |
| Preview | – | – | `{product}\|Preview` |
| Beta | – | – | `{product}\|Beta` |
| Deprecated | – | – | `{product}\|Deprecated` |
| Removed | – | – | `{product}\|Removed` |
| Unavailable | – | – | `{product}\|Unavailable` |

:::::

:::::{dropdown} No version declared (Other versioning systems)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | – | – | `{product}\|{base}+` |
| Preview | – | – | `{product}\|Preview {base}+` |
| Beta | – | – | `{product}\|Beta {base}+` |
| Deprecated | – | – | `{product}\|Deprecated {base}+` |
| Removed | – | – | `{product}\|Removed {base}+` |
| Unavailable | – | – | `{product}\|Unavailable {base}+` |

:::::

:::::{dropdown} Greater than or equal to (x.x+, x.x, x.x.x+, x.x.x)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | Released | \>= 1 | `{product}\|x.x+` |
| | Unreleased | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Preview | Released | \>= 1 | `{product}\|Preview x.x+` |
| | Unreleased | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Beta | Released | \>= 1 | `{product}\|Beta x.x+` |
| | Unreleased | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Deprecated | Released | \>= 1 | `{product}\|Deprecated x.x+` |
| | Unreleased | 1 | `{product}\|Deprecation planned` |
| | | \>= 2 | Use previous lifecycle |
| Removed | Released | \>= 1 | `{product}\|Removed x.x` |
| | Unreleased | 1 | `{product}\|Removal planned` |
| | | \>= 2 | Use previous lifecycle |

:::::

:::::{dropdown} Range (x.x-y.y, x.x.x-y.y.y)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | `y.y.y` is released | \>= 1 | `{product}\|x.x-y.y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `{product}\|x.x+` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Preview | `y.y.y` is released | \>= 1 | `{product}\|Preview x.x-y.y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `{product}\|Preview x.x+` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Beta | `y.y.y` is released | \>= 1 | `{product}\|Beta x.x-y.y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `{product}\|Beta x.x+` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Deprecated | `y.y.y` is released | \>= 1 | `{product}\|Deprecated x.x-y.y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `{product}\|Deprecated x.x+` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | \>= 1 | `{product}\|Deprecation planned` |
| Removed | `y.y.y` is released | \>= 1 | `{product}\|Removed x.x` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `{product}\|Removed x.x` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | \>= 1 | `{product}\|Removal planned` |
| Unavailable | `y.y.y` is released | \>= 1 | `{product}\|Unavailable X.X-Y.Y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `{product}\|Unavailable X.X+` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | \>= 1 | ??? |

:::::

:::::{dropdown} Exact version (=x.x, =x.x.x)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | Released | \>= 1 | `{product}\|X.X` |
| | Unreleased | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Preview | Released | \>= 1 | `{product}\|Preview X.X` |
| | Unreleased | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Beta | Released | \>= 1 | `{product}\|Beta X.X` |
| | Unreleased | 1 | `{product}\|Planned` |
| | | \>= 2 | Use previous lifecycle |
| Deprecated | Released | \>= 1 | `{product}\|Deprecated X.X` |
| | Unreleased | \>= 1 | `{product}\|Deprecation planned` |
| Removed | Released | \>= 1 | `{product}\|Removed X.X` |
| | Unreleased | \>=1 | `{product}\|Removal planned` |
| Unavailable | Released | \>= 1 | `{product}\|Unavailable X.X` |
| | Unreleased | \>= 1 | ??? |

:::::

### Popover availability text

:::::{dropdown} No version declared (Serverless)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | – | 1 | `Generally available` |
| Preview | – | 1 | `Preview` |
| Beta | – | 1 | `Beta` |
| Deprecated | – | 1 | `Deprecated` |
| Removed | – | 1 | `Removed` |
| Unavailable | – | 1 | `Unavailable` |

:::::

:::::{dropdown} No version declared (Other versioning systems)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | – | 1 | `Generally available since {base}` |
| Preview | – | 1 | `Preview since {base}` |
| Beta | – | 1 | `Beta since {base}` |
| Deprecated | – | 1 | `Deprecated since {base}` |
| Removed | – | 1 | `Removed in {base}` |
| Unavailable | – | 1 | `Unavailable since {base}` |

:::::

:::::{dropdown} Greater than or equal to (x.x+, x.x, x.x.x+, x.x.x)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | Released | \>= 1 | `Generally available since X.X` |
| | Unreleased | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Preview | Released | \>= 1 | `Preview since X.X` |
| | Unreleased | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Beta | Released | \>= 1 | `Beta since X.X` |
| | Unreleased | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Deprecated | Released | \>= 1 | `Deprecated since X.X` |
| | Unreleased | \>= 1 | `Planned for deprecation` |
| Removed | Released | \>= 1 | `Removed in X.X` |
| | Unreleased | \>=1 | `Planned for removal` |
| Unavailable | Released | \>= 1 | `Unavailable since X.X` |
| | Unreleased | 1 | `Unavailable` |
| | | \>= 2 | Do not add to availability list |

:::::

:::::{dropdown} Range (x.x-y.y, x.x.x-y.y.y)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | `y.y.y` is released | \>= 1 | `Generally available from X.X to Y.Y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `Generally available since X.X` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Preview | `y.y.y` is released | \>= 1 | `Preview from X.X to Y.Y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `Preview since X.X` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Beta | `y.y.y` is released | \>= 1 | `Beta from X.X to Y.Y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `Beta since X.X` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Deprecated | `y.y.y` is released | \>= 1 | `Deprecated from X.X to Y.Y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `Deprecated since X.X` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | \>= 1 | `Planned for deprecation` |
| Removed | `y.y.y` is released | \>= 1 | `Removed in X.X` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `Removed in X.X` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | \>= 1 | `Planned for removal` |
| Unavailable | `y.y.y` is released | \>= 1 | `Unavailable from X.X to Y.Y` |
| | `y.y.y` is **not** released, `x.x.x` is released | \>= 1 | `Unavailable since X.X` |
| | `y.y.y` is **not** released, `x.x.x` is **not** released | \>= 1 | Do not add to availability list |

:::::

:::::{dropdown} Exact version (=x.x, =x.x.x)

| Lifecycle | Release status | Lifecycle count | Rendered output |
|:----------|:---------------|-----------------|:----------------|
| GA | Released | \>= 1 | `Generally available in X.X` |
| | Unreleased | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Preview | Released | \>= 1 | `Preview in X.X` |
| | Unreleased | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Beta | Released | \>= 1 | `Beta in X.X` |
| | Unreleased | 1 | `Planned` |
| | | \>= 2 | Do not add to availability list |
| Deprecated | Released | \>= 1 | `Deprecated in X.X` |
| | Unreleased | \>= 1 | `Planned for deprecation` |
| Removed | Released | \>= 1 | `Removed in X.X` |
| | Unreleased | \>=1 | `Planned for removal` |
| Unavailable | Released | \>= 1 | `Unavailable in X.X` |
| | Unreleased | \>= 1 | Do not add to availability list |

:::::
