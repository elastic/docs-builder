# Applies to

The `applies_to` metadata allows you to specify which product versions, deployment types, and environments a specific page, section, or line applies to. Documentation published using Elastic Docs V3 follows a [cumulative model](../contribute/index.md) where a single page covers multiple versions cumulatively over time, instead of creating separate pages for each minor release.

## Syntax

```
<life-cycle> [version], <life-cycle> [version]
```

Taking a mandatory [life-cycle](#life-cycle) with an optional version.

### Version

Can be in either `major.minor` or `major.minor.patch` format

Versioned products require a `version` tag to be used with the `lifecycle` tag. See [Syntax](#syntax):

```
applies_to:
  stack: preview 9.1, ga 9.4
  deployment:
    ece: deprecated 9.2, removed 9.8
```
Unversioned products use `lifecycle` tags without a version:

```
applies_to:
  serverless:
    elasticsearch: beta
    observability: removed
```

### Combined states
You can specify multiple lifecycle states for the same product, separated by commas. For example:

```
applies_to:
  stack: preview 9.1, ga 9.4
```
This shows that the feature was introduced in version 9.1 as a preview and became generally available in 9.4.

### Life cycle

`applies_to` accepts the following lifecycle states:

  * `preview`
  * `beta`
  * `deprecated`
  * `removed`
  * `unavailable`
  * `ga`

Both versioned and unversioned products use the same lifecycle tags, but only versioned products can be marked `ga`. Unversioned products are considered `ga` by default and don’t need specification.

## When and where to use `applies_to`

✅ Use `applies_to` tags when features change state (`introduced`, `deprecated`, `removed`) or when availability differs across deployments and environments.

❌ Don't tag content-only changes like typos, formatting, or documentation updates that don't reflect feature lifecycle changes.

The `applies_to` metadata can be added at different levels in the documentation: 

* [Page-level](#page-annotations) metadata is **mandatory** and must be included in the frontmatter. This defines the overall applicability of the page across products, deployments, and environments.
* [Section-level](#section-annotations) annotations allow you to specify different applicability for individual sections when only part of a page varies between products or versions.
* [Inline](#inline-applies-to) annotations allow fine-grained annotations within paragraphs or definition lists. This is useful for highlighting the applicability of specific phrases, sentences, or properties without disrupting the surrounding content.

### Page annotations

All documentation pages **must** include an `applies_to` tag in the YAML frontmatter. Use yaml frontmatter to indicate each deployment targets availability and lifecycle status.

``` yaml
---
applies_to:
  product: preview 9.5, ga 9.6
products:
  - id: cloud-kubernetes
---
```

```yaml
---
applies_to:
  stack: ga 9.1
  deployment:
    eck: ga 9.0
    ess: beta 9.1
    ece: deprecated 9.2.0
    self: unavailable
  serverless:
    security: unavailable
    elasticsearch: beta
    observability: deprecated
  product: preview 9.5, deprecated 9.7
---
```

### Section annotations

```yaml {applies_to}
stack: ga 9.1
deployment:
  eck: ga 9.0
  ess: beta 9.1
  ece: deprecated 9.2.0
  self: unavailable
serverless:
  security: unavailable
  elasticsearch: beta
  observability: deprecated
product: preview 9.5, deprecated 9.7
```

A header may be followed by an `{applies_to}` directive which will contextualize the applicability
of the section further.

:::{note}
the `{applies_to}` directive **MUST** be preceded by a heading directly.
:::


Note that this directive needs triple backticks since its content is literal. See also [](index.md#literal-directives)

````markdown
```{applies_to}
stack: ga 9.1
```
````

In order to play even better with markdown editors the following is also supported:

````markdown
```yaml {applies_to}
stack: ga 9.1
```
````

This will allow the yaml inside the `{applies_to}` directive to be fully highlighted.

### Inline annotations

Inline applies to can be placed anywhere using the following syntax

```markdown
This can live inline {applies_to}`section: <life-cycle> [version]`
```

An inline version example would be {applies_to}`stack: beta 9.1` this allows you to target elements more concretely visually.

A common use case would be to place them on definition lists:

Fruit {applies_to}`stack: preview 9.1`
:   A sweet and fleshy product of a tree or other plant that contains seed and can be eaten as food. Common examples include apples, oranges, and bananas. Most fruits are rich in vitamins, minerals and fiber.

Applies {preview}`9.1`
:   A sweet and fleshy product of a tree or other plant that contains seed and can be eaten as food. Common examples include apples, oranges, and bananas. Most fruits are rich in vitamins, minerals and fiber.


A specialized `{preview}` role exist to quickly mark something as a technical preview. It takes a required version number
as argument.

```markdown
Property {preview}`<version>`
:   definition body
```

## Structured model

![Applies To Model](images/applies.png)

The above model is projected to the following structured yaml.

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
---
```
This allows you to annotate various facets as defined in [](../migration/versioning.md)

## Examples

#### Stack only
```yaml {applies_to}
stack: ga 9.1
```

#### Stack with deployment
```yaml {applies_to}
stack: ga 9.1
deployment:
  eck: ga 9.0
  ess: beta 9.1
```

#### Deployment only
```yaml {applies_to}
deployment:
  ece: deprecated 9.2.0
  self: unavailable
```

#### Serverless only
When a change is released in `ga` for unversioned products, it doesn’t need any specific tagging.

```yaml {applies_to}
  serverless:
    elasticsearch: preview
```

#### Serverless with project differences
```yaml {applies_to}
serverless:
  security: unavailable
  elasticsearch: beta
  observability: deprecated
```
#### Stack with product
```yaml {applies_to}
stack: ga 9.1
```