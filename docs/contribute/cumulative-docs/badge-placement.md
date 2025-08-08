---
navigation_title: Badge placement
---

% Audience: Technical writers and other frequent docs contributors
% Goals:
%   * Provide guidance on badge placement in specific situations

# `applies_to` badge placement

As you continue contributing to documentation and more versions are released,
you might have questions about how to integrate `applies_to` badges in
cumulative documentation.

## General principles

% Source: Brandon's PR review comment
At a high level, you should follow these badge placement principles:

* Place badges where they're most visible but least disruptive to reading flow
* Consider scanning patterns - readers often scan for relevant information
* Ensure badges don't break the natural flow of content
* Use consistent placement patterns within similar content types

## Specific elements

More specific guidance in common situations is outlined below.

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

Add the badge to the end of the first column to indicate that it applies to all cells in a row.

For example, the [Streaming Input](https://www.elastic.co/docs/reference/beats/filebeat/filebeat-input-streaming#_metrics_14) page includes a table that contains one setting per row and a new setting is added in 9.0.4.

::::{image} ./images/table-entire-row-correct.png
:screenshot:
:alt:
::::

:::{warning}
Do **not** create a new column just for versions.
The `applies_to` badges should _not_ require contributors to add specific Markdown real estate to the page layout.
Instead, contributors should be able to add them anywhere they need, and the system should be in charge of rendering them clearly.

::::{image} ./images/table-entire-row-incorrect.png
:screenshot:
:alt:
::::
:::

#### If the badge is relevant to one cell or part of a cell, add the badge to the cell it applies to [table-cell]

Add the badge to the cell to indicate that it applies to that one cell only.

For example, the [Collect application data](https://www.elastic.co/docs/solutions/observability/apm/collect-application-data#_capabilities) page includes a table that compares functionality across two methods for collecting APM data, and only one of the methods is in technical preview.

::::{image} ./images/table-one-cell-correct.png
:screenshot:
:alt:
::::
:::

:::{tip}
If the one cell that the badge applies to is in the first column, consider formatting the content
using something other than a table (for example, a definition list) to avoid confusion with the
[previous scenario](#table-row) in which adding the badge to the first column indicates that the
badge applies to the whole row.
:::

#### If the badge is relevant to part of a cell, add the badge to the end of the line it applies to [table-cell-part]

For example, the [Parse AWS VPC Flow Log](https://www.elastic.co/docs/reference/beats/filebeat/processor-parse-aws-vpc-flow-log) page includes new information relevant to 9.2.0 and later about a setting that already existed before 9.2.0.

::::{image} ./images/table-part-of-cell-correct.png
:screenshot:
:alt:
::::
:::

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231856744
:::{tip}
If needed, break the contents of the cell into multiple lines using `<br>` to isolate the content you're labeling or consider not using a table to format the related content.
:::

### Tabs

In the future ([elastic/docs-builder#1436](https://github.com/elastic/docs-builder/issues/1436)), tabs will be able to accept `applies_to` information. Until then, if an `applies_to` badge is relevant to the entire tab item, add the badge to the beginning of the content.

% TO DO: Add example
% <image>

### Admonitions

In the future ([elastic/docs-builder#1436](https://github.com/elastic/docs-builder/issues/1436)), admonitions will be able to accept `applies_to` information. Until then, if an `applies_to` badge is relevant to the entire admonition, add the badge to the beginning of the content.

::::{image} ./images/admonition-correct.png
:screenshot:
:alt:
::::

### Dropdowns

In the future ([elastic/docs-builder#1436](https://github.com/elastic/docs-builder/issues/1436)), dropdowns will be able to accept `applies_to` information. Until then, if an `applies_to` badge is relevant to the entire dropdown, add the badge to the beginning of the content.

% TO DO: Add example
% <image>

### Code blocks

To specify `applies_to` information for a code block, refer to [](/contribute/cumulative-docs/example-scenarios.md#code-block).

### Images

To specify `applies_to` information for an image, refer to [](/contribute/cumulative-docs/example-scenarios.md#screenshot).
