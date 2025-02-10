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

:::{dropdown} Versioning facets
There are multiple facets to versioning that we need to consider: 

* **Elasticsearch / Kibana flavor:** The feature base used for basic functionality. Either **Serverless** (sometimes "Elastic Stack Serverless") or **Stack <version>**

% TODO: Final term for "Stack"
* **Deployment type:** The way Elastic is deployed. Either **Serverless** (sometimes "Elastic Stack Serverless"), **Elastic Cloud Hosted**, **Elastic Cloud on Kubernetes**, **Elastic Cloud Enterprise**, or **Self-managed**. 

  All deployment types other than **Serverless** use the **Stack <version>** flavor of Elasticsearch / Kibana.

% TODO: Final term for "Self-managed"

* **Project type:** The Serverless project types where a feature can be used.

* **Other versioning schemes:** Elastic products or tools with a versioned component, where stack versioning is not followed. 
  
  E.g. clients, Elastic Common Schema

**How many facets do I need to use?**

The role of these labels is providing a trust signal that the reader is viewing content that’s applicable to them. This means that the relevant labels should appear on all pages. However, we can choose to expose only one versioning plane on pages where only one plane is relevant:

* Depending on what you're documenting, you might need to include information from multiple facets. For example, when relevant features exist at both the stack and the deployment level, both of these groups might be used together (e.g. security, user management, trust).

* In some sections, such as **Explore and analyze**, features generally only differ by Elasticsearch flavor. In these cases, you can choose to include only this facet on the page.
:::

:::{dropdown} Defaults and hierarchy 

You should not assume a default deployment type or Elasticsearch / Kibana flavor.

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
:::

:::{dropdown} Versions and lifecycle states

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

:::


## Content patterns

Depending on what you're trying to communicate, you can use the following patterns to represent version and deployment type differences in your docs.

Choose from:
% this should become a quick-ref table

* [Page-level `applies` tags](#page-level-applies-tags)
* [Section/heading-level `applies` tags](#sectionheading-level-applies-tags)
* [Inline `applies` tags](#inline-applies-tags) (currently just prose)
* [Tabs](#tabs)
* [Sibling bullets](#sibling-bullets)
* [Prose (inline)](#prose-inline)
* [Callouts](#callouts)
* [Prose (explanatory paragraphs and sections)](#prose-explanatory-paragraphs-and-sections)
* [Sibling pages](#sibling-pages)

### Page-level `applies` tags

**Use case:** Provide signals that a page applies to the reader

**Number to use:** Minimum to add clarity. See [basic concepts and principles](#basic-concepts-and-principles). 

**Approach:**

Add tags for all **versioning facets** relevant to the page.

See **Versions and lifecycle states** to learn when to specify versions in these tags.

#### Example
[Manage your Cloud organization](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization.html)

### Section/heading-level `applies` tags
:::{applies}
:ece: all
:hosted: all
:eck: all
:stack: all
:::


**Use case:** Provide signals about a section’s scope so a user can choose to read or skip it as needed

**Number to use:** Minimum to add clarity. See [basic concepts and principles](#basic-concepts-and-principles).  

**When to use:** When the section-level scope differs from the page-level scope

**Example**
See above

### Inline `applies` tags

::::{warning}
This feature doesn't currently work - we're working around it by using prose.
::::

**Use case:** When features in a **list of features** are exclusive to a specific context, or were introduced in a specific version

**Number to use:** Three max. If the number of tags is longer than that, consider: 

*  Breaking up the list by facet
*  Using labels that don't have version / lifecycle information and deferring that information to the page or section for the feature
  
Currently, we don't have inline `applies` tags. Instead, append the information to the bullet item in bolded square brackets `**[]**`. Add all information within a single set of square brackets. The brackets should appear after any final punctuation. 

Because this approach is less clear, consider using words like `only` to help people to understand that this information indicates the applicability of a feature.

#### Example

Spaces let you organize your content and users according to your needs.

::::{tab-set}
:group: one-two

:::{tab-item} One
:sync: one

* Each space has its own saved objects.
* Users can only access the spaces that they have been granted access to. This access is based on user roles, and a given role can have different permissions per space.
* Each space has its own navigation. **[Stack v9 only]**

:::
:::{tab-item} Two
:sync: two


* Learn about internal users, which are responsible for the operations that take place inside an Elasticsearch cluster
* Learn about service accounts, which are used for integration with external services that connect to Elasticsearch
* Learn about the services used for token-based authentication
* Learn about the services used by orchestrators **[Elastic Cloud Hosted, Elastic Cloud Enterprise, Elastic Cloud on Kubernetes]**
* Learn about user lookup technologies
* Manage the user cache

:::
::::


### Tabs

**Use case:** When one or more steps in a process differs, put the steps into tabs - one for each process.

**Number to use:** Max 4 or 5

Try to minimize the number of tabs where you can - try to work around small differences by rewording or adding context in prose or in `note` style admonitions:

* In version 9.1 and earlier, **Spaces** were referred to as **Places**. 
* 
  ::::{note}
  In version 9.1 and earlier, click the **Permissions** tab.
  ::::

Try not to include information surrounding a procedure in the tabs. Make the tab content as small as possible apart from the procedure itself.

Consider breaking up procedures into sets of procedures if only one section differs between contexts.

#### Example

To create a space: 

:::::{tab-set}
:group: stack-serverless

::::{tab-item} Serverless
:sync: serverless

1. Click **Create space** or select the space you want to edit.
2. Provide:

    * A meaningful name and description for the space.
    * A URL identifier. The URL identifier is a short text string that becomes part of the Kibana URL. Kibana suggests a URL identifier based on the name of your space, but you can customize the identifier to your liking. You cannot change the space identifier later.

3. Customize the avatar of the space to your liking.
4. Save the space.
::::

::::{tab-item} Stack v9
:sync: stack

1. Select **Create space** and provide a name, description, and URL identifier.
   The URL identifier is a short text string that becomes part of the Kibana URL when you are inside that space. Kibana suggests a URL identifier based on the name of your space, but you can customize the identifier to your liking. You cannot change the space identifier once you create the space.

2. Select a **Solution view**. This setting controls the navigation that all users of the space will get:
   * **Search**: A light navigation menu focused on analytics and Search use cases. Features specific to Observability and Security are hidden.
   * **Observability**: A light navigation menu focused on analytics and Observability use cases. Features specific to Search and Security are hidden.
   * **Security**: A light navigation menu focused on analytics and Security use cases. Features specific to Observability and Search are hidden.
   * **Classic**: All features from all solutions are visible by default using the classic, multilayered navigation menus. You can customize which features are visible individually.

3. If you selected the **Classic** solution view, you can customize the **Feature visibility** as you need it to be for that space.

   :::{note} 
   Even when disabled in this menu, some Management features can remain visible to some users depending on their privileges. Additionally, controlling feature visibility is not a security feature. To secure access to specific features on a per-user basis, you must configure Kibana Security.
   :::
4. Customize the avatar of the space to your liking.
5. Save your new space by selecting **Create space**.
::::

:::::

You can edit all of the space settings you just defined at any time, except for the URL identifier.

### Sibling bullets 

**Use case:** Requirements, limits, other simple, mirrored facts.

**Number to use:** Ideal is one per option (e.g. one per deployment type). You might consider nested bullets in limited cases.

#### Examples

::::{tab-set}
:group: one-two

:::{tab-item} One
:sync: one

##### Required permissions

* **Serverless**: `Admin` or equivalent
* **Stack v9**: `kibana_admin` or equivalent

:::
:::{tab-item} Two
:sync: two

##### Create a space

The maximum number of spaces that you can have differs by [what do we call this]: 

* **Serverless**: Maximum of 100 spaces.
* **Stack v9**: Controlled by the `xpack.spaces.maxSpaces` setting. Default is 1000.
:::
::::

### Prose (inline)

**Use case:** Clarifying or secondary information

**Number to use:** ~ once per section (use your judgement)

**When to use:** Cases where the information isn’t wildly important, but nice to know, or to add basic terminology change info to overviews

Sometimes, you can just preface a paragraph with version info. 

#### Example

If you're managing a Stack v9 deployment, then you can also assign roles and define permissions for a space from the **Permissions** tab of the space settings. 

When a role is assigned to *All Spaces*, you can’t remove its access from the space settings. You must instead edit the role to give it more granular access to individual spaces.


### Callouts

**Use case:** Happy differences, clarifications

Some sections don’t apply to, say, Serverless, because we manage the headache for you. Help people to feel like they’re not getting shortchanged with a callout.

If there’s a terminology change or other minor change (especially where x equates with y), consider handling as a note for simplicity.

#### Examples

* *In **Manage TLS certificates** section*:

  :::{tip}
  Elastic Cloud manages TLS certificates for you.
  :::

* *In a **Spaces** overview*

  :::{note}
  In Stack versions 9.0 and lower, **Spaces** are referred to as **Places**.

### Prose (explanatory paragraphs and sections)

**Use case**: Differences with a "why"

**When to use:** Comparative overviews

Sometimes, a close explanation of the differences between things helps people to understand how something works, or why something behaves the way it does. Compare and contrast differences in paragraphs when the explanation helps people to use our features effectively.

You also might do this to compare approaches between deployment types, when [sibling bullets](#sibling-bullets) aren't enough.

#### Example

::::{tab-set}
:group: one-two

:::{tab-item} One
:sync: one

The way that TLS certificates are managed depends on your deployment type:

In **self-managed Elasticsearch**, you manage both of these certificates yourself. 

In **Elastic Cloud on Kubernetes**, you can manage certificates for the HTTP layer. Certificates for the transport layer are managed by ECK and can’t be changed. However, you can set your own certificate authority, customize certificate contents, and provide your own certificate generation tools using transport settings.

In **Elastic Cloud Enterprise**, you can use one or more proxy certificates to secure the HTTP layer. These certificates are managed at the ECE installation level. Transport-level encryption is managed by ECE and certificates can’t be changed.
:::

:::{tab-item} Two
:sync: two

**Managed security in Elastic Cloud**

Elastic Cloud has built-in security. For example, HTTPS communications between Elastic Cloud and the internet, as well as inter-node communications, are secured automatically, and cluster data is encrypted at rest. 

You can augment Elastic Cloud security features in the following ways: 

* Configure traffic filtering to prevent unauthorized access to your deployments. 
* Encrypt your deployment with a customer-managed encryption key. 
* Secure your settings with the Elasticsearch keystore. 
* Allow or deny Cloud IP ranges using Elastic Cloud static IPs.

[Learn more about security measures in Elastic Cloud](https://www.elastic.co/cloud/security).

:::
::::

You also might do this to compare approaches between deployment types, when [sibling bullets](#sibling-bullets) aren't enough.

### Sibling pages

**Use case:**

* Processes that have significant differences **across multiple procedures**
* Chained procedures where not all of the procedures are needed for all contexts / where the flow across procedures is muddied when versioning context is added
* Very complex pages that are already heavily using tabs and other tools we use for versioning differences, making it hard to add another “layer” of content
* Beta to GA transitions? Hide the beta doc and leave it linked to the GA doc, which will presumably be more stable?

**Number to use:** As few as possible. Consider leveraging other ways of communicating versioning differences to reduce the number of sibling pages.

**When to use:**

When version differences are just too large to be handled with any of our other tools. Try to avoid creating sibling pages when you can.

% Down the line, if we need to, we could easily convert version-sensitive sibling pages into “picker” pages

#### Example

[Cloud Hosted deployment billing dimensions](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization/billing/cloud-hosted-deployment-billing-dimensions.html)

and its sibling

[Serverless project billing dimensions](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization/billing/serverless-project-billing-dimensions.html)

## Best practices

### Screenshots

Follow these principles to use screenshots in our unversioned documentation system:

* Reduce screenshots when they don’t explicitly add value.
* When adding a screenshot, determine the minimum viable screenshot and whether it can apply to all relevant versions.
* Take and maintain screenshots for only the most recent version, with very few exceptions that should be considered on a case-by-case basis.
