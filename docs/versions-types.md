---
navigation_title: "Versions and types"
---
# Documenting versions and deployment types

In the new documentation system, we document features in a centralized place, regardless of the software version or deployment type it applies to. 
For example, we might document the serverless and v9 implementations of a feature on a single page, and then use content patterns to highlight where prerequisites, options, or behaviors differ between these implementations.

This approach improves our documentation in several ways: 

* When a user arrives in our documentation from an outside source, they'll be likelier to arrive on a page that applies to them.
* There is a single "source of truth" for each feature, which helps us to maintain consistency, accuracy, and maintainability of our documentation over time, and avoids "drift" between multiple similar sets of documentation.
* Comparing and contrasting differences helps users to understand what is available to them, and improves awareness of the ways certain offerings might improve their experience.

::::{note}
This page documents the first version of our approach to documenting differences in versions and deployment types, using our current technologies. 
Our approach might change as additional documentation features become available.
::::

## Basic concepts and principles

:::{dropdown} Versioning facets
There are multiple facets to versioning that we need to consider: 

* **Elasticsearch flavor:** The feature base used for basic functionality. Either **Serverless** (sometimes "Elastic Stack Serverless") or **Stack <version>**
* **Deployment type:** The way Elastic is deployed. Either **Serverless** (sometimes "Elastic Stack Serverless"), **Elastic Cloud Hosted**, **Elastic Cloud on Kubernetes**, **Elastic Cloud Enterprise**, or **Self-managed**. 

  All deployment types other than **Serverless** use the **Stack <version>** Elasticsearch flavor.

% TODO: Final term for "Self-managed"
* **Other versioning schemes:** Elastic products or tools with a versioned component, where stack versioning is not followed. E.G. clients, Elastic Common Schema

**How many facets do I need to use?**

The role of these labels is providing a trust signal that you’re viewing content that’s applicable to you. This means that the relevant labels should appear on all pages. However, we can choose to expose only one versioning plane on pages where only one plane is relevant:

* Depending on what you're documenting, you might need to include information from multiple facets. For example, when features are added both at the stack and the deployment level, both of these groups might be used together (e.g. security, user management, trust).

* In some sections, such as **Explore and analyze**, features generally only differ by Elasticsearch flavor. In these cases, you can choose to include only this facet on the page.
:::

:::{dropdown} Defaults and hierarchy 

You should not assume a default deployment type or Elasticsearch flavor.

Treat all flavors and deployment types equally. Don't treat one as the "base" and the other as the "exception".

However, we should all display our flavors and versions in the same order for consistency. This order represents organizational priorities.

**Flavors:**

1. Serverless
2. Stack

**Deployment types:**

1. Serverless
2. Elastic Cloud Hosted
3. Elastic Cloud on Kubernetes
4. Elastic Cloud Enterprise
5. Self-managed

When it comes to hierarchy of versions, always put the newest version first.
:::

:::{dropdown} Versions and lifecycle states

In the V3 docs system, we currently document only our latest major versions (Stack 9.0+, ECE 4.0+, ECK 3.0+).

Add version labels only when a feature was added as part of the major release, or added in subsequent dot releases. Do not add version information to features added before these releases (e.g. features added in 8.16 don't need an 8.16 label or a 9.0 label).

From the latest major onward, the entire feature lifecycle should be represented. This means that you need to add separate labels for the following states:

* beta or technical preview
* GA
* deprecated
* removed

We hope to simplify or consolidate these lifecycle labels in future.

::::{warning}
This feature doesn't currently work - currently, only one label will appear.
::::

:::


## Content patterns

Depending on what you're trying to communicate, you can use the following patterns to represent version and deployment type differences in your docs.

### Page-level `applies` tags

**Use case:** Provide signals that a page applies to the reader

**Number to use:** Minimum to add clarity 

**Approach:**

Add tags for all **versioning facets** relevant to the page.

See **Versions and lifecycle states** to learn when to specify versions in these tags.

**Example**
TODO

### Section/heading-level `applies` tags

**Use case:** Provide signals about a section’s scope so a user can choose to read or skip it as needed

**Number to use:** Minimum to add clarity

**When to use:** When the section-level scope differs from the page-level scope

**Example**
TODO

### Inline `applies` tags

**Use case:** When features in a **list of features** are exclusive to a specific context, or were introduced in a specific version

**Number to use:** Three max. If the number of tags is longer than that, consider breaking up the list by facet.

### Tabs

**Use case:** When one or more steps in a process differs, put the steps into tabs - one for each process.

**Number to use:** Max 4 or 5

Try to minimize the number of tabs where you can - try to work around small differences by rewording or adding context in prose:

* “In version x and earlier, Spaces were referred to as Places.” 
* “In version x and earlier, click the Permissions tab”.

Try not to include information surrounding a procedure in the tabs. Make the tab content as small as possible apart from the procedure itself.

Consider breaking up procedures into sets of procedures if only one section differs between contexts.

### Sibling bullets 

**Use case:** Requirements, limits, other simple, mirrored facts.

**Number to use:** Ideal is one per option (e.g. one per deployment type). You might consider nested bullets in limited cases.

#### Examples

##### Required permissions

* **Serverless**: `Admin` or equivalent
* **Stack v9**: `kibana_admin` or equivalent

##### Create a space

The maximum number of spaces that you can have differs by [what do we call this]: 

* **Serverless**: Maximum of 100 spaces.
* **Stack v9**: Controlled by the `xpack.spaces.maxSpaces` setting. Default is 1000.

##### Prose (inline)

**Use case:** Clarifying or secondary information

Sometimes, you can just preface a paragraph with version info. Do this in cases where the information isn’t wildly important, but nice to know, or to add basic terminology change info to overviews.

### Screenshots

* Reduce screenshots when they don’t explicitly add value
* When adding a screenshot, determine the minimum viable screenshot and whether it can apply to all relevant versions
* Screenshots should always only represent the most recent version, with very few exceptions that should be considered on a case-by-case basis.
