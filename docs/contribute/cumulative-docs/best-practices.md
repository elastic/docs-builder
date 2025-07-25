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

* Always put the newest version first when [listing multiple versions]().

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231876006
* Avoid using version numbers in prose adjacent to `applies_to` badge to prevent
  confusion when the badge is rended with `Planned` ahead of a release.

% Reference: https://github.com/elastic/kibana/pull/229485/files#r2231850710
* Create hierarchy of versioned information??

% Reference: https://elastic.github.io/docs-builder/versions/#defaults-and-hierarchy
* Do not assume a default deployment type, stack flavor, product version, or project type.
  Treat all flavors and deployment types equally. Don't treat one as the "base" and the other as the "exception".

% Reference: https://elastic.github.io/docs-builder/versions/#defaults-and-hierarchy
% Needs work...
* List keys in the same order for consistency. This order reflects organizational priorities.
  * Serverless
  * Stack
  * Elastic Cloud Hosted
  * Elastic Cloud on Kubernetes
  * Elastic Cloud Enterprise
  * Self-managed

## Placement of labels

% Reference: https://github.com/elastic/docs-builder/issues/1574
* **Headings**: Use section annotations immediately after the heading.
  Do _not_ use inline annotations with headings, which will cause rendering issues in "On this page".

% Reference: https://github.com/elastic/kibana/pull/229485/files
* **Definition lists**: If the `applies_to` badge is relevant to the entire contents of the list item,
  put it at the end of the term (on the same line).

% Reference: TBD
* **Ordered and unordered lists**: Reorganize content as needed so the `applies_to` badge is relevant
  to the entire contents of the list item. The recommended placement of the badge varies:
  * If the purpose of the list is to illustrate the difference between several applications (deployment types,
    products, lifecycles, versions, etc.) put an `applies_to` badge at the start of each item.
  * If the list just happens to have one or more items that are only relevant to specific application
    (deployment type, product, lifecycle, version, etc.) put the `applies_to` badge at the end of the list item.

% Reference: Slack conversation
* **Tables**: The recommended placement in tables varies:
  * If the `applies_to` badge is relevant to the entire row, add the badge to the end of
    the first column. For example, a table that contains one setting per row and a new setting
    is added in 9.1.0.
  * If the `applies_to` badge is relevant to one cell or part of a cell, add the badge to the
    end of the line it applies to. For example, a new property is available in 9.1.0 for a setting
    that already existed before 9.1.0.
    % Reference: https://github.com/elastic/kibana/pull/229485/files#r2231856744
    If needed, break the contents of the cell into multiple lines using `<br>` to isolate the
    content you're labeling.

## Scenarios [scenarios]

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
::::
