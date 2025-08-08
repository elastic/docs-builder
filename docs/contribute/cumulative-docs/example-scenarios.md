---
navigation_title: Example scenarios
---

% Audience: Technical writers and other frequent docs contributors
% Goals:
%   * Provide realistic examples of situations with one or more solution

# Cumulative docs example scenarios

## Page applies to both stateful and serverless [stateful-serverless]

If an entire page is primarily about using or interacting with both Elastic Stack components and
the Serverless UI, add both the `stack` and `serverless` keys to the `applies_to` in the frontmatter.

### If released in Serverless, but not yet released in Elastic Stack

:::{include} /syntax/_snippets/stack-serverless-lifecycle-example.md
:::

% ## Content on orchestrating, deploying, or configuring an installation

## Only one section applies [one-section]

The whole page is generally applicable to all deployment types, but one specific paragraph
only applies to Elastic Cloud Hosted and Serverless, and another paragraph only applies to
Elastic Cloud Enterprise:

````
---
applies_to:
  deployment: all
---

# Security

[...]

## Cloud organization level security [cloud-organization-level]

```{applies_to}
deployment:
  ess: ga
serverless: ga
```

[...]

## Orchestrator level security [orchestrator-level]

```{applies_to}
deployment:
  ece: ga
```

[...]
````

% TO DO: Add real example
% <image>

:::{tip}
Likewise, when the difference is specific to just one paragraph or list item, the same rules apply.
Just the syntax slightly differs so that it stays inline: `` {applies_to}`ess: ga` {applies_to}`serverless: ga` ``.


:::

## Only one section does _not_ apply [not-one-section]

A whole page is generally applicable to Elastic Stack 9.0.0 and to Serverless,
but one specific section isn’t applicable to Serverless.

````
---
applies_to:
  stack: ga
  serverless: ga
---

# Spaces

[...]

## Configure a space-level landing page [space-landing-page]

```{applies_to}
serverless: unavailable
```
````

% TO DO: Add real example
% <image>

:::{warning}
Don’t overload with exclusions unless it is necessary.
:::

:::{tip}
Likewise, when the difference is specific to just one paragraph or list item, the same rules apply.
Just the syntax slightly differs so that it stays inline: `` {applies_to}`serverless: unavailable` ``.
:::

## Feature added to unversioned product [unversioned-added]

_Work in progress._

% TO DO: Add description
% TO DO: Add example

## Feature in an unversioned product changes lifecycle [unversioned-changed]

_Work in progress._

% TO DO: Add description
% TO DO: Add example

## Feature in an unversioned product is removed [unversioned-removed]

_Work in progress._

% TO DO: Add description

```
---
applies_to:
  stack: deprecated 9.1, removed 9.4
  serverless: removed
---
```

## Feature in a versioned product changes lifecycle [versioned-changed]

_Work in progress._

% TO DO: Add description
% TO DO: Add example

## Code block content varies [code-block]

Often the content in a code block will vary between situations (versions, deployment types, etc).
There are a couple possible solutions.

### Solution A: Use a code callout [code-block-callout]

Using a code callout is the lightest-touch solution, but might not be sufficient in all cases.

**When to use a code callout**:

* The code block and its callouts fit vertically on a typical laptop screen.
  This will reduce the risk of users copying the code snippet without reading the information in the callout.
* Syntax is either just added or just removed — syntax is not modified.
  It is difficult to communicate that some syntax is needed in more than one situation but varies depending on the situation.
* The code block will not require more than 3 `applies_to`-related callouts.
  At that point, the code becomes more difficult to read and use.

**Best practices**:

* Place the badge at the beginning of the callout.

**Example**: A new option was made available in 9.1.0.

::::{image} ./images/example-code-block-callout.png
:screenshot:
:alt:
::::

### Solution B: Use tabs [code-block-tabs]

**When to use tabs**: If using a [code callout](#code-block-callout) isn't appropriate.

**Best practices**:

* Try to minimize the number of tabs where possible,
  but do not mix tabs and `applies_to`-related code callouts.
* Try not to include information surrounding a code block in the tabs.
  Make the tab content as small as possible apart from the procedure itself.

**Example**:

::::{image} ./images/example-code-block-tabs.png
:screenshot:
:alt:
::::

## Workflows vary [workflow]

When one or more steps in a process differs.

### Solution A: Use inline `applies_to` [workflow-inline]

Using inline `applies_to` badges to a few line items in an ordered list is the lightest-touch solution,
but might not be sufficient in all cases.

**When to use inline `applies_to`**:

* Workflow steps that vary between situations can be easily isolated.
* Each step that varies, only varies between 3 or fewer situations (deployment types, versions, etc).
* There are no more than 3 steps that need to be split into multiple lines with `applies_to` badges.

**Best practices**:

* Follow the [best practices for ordered and unordered lists](/contribute/cumulative-docs/badge-placement.md#ordered-and-unordered-lists)
  including the order of items and the placement of labels.

**Example**: Only one item in an ordered list varies between Serverless and stateful.

::::{image} ./images/workflow-inline.png
:screenshot:
:alt:
::::

### Solution B: Use tabs [workflow-tabs]

Tabs are minimally disruptive in many situations.

**When to use tabs**:

* Using [inline `applies_to` badges](#workflow-inline) isn't appropriate.
* All the tabs fit horizontally on a single row on a typical laptop screen.
  This is usually around a maximum of four tabs.
* The tab with the most content fits vertically on a typical laptop screen.
  This is usually around of 20 lines.

**Best practices**:

* Try to minimize the number of tabs where possible. Try to work around small differences by
  rewording or adding context in prose or in `note` style admonitions.
* Try not to include information surrounding a procedure in the tabs.
  Make the tab content as small as possible apart from the procedure itself.
* Consider breaking up procedures into sets of procedures if only one section differs between contexts.

% TO DO: Add example
% **Example**:
% <image>

### Solution C: Use sibling pages [workflow-sibling-pages]

Sibling pages are a last resort when no other solutions are appropriate.

**When to use sibling pages**:

* Neither [inline `applies_to` badges](#workflow-inline) or [tabs](#workflow-tabs) are appropriate.
* The workflow has significant differences across multiple procedures.
* There are chained procedures where not all of the procedures are needed for all contexts
  or where the flow across procedures is muddied when versioning context is added.
* The workflow exists in a very complex page that is already heavily using tabs and other tools we use for versioning differences.
  This makes it difficult to add another “layer” of content.
% * Beta to GA transitions? Hide the beta doc and leave it linked to the GA doc, which will presumably be more stable?

% TO DO: Add best practices
% **Best practices**:

**Example**:
* [Cloud Hosted deployment billing dimensions](https://elastic.co/docs/deploy-manage/cloud-organization/billing/cloud-hosted-deployment-billing-dimensions)
* [{{serverless-short}} project billing dimensions](https://elastic.co/docs/deploy-manage/cloud-organization/billing/serverless-project-billing-dimensions)

## Screenshots vary [screenshot]

Sometimes the UI differs between versions, deployment types or other conditions.

### Solution A: Use tabs [screenshot-tabs]

**When to use tabs**:
* When the screenshot shows significantly different interfaces or workflows for each product, deployment type, or version.
* When the screenshot represents a specific, interactive action, like clicking a button or navigating a UI that changes meaningfully between contexts.

**Best practices**:
* Keep any explanatory text outside the tab unless it's specific to the screenshot inside.

% TO DO: Add example
% **Example**:
% A walkthrough for configuring alerts in Kibana differs between Elastic Stack and Serverless deployments.
% <image>

### Solution B: Add a note [screenshot-note]

In cases where only a small visual detail differs (for example, a button label or icon), it’s often more efficient to add a note rather than creating tabbed screenshots.

**When to use a note**:
* When the screenshot is mostly consistent, but includes minor visual or behavioral differences.
* When adding another screenshot would be redundant or distracting.

**Best practices**:
* Keep notes concise, ideally one sentence.
* Place the note directly after the screenshot.
* Use an `applies_to` badge at the start of the note if relevant.

% TO DO: Add example
% **Example**:
% In Serverless, the "Create rule" button is labeled "Add rule."
% <image>

## Multiple adjacent block elements vary [multiple-block]

### Solution A: Use headings [multiple-block-headings]

_Work in progress._

% TO DO: Add all sections
% **When to use headings**:
% **Best practices**:
% **Example**:

### Solution B: Use tabs [multiple-block-tabs]

**When to use tabs**:
* When the content is structurally similar but differs in detail — for example, slightly different instructions, outputs, or paths.
* When you want to avoid repeating most of the surrounding content and isolate just the difference.

**Best practices**:
* Only include content that varies inside the tab — don’t wrap entire pages or unrelated information.
* Keep tabs short and focused to reduce cognitive load.
* Label tabs clearly and consistently (e.g., by version or product).

% TO DO: Add example
% **Example**:
% <image>

## Feature in beta or technical preview is removed [beta-removed]

_Work in progress._

% TO DO: Add description
% TO DO: Add example
% <image>
