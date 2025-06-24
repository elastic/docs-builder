

Documentation is cumulative: As functionality changes, both the old and new functionality should be clear to the reader. Don’t delete or destructively edit content to remove context about previous versions.


Lifecycle statuses are additive: If a feature is beta in 9.1 and GA in 9.2, both statuses should be specified so users on any version can understand what applies to them. 


# Write cumulative documentation {#write-cumulative-documentation}

In our markdown-based documentation system, we write docs cumulatively regardless of the publication model selected.

**What does this mean?**  
With our markdown-based docs, there is no longer a new documentation set published with every minor release: the same page stays valid over time and shows version-related evolutions.

This new behavior starts with the following **versions** of our products: Elastic Stack 9.0, ECE 4.0, ECK 3.0, and even more like EDOT docs. It also includes our unversioned products: Serverless and Elastic Cloud.

**Note:** Nothing changes for our asciidoc-based documentation system, that remains published and maintained for the following versions: Elastic Stack until 8.19, ECE until 3.8, ECK until 2.x, etc.

**How does it change the way we write docs?**  
As new minor versions are released, we want users to be able to distinguish which content applies to their own ecosystem and product versions without having to switch between different versions of a page.

This extends to deprecations and deletions: No information should be removed for supported product versions, unless it was never accurate. It can be refactored to improve clarity and flow, or to accommodate information for additional products, deployment types, and versions as needed.

In order to achieve this, the markdown source files integrate a tagging system meant to identify:

* Which Elastic products and deployment models the content applies to.  
* When a feature goes into a new state as compared to the established base version.

This tagging system is mandatory for all of the public-facing documentation. We refer to it as “[applies_to](https://elastic.github.io/docs-builder/syntax/applies/)”.

## Tagging products and deployment models

**Page-level frontmatter   mandatory**  
First and foremost, each documentation page **must** specify which contexts it applies to in its [frontmatter](https://elastic.github.io/docs-builder/syntax/frontmatter).

* The `applies_to` attribute is used to display contextual badges on each page.  
* The `products` attribute is used by the search to let users filter their results when searching the docs.

  For the full list of supported keys and values, check [frontmatter](https://elastic.github.io/docs-builder/syntax/frontmatter).  
  /\!\\ When a key has no value, it is not rendered.


There are 3 typical scenarios to start from:

* The documentation set or page is primarily about using or interacting with Elastic Stack components or the serverless UI:

```yml
--- 
applies_to:
  stack: ga
  serverless: ga
products:
  - id: kibana
  - id: elasticsearch
  - id: elastic-stack
---
```

* The documentation set or page is primarily about orchestrating, deploying or configuring an installation (only include relevant keys):

```yml
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

* The documentation set or page is primarily about a product following its own versioning schema:

```yml
--- 
applies_to:
  product: ga
products:
  -id: edot-collector
---
```

Note: It can happen that it’s relevant to identify several or all of these dimensions for a page. Use your own judgement and check existing pages in similar contexts.


```yml
--- 
applies_to:
  stack: ga
  serverless: ga
  deployment: 
    ess: ga
    ece: ga
    eck: ga
products:
  -id: kibana
  -id: elasticsearch
  -id: elastic-stack
  -id: cloud-serverless
  -id: cloud-hosted
  -id: cloud-enterprise
  -id: cloud-kubernetes
---
```

**Section or inline contexts   situational**  
When the context differs from what was specified at the page level in a specific section or part of the page, it is appropriate to re-establish it. For example: 

* The whole page is generally applicable to Elastic Stack 9.0 and to Serverless, but one specific section isn’t applicable to Serverless (and there is no alternative for it in serverless):

````md
## Configure a space-level landing page [space-landing-page]
```{applies_to}
stack: ga
serverless: unavailable
```
````

* The whole page is generally applicable to Elastic Cloud Enterprise and Elastic Cloud Hosted, but one specific paragraph only applies to Elastic Cloud Enterprise, and another paragraph explains the same, but for Elastic Cloud Hosted:

````md
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

* Likewise, when the difference is specific to just one paragraph or list item, the same rules apply. Just the syntax slightly differs so that it stays inline:

```md
**Spaces** let you organize your content and users according to your needs.

- Each space has its own saved objects.
- Users can only access the spaces that they have been granted access to. This access is based on user roles, and a given role can have different permissions per space.
- {applies_to}`stack: ga` {applies_to}`serverless: unavailable` Each space has its own navigation, called solution view.
```

**When should I indicate that something is NOT applicable to a specific context?**  
By default, we communicate that content does not apply to a certain context by simply **not specifying it**.  
For example, a page describing how to create an Elastic Cloud Hosted deployment just requires identifying “Elastic Cloud Hosted” as context. No need to overload the context with additional `serverless: unavailable` indicators.

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

````yml
--- 
applies_to:
  stack: ga
  serverless: ga
—

# Spaces

[...]

## Configure a space-level landing page [space-landing-page]
```{applies_to}
stack: ga
serverless: unavailable
```
````
##  Tagging version-related changes (mandatory)

In the previous section, we’ve considered product and deployment availability. Feature lifecycle and version-related changes are communicated as part of the same [applies\_to](https://elastic.github.io/docs-builder/syntax/applies/) tagging logic, by specifying different values to each supported key. 

**Are you sure your change is related to a specific version? Maybe not…**  
This is a frequent case. For example, fixing typos, improving styling, adding a long-forgotten setting, etc.  
For this case, no specific version tagging is necessary.

**A new feature is added to Serverless or Elastic Cloud, how do I tag it?**  
Cumulative documentation is not meant to replace release notes. If a feature becomes available in Serverless and doesn’t have a particular lifecycle state to call out (preview, beta, deprecated…), it does not need specific tagging.

However, in this scenario, it is important to consider carefully [when the change is going to be published](#choose-the-docs-deployment-model-for-a-repository).

We do not do date-based tagging for unversioned products.

**Feature lifecycle**

* For unversioned products (typically Serverless and Elastic Cloud Hosted):  
  Unversioned products aren’t following a fixed versioning scheme and are released a lot more often than versioned products. All users are using the same version of this product.  
  * When a change is released in GA, it doesn’t need any specific tagging.  
  * When a change is introduced as preview or beta, use preview or beta as value for the corresponding key within the `applies_to`:


```yml
applies_to:
  serverless: preview
```
    

  * When a change introduces a deprecation, use deprecated as value for the corresponding key within the `applies_to`:

```yml
applies_to:
  deployment:
    ess: deprecated
```

    

  * When a change removes a feature, **remove the content.**   
    Exception: If the content also applies to another context (for example a feature is removed in both Kibana 9.x and Serverless), then it must be kept for any user reading the page that may be using a version of Kibana prior to the removal. For example:

```yml
applies_to:
  stack: deprecated 9.1, removed 9.4
  serverless: removed
```  

* For versioned products:

  * When a change is released in GA, users need to know which version the feature became available in:

```yml
applies_to:
  stack: ga 9.3
```

  * When a change is introduced as preview or beta, use preview or beta as value for the corresponding key within the `applies_to`:

```yml
applies_to:
  stack: beta 9.1
```
    

  * When a change introduces a deprecation, use deprecated as value for the corresponding key within the `applies_to`:

```yml
applies_to:
  deployment:
    ece: deprecated 4.2
```
    

  * When a change removes a feature**,** any user reading the page that may be using a version of Kibana prior to the removal must be aware that the feature is still available to them. For that reason, we do not remove the content, and instead mark the feature as removed:

```yml
applies_to:
  stack: deprecated 9.1, removed 9.4
```

    

**Identify multiple states for the same content**  
`applies_to` keys accept comma-separated values. For example:

* A feature is added in 9.1 as tech preview and becomes GA in 9.4: 

```yml
applies_to:
  stack: preview 9.1, ga 9.4
```


* A feature is deprecated in ECE 4.0 and is removed in 4.8. At the same time, it has already been removed in Elastic Cloud Hosted:

```yml
applies_to:
  deployment:
    ece: deprecated 4.0, removed 4.8
    ess: removed
```
  
**How do these tags behave in the output?**  
Applies\_to tags are rendered as badges in the documentation output. They reproduce the “key \+ lifecycle status \+ version” indicated in the sources.

Specifically for versioned products, badges will show “Planned” (if the lifecycle is preview, beta, or ga), “Deprecation planned” (if the lifecycle is deprecated), or “Removal planned” (if the lifecycle is removed) when the `applies_to` key specifies a product version that has not been released to our customers yet.

This is computed at build time (there is a docs build every 30 minutes). The documentation team tracks and maintains released versions for these products centrally.

When multiple lifecycle statuses and versions are specified in the sources, several badges are shown.

**Note**: Visuals and wording in the output documentation are subject to changes and optimizations.