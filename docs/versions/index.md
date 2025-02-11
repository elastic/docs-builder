---
navigation_title: "Versions and types"
---

# Documenting versions and deployment types

In the new documentation system, we document features in a centralized place, regardless of the software version or deployment type it applies to. 
For example, we might document the serverless and v9 implementations of a feature on a single page, and then use content patterns to highlight where prerequisites, options, or behaviors differ between these implementations.

This approach improves our documentation in several ways: 

* When a user arrives in our documentation from an outside source, they'll be likelier to land on a page or section that applies to them.
* There is a single "source of truth" for each feature, which helps us to maintain consistency, accuracy, and maintainability of our documentation over time, and avoids "drift" between multiple similar sets of documentation.
* Comparing and contrasting differences helps users to understand what is available to them, and improves awareness of the ways certain offerings might improve their experience.

::::{note}
This page documents the first version of our approach to documenting differences in versions and deployment types, using our current technologies. 
Our approach might change as additional documentation features become available.
::::

## Basic concepts and principles

* [Versioning facets](#versioning-facets)
* [Defaults and hierarchy](#defaults-and-hierarchy)
* [Versions and lifecycle states](#versions-and-lifecycle-states)

### Versioning facets
There are multiple facets to versioning that we need to consider: 

* **Stack flavor:** The Elasticsearch or Kibana feature base used for basic functionality. Either **Serverless** (sometimes "Elastic Cloud Serverless") or **{{stack-short}} <version>**

% TODO: Final term for "Stack"
* **Deployment type:** The way Elastic is deployed: 
  * **Serverless** (sometimes "Elastic Cloud Serverless")
  * **Elastic Cloud Hosted**
  * **Elastic Cloud on Kubernetes**
  * **Elastic Cloud Enterprise**
  * **Self-managed**

  All deployment types other than **Serverless** are used to run a **{{stack-short}} <version>** flavor of Elasticsearch / Kibana. ECK and ECE also have their own versioning. For example, one can run a v9.0.0 deployment on ECE 4.0.

% TODO: Final term for "Self-managed"

* **Project type:** The Serverless project types where a feature can be used - either **Elasticsearch**, **Security**, or **Observability**.

* **Other versioning schemes:** Elastic products or tools with a versioned component, where stack versioning is not followed. 
  
  E.g. clients, Elastic Common Schema

:::{warning}
The self-managed deployment type and support for products or platforms other than {{stack-short}} does not exist yet. Contribute to the discussion in [#452](https://github.com/elastic/docs-builder/discussions/452)
:::

**How many facets do I need to use?**

The role of these labels is providing a trust signal that the reader is viewing content that’s applicable to them. This means that the relevant labels should appear on all pages. However, we can choose to expose only one versioning plane on pages where only one plane is relevant:

* Depending on what you're documenting, you might need to include information from multiple facets. For example, when relevant features exist at both the stack and the deployment level, both of these groups might be used together (e.g. security, user management, and trust features differ both at the deployment level and at the stack version level).

* In some sections, such as **Explore and analyze**, features generally only differ by stack flavor. In these cases, you can choose to include only this facet on the page.

### Defaults and hierarchy 

Do not assume a default deployment type, stack flavor, product version, or project type.

Treat all flavors and deployment types equally. Don't treat one as the "base" and the other as the "exception".

However, we should all display our flavors and versions in the same order for consistency. This order reflects organizational priorities.

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

### Versions and lifecycle states

In the V3 docs system, we currently document only our latest major versions (Stack 9.0+, ECE 4.0+, ECK 3.0+).

Add version labels only when a feature was added as part of the major version release, or added in subsequent dot releases. Do not add version information to features added before these releases. For example, features added in 8.16 don't need an 8.16 label or a 9.0 label, but features added in 9.0 need a 9.0 label.

From the latest major onward, the entire feature lifecycle should be represented. This means that you need to add separate labels for the following states:

* beta or technical preview
* GA
* deprecated
* removed

We hope to simplify or consolidate these lifecycle labels in future.

::::{warning}
This feature doesn't currently work - currently, only one label will appear.
::::

## Content patterns

Depending on what you're trying to communicate, you can use the following patterns to represent version and deployment type differences in your docs.

Choose from:

:::{include} _snippets/content-patterns-list.md
:::

## Best practices

### Screenshots

Follow these principles to use screenshots in our unversioned documentation system:

* Reduce screenshots when they don’t explicitly add value.
* When adding a screenshot, determine the minimum viable screenshot and whether it can apply to all relevant versions.
* Take and maintain screenshots for only the most recent version, with very few exceptions that should be considered on a case-by-case basis.
* In case of doubt, prioritize serverless.