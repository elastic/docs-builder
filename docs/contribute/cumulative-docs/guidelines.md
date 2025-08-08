---
navigation_title: Guidelines
---

% Audience: Anyone who contributes to docs
% Goals:
%   * Provide guidance that is appropriate for new contributors

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
% TO DO: Add when https://github.com/elastic/docs-builder/issues/1436 is complete
% * **Element-level** annotations allow tagging block-level elements like tabs, dropdowns, and admonitions.
%  This is useful for ...
* **Inline** annotations allow fine-grained annotations within paragraphs or definition lists.
  This is useful for highlighting the applicability of specific phrases, sentences,
  or properties without disrupting the surrounding content.

% TO DO: Can these be pruned? 🌿
## General guidelines

% Source: Used to be in `docs/syntax/applies.md`
* Use `applies_to` tags when features change state (`preview`, `beta`, `ga`, `deprecated`, `removed`) or when
  availability differs across deployments and environments.
* Use `applies_to` tags to indicate which product or deployment type the content applies to. This is mandatory for every
  page.
* Use `applies_to` tags when features change state in a specific update or release.
* Do _not_ tag content-only changes like typos, formatting, or documentation updates that don't reflect feature lifecycle
  changes.
* You do _not_ need to tag every section or paragraph. Only do so if the context or applicability changes from what has
  been established earlier.
* If the product is not versioned (meaning all users are always on the latest version, like in serverless or cloud), you
  do _not_ need to tag a new GA feature.

% Source: Slack conversation
* The `applies_to` badges should not require contributors to add specific Markdown real estate
  to the page layout (such as a specific `Version` column in a table).
  Instead, contributors should be able to add them anywhere they need, and the system should
  be in charge of rendering them clearly.

% Source: https://github.com/elastic/kibana/pull/229485/files#r2231850710
% * Create hierarchy of versioned information??

% Source: George's checklist
* Don’t overload with exclusions unless it is necessary - If a page is only about Elastic Cloud Hosted, no need to say serverless: unavailable, just add deployment: ess:

% TO DO: Open an issue to force the order in the code.
### Order of items

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

% Source: George's checklist
### Common pitfalls

* Don’t assume features are available everywhere - If a Kibana UI panel is missing from Serverless, notate it in the documentation even if it is intuitive.
* Clarify version availability per context - Sometimes features GA for one deployment but remain preview for another.
* Think across time - Product lifecycle changes with each release. Even if a feature might be deprecated or legacy in one deployment it may still be supported elsewhere. (ILM / datastreams)
* For updates, remember they may be older than you think - Some updates that may be required to the documentation could precede v9.0. For these changes need to be made to the old ASCIIdoc versions of the content.

## Product and deployment model tags [products-and-deployment-models]

For the full list of supported product and deployment model tags,
refer to [](/contribute/cumulative-docs/reference.md#key).

### Guidelines [products-and-deployment-models-guidelines]

* **Always include page-level product and deployment model applicability information**.
  This is _mandatory_ for all pages.
* **Determine if section or inline applicability information is necessary.**
  This _depends on the situation_.
  * For example, if a portion of a page is applicable to a different context than what was specified at the page level,
  clarify in what context it applies using section or inline `applies_to` badges.
% Source: https://elastic.github.io/docs-builder/versions/#defaults-and-hierarchy
* **Do not assume a default product or deployment type.**
  Treat all products and deployment types equally. Don't treat one as the "base" and the other as the "exception".

### Example scenarios [products-and-deployment-models-examples]

Here are some common scenarios you might come across:

* Content is about both Elastic Stack components and the Serverless UI
  ([example](/contribute/cumulative-docs/example-scenarios.md#stateful-serverless)).
* Content is primarily about orchestrating, deploying or configuring an installation
  ([example](/contribute/cumulative-docs/example-scenarios.md#stateful-serverless)).
* Content is primarily about a product following its own versioning schema
  ([example](/contribute/cumulative-docs/example-scenarios.md#)).
* A whole page is generally applicable to Elastic Stack 9.0 and to Serverless,
  but one specific section isn’t applicable to Serverless
  ([example](/contribute/cumulative-docs/example-scenarios.md#)).
* The whole page is generally applicable to all deployment types,
  but one specific paragraph only applies to Elastic Cloud Hosted and Serverless,
  and another paragraph only applies to Elastic Cloud Enterprise
  ([example](/contribute/cumulative-docs/example-scenarios.md#)).
* Likewise, when the difference is specific to just one paragraph or list item, the same rules apply.
  Just the syntax slightly differs so that it stays inline
  ([example](/contribute/cumulative-docs/example-scenarios.md#)).

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
% Source: https://github.com/elastic/kibana/pull/229485/files#r2231876006
* **Do _not_ use version numbers in prose**.
  Avoid using version numbers in prose adjacent to `applies_to` badge to prevent
  confusion when the badge is rended with `Planned` ahead of a release.
* **Cumulative documentation is not meant to replace release notes.**
  * For example, if a feature becomes available in {{serverless-short}} and
    doesn’t have a particular lifecycle state to call out (preview, beta, deprecated…),
    it does not need specific tagging.
* Consider carefully [when the change is going to be published](/contribute/branching-strategy.md).
* We do not do date-based tagging for unversioned products.

### Example scenarios [versions-examples]

* A new feature is added to {{serverless-short}} or {{ecloud}}
  ([example](/contribute/cumulative-docs/example-scenarios.md#)).




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

