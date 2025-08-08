---
navigation_title: Guidelines
---

# Cumulative docs guidelines

Start by asking yourself:

* Does this content vary between products, versions, or deployment types?
* Is this a feature lifecycle change or just content improvement?
* Will users benefit from knowing this information?

If the answer to at least one of these questions is _yes_, follow these guidelines to write cumulative documentation.

## Dimensions of applicability

### Type

In cumulative documentation, you can use `applies_to` to communicate:

* **Product- or deployment-specific availability**: When content applies to or functions differently between products or deployment types (for example, Elastic Cloud Serverless or Elastic Cloud Hosted). Read more in [Product and deployment model tags](#products-and-deployment-models).
* **Feature lifecycle and version-related functionality**: When features are introduced, modified, or removed in specific versions including lifecycle changes (for example, going from Beta to GA). Read more in [Tagging version-related changes](#versions).

Both types of applicability are added as part of the same `applies_to` tagging logic.
The type of applicability is the [keys](/contribute/cumulative-docs/reference.md#key)
and the [feature lifecycle](/contribute/cumulative-docs/reference.md#lifecycle)
and [version](/contribute/cumulative-docs/reference.md#version) are make up the value.

```
<key>: <lifecycle> <version>
```

### Level

For each type of applicability information, you can add `applies_to` metadata at different levels:

* **Page-level** metadata is **mandatory** and must be included in the frontmatter.
  This defines the overall applicability of the page across products, deployments, and environments.
* **Section-level** annotations allow you to specify different applicability for individual sections
  when only part of a page varies between products or versions.
% * **Element-level** annotations allow tagging block-level elements like tabs, dropdowns, and admonitions.
%  This is useful for ...
* **Inline** annotations allow fine-grained annotations within paragraphs or definition lists.
  This is useful for highlighting the applicability of specific phrases, sentences,
  or properties without disrupting the surrounding content.

## General guidelines

% Reference: Slack conversation
* The `applies_to` badges should not require contributors to add specific Markdown real estate
  to the page layout (such as a specific `Version` column in a table).
  Instead, contributors should be able to add them anywhere they need, and the system should
  be in charge of rendering them clearly.

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231876006
* Avoid using version numbers in prose adjacent to `applies_to` badge to prevent
  confusion when the badge is rended with `Planned` ahead of a release.

% Reference: https://elastic.github.io/docs-builder/versions/#defaults-and-hierarchy
* Do not assume a default deployment type, stack flavor, product version, or project type.
  Treat all flavors and deployment types equally. Don't treat one as the "base" and the other as the "exception".

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231850710
% * Create hierarchy of versioned information??

% TO DO: Open an issue to force the order in the code.
## Order of items

**Versions.** Always put the newest version first when listing multiple versions. As a result, the lifecycles should be in reverse order of product development progression, too.

% TO DO: Add example / image
% <image>

% Reference: https://elastic.github.io/docs-builder/versions/#defaults-and-hierarchy
% Needs work...
**Keys.** Always list [keys](/contribute/cumulative-docs/reference.md#key) in the same order for consistency. The order of keys should reflect organizational priorities. For example, use the following order:
* **Serverless/Elastic Stack**: Serverless, Stack
* **Deployment types**: Elastic Cloud Serverless, Elastic Cloud Hosted, Elastic Cloud on Kubernetes, Elastic Cloud Enterprise, Self-managed
* **Monitoring for Java applications**: Elastic Distribution of OpenTelemetry (EDOT) Java, APM Java agent

% TO DO: Add example / image
% <image>

## Product and deployment model tags [products-and-deployment-models]

For the full list of supported `applies_to` keys, refer to [](/contribute/cumulative-docs/reference.md#key).

### Guidelines [products-and-deployment-models-guidelines]

* **Always include page-level product and deployment model applicability information**.
  This is _mandatory_ for all pages.
* **Determine if section or inline applicability information is necessary.**
  This _depends on the situation_.
  * For example, if a portion of a page is applicable to a different context than what was specified at the page level,
  clarify in what context it applies using section or inline `applies_to` badges.

### Example scenarios [products-and-deployment-models-examples]

* Content is primarily about both Elastic Stack components and the Serverless UI ([example](/contribute/cumulative-docs/content-patterns.md#stateful-serverless)).
* Content is primarily about orchestrating, deploying or configuring an installation ([example](/contribute/cumulative-docs/content-patterns.md#stateful-serverless)).
* Content is primarily about a product following its own versioning schema ([example]()).
* A whole page is generally applicable to Elastic Stack 9.0 and to Serverless,
  but one specific section isn’t applicable to Serverless ([example]()).
* The whole page is generally applicable to all deployment types,
  but one specific paragraph only applies to Elastic Cloud Hosted and Serverless,
  and another paragraph only applies to Elastic Cloud Enterprise ([example]()).
* Likewise, when the difference is specific to just one paragraph or list item, the same rules apply.
  Just the syntax slightly differs so that it stays inline ([example]()).

% :::{include} /syntax/_snippets/page-level-applies-examples.md
% :::

% :::{tip}
% Docs V3 frontmatter also supports a `products` attribute. This attribute is not surfaced to users on docs pages. Instead, it's used by the elastic.co search to let users filter their docs search results.
% :::

% Use `applies_to` in the YAML frontmatter to indicate each deployment target's availability and lifecycle status.
% The `applies_to` attribute is used to display contextual badges on each page.
% For the full list of supported keys and values, refer to [](/contribute/cumulative-docs/reference.md#key).

% :::{include} /syntax/_snippets/section-level-applies-examples.md
% :::
%
% * Likewise, when the difference is specific to just one paragraph or list item, the same rules apply. Just the syntax slightly differs so that it stays inline:
%
%  :::{include} /syntax/_snippets/line-level-applies-example.md
%  :::

## Version-related changes [versions]

### Guidelines [versions-guidelines]

* **Ensure your change is related to a specific version.**
  Even though a change is made when a specific version is the latest version,
  it does not mean the added or updated content only applies to that version.
  * For example, you should not use version tagging when fixing typos,
    improving styling, or adding a long-forgotten setting.

### Examples [versions-examples]

* **A new feature is added to {{serverless-short}} or {{ecloud}}. How do I tag it?**
  Cumulative documentation is not meant to replace release notes. If a feature becomes available in {{serverless-short}} and doesn’t have a particular lifecycle state to call out (preview, beta, deprecated…), it does not need specific tagging.

  However, in this scenario, it is important to consider carefully [when the change is going to be published](/contribute/branching-strategy.md).

We do not do date-based tagging for unversioned products.

% ### For unversioned products (typically {{serverless-short}} and {{ech}})
%
% :::{include} /syntax/_snippets/unversioned-lifecycle.md
% :::
%
% ### For versioned products
%
% :::{include} /syntax/_snippets/versioned-lifecycle.md
% :::
%
% ### Document features shared between serverless and {{stack}}
%
% :::{include} /syntax/_snippets/stack-serverless-lifecycle-example.md
% :::
%
% ### Identify multiple states for the same content
%
% :::{include} /syntax/_snippets/multiple-lifecycle-states.md
% :::

## When to indicate something is NOT applicable

By default, we communicate that content does not apply to a certain context by simply **not specifying it**.
For example, a page describing how to create an {{ech}} deployment just requires identifying "{{ech}}" as context. No need to overload the context with additional `serverless: unavailable` indicators.

This is true for most situations. However, it can still be useful to call it out in a few specific scenarios:

* When there is a high risk of confusion for users. This may be subjective, but let’s imagine a scenario where a feature is available in 2 out of 3 serverless project types. It may make sense to clarify and be explicit about the feature being “unavailable” for the 3rd type. For example:

  ```yml
  ---
  applies_to:
    stack: ga
    serverless:
      elasticsearch: ga
      security: ga
      observability: unavailable
  ---
  ```


* When a specific section, paragraph or list item has specific applicability that differs from the context set at the page or section level, and the action is not possible at all for that context (meaning that there is no alternative). For example:

  ````md
  ---
  applies_to:
    stack: ga
    serverless: ga
  —--

  # Spaces

  [...]

  ## Configure a space-level landing page [space-landing-page]
  ```{applies_to}
  serverless: unavailable
  ```
  ````
% I think we wanted to not specify stack here

## Placement of badges

### Headings

Use [section annotations](/syntax/applies.md#section-annotations) immediately after a heading when the entire content between that heading and the next [heading](/syntax/headings.md) of the same or higher level is version or product-specific.

For example, in the [Semantic text field type](https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/semantic-text#custom-by-pipelines) page, all the content in this section is only applicable to Elastic Stack versions 9.0.0 and later.

::::{image} ./images/heading-correct.png
:screenshot:
:alt: Correct use of applies_to with headings
::::

:::{warning}
Do **not** use [inline annotations](/syntax/applies.md#inline-annotations) with headings, which will cause rendering issues in _On this page_.

::::{image} ./images/heading-incorrect.png
:screenshot:
:alt: Rendering error when using inline applies_to with headings
::::
:::


### Definition lists

The recommended placement of `applies_to` badges in definition lists varies based what part(s) of the list item relate to the badge.

#### If the badge is relevant to the entire contents of a list item, put it at the end of the term [definition-list-item-full]

This means using an inline annotation at the end of the same line as the term. For example, on the Kibana [Advanced settings](https://www.elastic.co/docs/reference/kibana/advanced-settings#kibana-banners-settings) page, the entire `banners:linkColor` option is only available in Elastic Stack 9.1.0 and later.

:::{image} ./images/definition-list-entire-item.png
:screenshot:
:alt: Correct use of applies_to with definition list item
:::

:::{warning}
Do **not** put the `applies_to` badge at the beginning or end of the definition if it relates to the entire contents of the item.

::::{image} ./images/definition-list-item-incorrect.png
:screenshot:
:alt: Incorrectly using inline applies_to with a definition list item
::::
:::

#### If the badge is only relevant to a portion of the definition, follow the appropriate placement guidelines for the elements used in the definition [definition-list-item-part]

This might include labeling just one of multiple paragraphs, or one item in an ordered or unordered list. For example, on the [Google Gemini Connector page](https://www.elastic.co/docs/reference/kibana/connectors-kibana/gemini-action-type#gemini-connector-configuration), the default model is different depending on the deployment type and version of the Elastic Stack. These differences should be called out with their own `applies_to` badges.

::::{image} ./images/definition-list-portion-correct.png
:screenshot:
:alt: Correctly using inline applies_to in a portion of a definition list item
::::


### Ordered and unordered lists

Reorganize content as needed so the `applies_to` badge is relevant to the entire contents of the list item.
The recommended placement of the badge varies based on the purpose of the list.

#### If the purpose of the list is to illustrate the difference between situations, put a badge at the start of each item [list-compare-applies_to]

This could mean distinguishing between deployment types, products, lifecycles, or versions.
Placing the badge at the beginning of the list item, allows the reader to scan the list for the item that is relevant to them.

For example, the [Alerting and action settings in Kibana](https://www.elastic.co/docs/reference/kibana/configuration-reference/alerting-settings) page lists how the default value for the `xpack.actions.preconfigured.<connector-id>.config.defaultModel` setting varies in Serverless/Stack and across versions.

::::{image} ./images/list-correct.png
:screenshot:
:alt:
::::

#### If the list just happens to have one or more items that are only relevant to a specific situation, put the badge at the end of the list item [list-other]

Placing the badge at the end of the list item maintains the flow of the list without distracting the reader with badges while still making it clear that the content in that list item is only applicable to the specified situation.

For example, the [Add filter controls](https://www.elastic.co/docs/explore-analyze/dashboards/add-controls) page lists ways to configure ES|QL controls. Only one of the ways they can be controlled was added for the first time in 9.1.0.

::::{image} ./images/list-misc-correct.png
:screenshot:
:alt:
::::


% Reference: Slack conversation
### Tables

The recommended placement in tables varies based on what part(s) of the table related to the `applies_to` label.

#### If the badge is relevant to the entire row, add the badge to the end of the first column [table-row]

For example, a table that contains one setting per row and a new setting is added in 9.1.0.

<image>

#### If the badge is relevant to one cell or part of a cell, add the badge to the cell it applies to [table-cell]

For example, a new property is available in 9.1.0 for a setting that already existed before 9.1.0.

<image>

#### If the badge is relevant to part of a cell, add the badge to the end of the line it applies to [table-cell-part]

For example, a new property is available in 9.1.0 for a setting that already existed before 9.1.0.

<image>

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231856744
:::{tip}
If needed, break the contents of the cell into multiple lines using `<br>` to isolate the content you're labeling.

<image>
:::

### Tabs

In the future ([elastic/docs-builder#1436](https://github.com/elastic/docs-builder/issues/1436)), tabs will be able to accept `applies_to` information. Until then, if an `applies_to` badge is relevant to the entire tab item, add the badge to the beginning of the content.

<image>

### Admonitions

In the future ([elastic/docs-builder#1436](https://github.com/elastic/docs-builder/issues/1436)), admonitions will be able to accept `applies_to` information. Until then, if an `applies_to` badge is relevant to the entire admonition, add the badge to the beginning of the content.

::::{image} ./images/admonition-correct.png
:screenshot:
:alt:
::::

### Dropdowns

In the future ([elastic/docs-builder#1436](https://github.com/elastic/docs-builder/issues/1436)), dropdowns will be able to accept `applies_to` information. Until then, if an `applies_to` badge is relevant to the entire dropdown, add the badge to the beginning of the content.

<image>

### Code blocks

To specify `applies_to` information for a code block, refer to [](/contribute/cumulative-docs/content-patterns.md#code-block).

### Images

To specify `applies_to` information for an image, refer to [](/contribute/cumulative-docs/content-patterns.md#screenshot).
