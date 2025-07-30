---
navigation_title: Best practices
---

# Best practices for using `applies_to`

Depending on what you're trying to communicate, you can use the following patterns to represent version and deployment type differences in your docs.

## General guidelines

% Reference: Slack conversation
* The `applies_to` badges should not require contributors to add specific Markdown real estate
  to the page layout (such as a specific `Version` column in a table).
  Instead, contributors should be able to add them anywhere they need, and the system should
  be in charge of rendering them clearly.

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231876006
* Avoid using version numbers in prose adjacent to `applies_to` badge to prevent
  confusion when the badge is rended with `Planned` ahead of a release.

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231850710
* Create hierarchy of versioned information??

% Reference: https://elastic.github.io/docs-builder/versions/#defaults-and-hierarchy
* Do not assume a default deployment type, stack flavor, product version, or project type.
  Treat all flavors and deployment types equally. Don't treat one as the "base" and the other as the "exception".

## Order of items

**Versions.** Always put the newest version first when listing multiple versions. As a result, the lifecycles should be in order of product development progression, too.

% Reference: https://elastic.github.io/docs-builder/versions/#defaults-and-hierarchy
% Needs work...
**Keys.** Always list [keys](/contribute/cumulative-docs/reference.md#key) in the same order for consistency. The order of keys should reflect organizational priorities. For example, use the following order:
* **Serverless/Elastic Stack**: Serverless, Stack
* **Deployment types**: Elastic Cloud Serverless, Elastic Cloud Hosted, Elastic Cloud on Kubernetes, Elastic Cloud Enterprise, Self-managed
* **Monitoring for Java applications**: Elastic Distribution of OpenTelemetry (EDOT) Java, APM Java agent

## Placement of badges

### Headings

Use [section annotations](/syntax/applies.md#section-annotations) immediately after a heading when the entire content between that heading and the next [heading](/syntax/headings.md) of the same and higher level.

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

This might include labeling just one of multiple paragraphs or one item in an ordered or unordered list. For example, on the ... page, ...

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

<!-- ### Stepper -->

<!-- ## Scenarios [scenarios]

There are several scenarios you will likely run into at some point when contributing to the docs.

### Feature in beta or technical preview is removed [beta-preview-removed]

If a feature in beta or technical preview is removed without going GA,
[list both || remove the beta/preview version]
in the `applies_to` badge.

% TO DO: Copy over example content https://github.com/elastic/kibana/pull/229485/files#r2231843057
**Example: A beta option was removed one minor after it was introduced**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

### Code block change between versions [code-blocks]

If the content of a code block changes between versions,
you have a couple options depending on the nature of the change.

#### Content is added or removed [code-blocks-added-removed]

Use code callouts to point out lines that have changed over time.

**Example: One new option is available**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

#### Content is replaced [code-blocks-replaced]

Use a tab for each version that contains a change.

**Example**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

### Adjacent block elements change between versions [adjacent-block-elements]

**Example**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

### Images change between versions [images]

#### Screenshots

Follow these principles to use screenshots in our unversioned documentation system:

* Reduce screenshots when they don’t explicitly add value.
* When adding a screenshot, determine the minimum viable screenshot and whether it can apply to all relevant versions.
* Take and maintain screenshots for only the most recent version, with very few exceptions that should be considered on a case-by-case basis.
* In case of doubt, prioritize serverless.

**Example**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

### UI changes between versions [ui]

**Example**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

### One sentence in a paragraph changes between versions [inline-elements]

**Example**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

### Table columns, rows, or cells change between versions

#### Content is added or removed

**Example**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
::::

#### Content is replaced

**Example**

::::{tab-set}
:::{tab-item} Visual
:::
:::{tab-item} Syntax
:::
:::: -->
