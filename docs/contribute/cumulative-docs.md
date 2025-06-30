# Write cumulative documentation 

<!--
This page explains our cumulative documentation philosophy, paired with examples. Component guidance for reference purposes goes in syntax/applies.md. 
-->

In elastic.co/docs (Docs V3), we write docs cumulatively regardless of the [branching strategy](branching-strategy.md) selected.

**What does this mean?** 

With our markdown-based docs, there is no longer a new documentation set published with every minor release: the same page stays valid over time and shows version-related evolutions.

This new behavior starts with the following **versions** of our products: Elastic Stack 9.0, ECE 4.0, ECK 3.0, and even more like EDOT docs. It also includes our unversioned products: Serverless and Elastic Cloud.

:::{note} 
Nothing changes for our ASCIIDoc-based documentation system, that remains published and maintained for the following versions: Elastic Stack until 8.x, ECE until 3.x, ECK until 2.x, etc.
:::

**How does it change the way we write docs?** 

As new minor versions are released, we want users to be able to distinguish which content applies to their own ecosystem and product versions without having to switch between different versions of a page.

This extends to deprecations and removals: No information should be removed for supported product versions, unless it was never accurate. It can be refactored to improve clarity and flow, or to accommodate information for additional products, deployment types, and versions as needed.

In order to achieve this, the markdown source files integrate a tagging system meant to identify:

* [Which Elastic products and deployment models the content applies to](#tagging-products-and-deployment-models) (for example, Elastic Cloud Serverless or Elastic Cloud Hosted).  
* [When a feature goes into a new state as compared to the established base version](#tagging-version-related-changes-mandatory) (for example, being added or going from Beta to GA).

This tagging system is mandatory for all of the public-facing documentation. We refer to it as “[applies_to](https://elastic.github.io/docs-builder/syntax/applies/)” tags or badges.

## Tagging products and deployment models

### Page-level frontmatter (mandatory)

All documentation pages **must** include an `applies_to` tag in the YAML frontmatter. Use YAML frontmatter to indicate each deployment target's availability and lifecycle status. 

* The `applies_to` attribute is used to display contextual badges on each page.  
* The `products` attribute is used by the search to let users filter their results when searching the docs.

  For the full list of supported keys and values, refer to [frontmatter](https://elastic.github.io/docs-builder/syntax/frontmatter).  


:::{include} /syntax/_snippets/page-level-applies-examples.md
:::

### Section or inline contexts (situational)

When the context differs from what was specified at the page level in a specific section or part of the page, it is appropriate to re-establish it. For example: 

:::{include} /syntax/_snippets/section-level-applies-examples.md
:::

* Likewise, when the difference is specific to just one paragraph or list item, the same rules apply. Just the syntax slightly differs so that it stays inline:

  :::{include} /syntax/_snippets/line-level-applies-example.md
  :::

### When should I indicate that something is NOT applicable to a specific context?

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
  stack: ga
  serverless: unavailable
  ```
  ````
% I think we wanted to not specify stack here

## Tagging version-related changes (mandatory)

In the previous section, we’ve considered product and deployment availability. Feature lifecycle and version-related changes are communicated as part of the same [applies\_to](https://elastic.github.io/docs-builder/syntax/applies/) tagging logic, by specifying different values to each supported key. 

**Are you sure your change is related to a specific version? Maybe not…**  
This is a frequent case. For example: fixing typos, improving styling, adding a long-forgotten setting, etc.  
For this case, no specific version tagging is necessary.

**A new feature is added to {{serverless-short}} or {{ecloud}}. How do I tag it?**  
Cumulative documentation is not meant to replace release notes. If a feature becomes available in {{serverless-short}} and doesn’t have a particular lifecycle state to call out (preview, beta, deprecated…), it does not need specific tagging.

However, in this scenario, it is important to consider carefully [when the change is going to be published](branching-strategy.md).

We do not do date-based tagging for unversioned products.

### For unversioned products (typically {{serverless-short}} and {{ech}})

:::{include} /syntax/_snippets/unversioned-lifecycle.md
:::

### For versioned products

:::{include} /syntax/_snippets/versioned-lifecycle.md
:::    

### Identify multiple states for the same content  

:::{include} /syntax/_snippets/multiple-lifecycle-states.md
:::
  
## How do these tags behave in the output? 

:::{include} /contribute/_snippets/tag-processing.md
:::