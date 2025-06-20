# Applies to

Starting with Elastic Stack 9.0, ECE 4.0, and ECK 3.0, documentation follows a [cumulative approach](../contribute/index.md#cumulative-docs): instead of creating separate pages for each product and release, we update a single page with product- and version-specific details over time.

To support this, source files use a tagging system to indicate:
* Which Elastic products and deployment models the content applies to.
* When a feature changes state relative to the base version.

This is what the `applies_to` metadata is for. It can be used at the page, section, or inline level to specify applicability with precision.

## When and where to use `applies_to`

The `applies_to` metadata can be added at different levels in the documentation:

* [Page-level](#page-annotations) metadata is **mandatory** and must be included in the frontmatter. This defines the overall applicability of the page across products, deployments, and environments.
* [Section-level](#section-annotations) annotations allow you to specify different applicability for individual sections when only part of a page varies between products or versions.
* [Inline](#inline-annotations) annotations allow fine-grained annotations within paragraphs or definition lists. This is useful for highlighting the applicability of specific phrases, sentences, or properties without disrupting the surrounding content.

### Do’s and don’ts

✅ Use `applies_to` tags when features change state (`preview`, `beta`, `ga`, `deprecated`, `removed`) or when availability differs across deployments and environments.

✅ Use `applies_to` tags to indicate which product or deployment type the content applies to. This is mandatory for every page.

✅ Use `applies_to` tags when features change state in a specific update or release.

❌ Don't tag content-only changes like typos, formatting, or documentation updates that don't reflect feature lifecycle changes.

❌ You don’t need to tag every section or paragraph. Only do so if the context or applicability changes from what has been established earlier.

❌ If the product is not versioned (meaning all users are always on the latest version, like in serverless or cloud), you do not need to tag a new GA feature.

## Syntax

The `applies_to` metadata supports an [exhaustive list of keys](#structured-model).

When you write or edit documentation, only specify the keys that apply to that content.
Each key accepts values with the following syntax:

```
<key>: <life-cycle> [version]
```

Where:

- The [life-cycle](#life-cycle) is mandatory
- The [version](#version) is optional
- You can specify multiple states by separating them with a comma. For example: `stack: preview 9.1, ga 9.4`

Note that a key without any value doesn't show any badge in the output.

### Life cycle

`applies_to` accepts the following lifecycle states:

  * `preview`
  * `beta`
  * `deprecated`
  * `removed`
  * `unavailable`
  * `ga`

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

## Examples

### Lifecycle examples

#### Unversioned products
Unversioned products don't follow a fixed versioning scheme and are released a lot more often than versioned products. All users are using the same version of this product.
* When a change is released in `ga`, it **doesn’t need any specific tagging**.
* When a change is introduced as preview or beta, use `preview` or `beta` as value for the corresponding key within the `applies_to`:

    ```
    ---
    applies_to:
      serverless: preview
    ---
    ```
* When a change introduces a deprecation, use deprecated as value for the corresponding key within the `applies_to`:

    ```
    ---
    applies_to:
      deployment:
        ess: deprecated
    ---
    ```

* When a change removes a feature, remove the content. 
**Exception:** If the content also applies to another context (for example a feature is removed in both Kibana 9.x and Serverless), then it must be kept for any user reading the page that may be using a version of Kibana prior to the removal. For example:

    ```
    ---
    applies_to:
      stack: deprecated 9.1, removed 9.4
      serverless: removed
    ---
    ```

#### Versioned products

* When a change is released in `ga`, users need to know which version the feature became available in:

    ```
    ---
    applies_to:
      stack: ga 9.3
    ---
    ```

* When a change is introduced as preview or beta, use `preview` or `beta` as value for the corresponding key within the `applies_to`:

    ```
    ---
    applies_to:
      stack: beta 9.1
    ---
    ```

* When a change introduces a deprecation, use `deprecated` as value for the corresponding key within the `applies_to`:

    ```
    ---
    applies_to:
      deployment:
        ece: deprecated 4.2
    ---
    ```

* When a change removes a feature, any user reading the page that may be using a version of Kibana prior to the removal must be aware that the feature is still available to them. For that reason, we do not remove the content, and instead mark the feature as removed:

    ```
    ---
    applies_to:
      stack: deprecated 9.1, removed 9.4
    ---
    ```

#### Identify multiple states for the same content

A feature is deprecated in ECE 4.0 and is removed in 4.8. At the same time, it has already been removed in Elastic Cloud Hosted:

```
---
applies_to:
  deployment:
    ece: deprecated 4.0, removed 4.8
    ess: removed
---
```

### Page annotations

All documentation pages **must** include an `applies_to` tag in the YAML frontmatter. Use YAML frontmatter to indicate each deployment targets availability and lifecycle status. For a complete list of supported keys and values, see the [frontmatter syntax guide](./frontmatter.md).

#### Page annotation examples

There are 3 typical scenarios to start from:
1. The documentation set or page is primarily about using or interacting with Elastic Stack components or the Serverless UI:

    ```yaml
    --- 
    applies_to:
      stack: ga
      serverless: ga
    products:
      -id: kibana
      -id: elasticsearch
      -id: elastic-stack
    ---
    ```

2. The documentation set or page is primarily about orchestrating, deploying or configuring an installation (only include relevant keys):

    ```yaml
    --- 
    applies_to:
      serverless: ga
      deployment: 
        ess: ga
        ece: ga
        eck: ga
    products:
      -id: cloud-serverless
      -id: cloud-hosted
      -id: cloud-enterprise
      -id: cloud-kubernetes
    ---
    ```

3. The documentation set or page is primarily about a product following its own versioning schema:

    ```yaml
    --- 
    applies_to:
      product: ga
    products:
      -id: edot-collector
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


Note that this directive requires triple backticks since its content is literal. See also [](index.md#literal-directives)

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

This will allow the YAML inside the `{applies_to}` directive to be fully highlighted.

#### Section annotation examples

1. The whole page is generally applicable to Elastic Stack 9.0 and to Serverless, but one specific section isn’t applicable to Serverless (and there is no alternative for it):

    ````markdown
    ## Configure a space-level landing page [space-landing-page]
    ```{applies_to}
    stack: ga
    serverless: unavailable
    ```
    ````

2. The whole page is generally applicable to Elastic Cloud Enterprise and Elastic Cloud Hosted, but one specific paragraph only applies to Elastic Cloud Enterprise, and another paragraph explains the same, but for Elastic Cloud Hosted:

    ````markdown
    ## Secure a deployment [secure-deployment-ech]
    ```{applies_to}
    deployment:
      ess: ga
    ```

    [...]

    ## Secure a deployment [secure-deployment-ece]
    ```{applies_to}
    deployment:
      ece: ga
    ```

    [...]
    ````
3. A specific section, paragraph or list item has specific applicability that differs from the context set at the page or section level, and the action is not possible at all for that context (meaning that there is no alternative). For example: 

    ````markdown
    --- 
    applies_to:
      stack: ga
      serverless: ga
    ---

    # Spaces

    [...]

    ## Configure a space-level landing page [space-landing-page]
    ```{applies_to}
    stack: ga
    serverless: unavailable
    ```
    ````

### Inline annotations

Inline applies to can be placed anywhere using the following syntax

```markdown
This can live inline {applies_to}`section: <life-cycle> [version]`
```

An inline version example would be {applies_to}`stack: beta 9.1` this allows you to target elements more concretely visually.

#### Inline annotation examples

1. The whole page is generally applicable to Elastic Stack 9.0 and to Serverless, but one specific section isn’t applicable to Serverless (and there is no alternative):

    ````markdown
    **Spaces** let you organize your content and users according to your needs.

    - Each space has its own saved objects.
    - {applies_to}`stack: ga` {applies_to}`serverless: unavailable` Each space has its own navigation, called solution view.
    ````

A specialized `{preview}` role exist to quickly mark something as a technical preview. It takes a required version number
as argument.

```markdown
Property {preview}`<version>`
:   definition body
```

## Structured model

![Applies To Model](images/applies.png)

The above model is projected to the following structured YAML.

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