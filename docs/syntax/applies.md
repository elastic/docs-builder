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

# Applies to

Allows you to annotate a page or section's applicability. The documentation follows a cumulative model: changes across versions are shown on a single page. Use the `applies_to` tag to reflect a feature’s state across versions. For more on the versioning approach, see [Contribution guide](../contribute/index.md).

### Syntax

```
<life-cycle> [version], <life-cycle> [version]
```

Taking a mandatory [life-cycle](#life-cycle) with an optional version.

#### Life cycle

Both versioned and unversioned products use the same lifecycle tags, but only versioned products can be marked `ga`. Unversioned products are considered `ga` by default and don’t need specification.

  * `preview`
  * `beta`
  * `deprecated`
  * `removed`
  * `unavailable`
  * `ga`

#### Version

Can be in either `major.minor` or `major.minor.patch` format

#### Examples

```
preview 9.5, ga 9.7
deprecated 9.9.0
unavailable
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

## Page annotations

All documentation pages **must** include an `applies_to` tag in the YAML frontmatter. Using yaml frontmatter pages can explicitly indicate to each deployment targets availability and lifecycle status.

``` yaml
applies_to:
  product: preview 9.5
products:
  -id: cloud-kubernetes
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

## Section annotation [#sections]

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

## Inline Applies To

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

## Tagging feature lifecycle and version changes

Elastic documentation follows a cumulative model: a single page shows the current state of a feature across versions and deployments. To support this, **version-related changes must be tagged using the `applies_to` key**.

Use the applies_to tag when:
* A feature is introduced (e.g., preview, beta, or ga)
* A feature is deprecated (e.g., deprecated)
* A feature is removed (e.g., removed)

You don’t need version tagging for:
* Typos, formatting, or style changes
* Long-standing features being documented for the first time
* Content updates that don’t reflect a feature lifecycle change

### Versioned vs. unversioned products

Versioned products require a lifecycle tag with a version:

```
applies_to:
  stack: preview 9.1, ga 9.4
  deployment:
    ece: deprecated 9.2, removed 9.8
```
Unversioned products use lifecycle tags without a version:

```
applies_to:
  serverless:
    elasticsearch: beta
    observability: removed
  deployment:
    ess: deprecated
```

### Combined states
You can specify multiple lifecycle states for the same product, separated by commas. For example:

```
applies_to:
  stack: preview 9.1, ga 9.4
```

This shows that the feature was introduced in version 9.1 as a preview and became generally available in 9.4.



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
