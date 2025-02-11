---
navigation_title: "Content patterns"
applies:
  stack: all
  serverless: all
  hosted: all
  eck: all
  ece: all
subs:
  stack: "Elastic Stack"
  serverless-tab: "Elastic Cloud Serverless"
  stack-all-tab: "All Elastic Stack"
---
# Version content patterns

Depending on what you're trying to communicate, you can use the following patterns to represent version and deployment type differences in your docs.

Choose from:
% this should become a quick-ref table

* [Page-level `applies` tags](#page-level-applies-tags)
* [Section/heading-level `applies` tags](#sectionheading-level-applies-tags)
* [Inline `applies` tags](#inline-applies-tags) (currently just prose)
* [Tabs](#tabs)
* [Sibling bullets](#sibling-bullets)
* [Callouts](#callouts)
* [Prose (inline)](#prose-inline)
* [Prose (explanatory paragraphs and sections)](#prose-explanatory-paragraphs-and-sections)
* [Sibling pages](#sibling-pages)

## Page-level `applies` tags

*see [`applies`](/syntax/applies.md)*

**Use case:** Provide signals that a page applies to the reader

**Number to use:** Minimum to add clarity. See [basic concepts and principles](/versions/index.md#basic-concepts-and-principles). 

**Approach:**

Add tags for all **versioning facets** relevant to the page.

See [Versions and lifecycle states](/versions/index.md#versions-and-lifecycle-states) to learn when to specify versions in these tags.

### Example
[Manage your Cloud organization](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization)

### Section/heading-level `applies` tags
:::{applies}
:ece: all
:hosted: all
:eck: all
:stack: all
:::

*see [`applies`](/syntax/applies.md#sections)*

**Use case:** Provide signals about a section’s scope so a user can choose to read or skip it as needed

**Number to use:** Minimum to add clarity. See [basic concepts and principles](/versions/index.md#basic-concepts-and-principles).  

**When to use:** When the section-level scope differs from the page-level scope

**Example**
See above

## Inline `applies` tags

::::{warning}
This feature doesn't currently work - we're working around it by using prose.
::::

**Use case:** When features in a **list of features** are exclusive to a specific context, or were introduced in a specific version

**Number to use:** Three max. If the number of tags is longer than that, consider: 

*  Breaking up the list by facet
*  Using labels that don't have version / lifecycle information and deferring that information to the page or section for the feature
  
Currently, we don't have inline `applies` tags. Instead, append the information to the bullet item in bolded square brackets `**[]**`. Add all information within a single set of square brackets. The brackets should appear after any final punctuation. 

Because this approach is less clear, consider using words like `only` to help people to understand that this information indicates the applicability of a feature.

### Example

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


## Tabs

**Use case:** When one or more steps in a process differs, put the steps into tabs - one for each process.

**Number to use:** Max 4 or 5 (in a deployment type / versioning context - might be different for other situations)

Try to minimize the number of tabs where you can - try to work around small differences by rewording or adding context in prose or in `note` style admonitions:

* In version 9.1 and earlier, **Spaces** were referred to as **Places**. 
* 
  ::::{note}
  In version 9.1 and earlier, click the **Permissions** tab.
  ::::

Try not to include information surrounding a procedure in the tabs. Make the tab content as small as possible apart from the procedure itself.

Consider breaking up procedures into sets of procedures if only one section differs between contexts.

### Example

To create a space: 

:::::{tab-set}
:group: stack-serverless

::::{tab-item} {{serverless-tab}}
:sync: serverless

1. Click **Create space** or select the space you want to edit.
2. Provide:

    * A meaningful name and description for the space.
    * A URL identifier. The URL identifier is a short text string that becomes part of the Kibana URL. Kibana suggests a URL identifier based on the name of your space, but you can customize the identifier to your liking. You cannot change the space identifier later.

3. Customize the avatar of the space to your liking.
4. Save the space.
::::

::::{tab-item} {{stack-all-tab}}
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

## Sibling bullets 

**Use case:** Requirements, limits, other simple, mirrored facts.

**Number to use:** Ideal is one per option (e.g. one per deployment type). You might consider nested bullets in limited cases.

### Examples

::::{tab-set}
:group: one-two

:::{tab-item} One
:sync: one

#### Required permissions

* **Serverless**: `Admin` or equivalent
* **Stack v9**: `kibana_admin` or equivalent

:::
:::{tab-item} Two
:sync: two

#### Create a space

The maximum number of spaces that you can have differs by [what do we call this]: 

* **Serverless**: Maximum of 100 spaces.
* **Stack v9**: Controlled by the `xpack.spaces.maxSpaces` setting. Default is 1000.
:::
::::

## Callouts

**Use case:** Happy differences, clarifications

Some sections don’t apply to, say, Serverless, because we manage the headache for you. Help people to feel like they’re not getting shortchanged with a callout.

If there’s a terminology change or other minor change (especially where x equates with y), consider handling as a note for simplicity.

### Examples

* *In **Manage TLS certificates** section*:

  :::{tip}
  Elastic Cloud manages TLS certificates for you.
  :::

* *In a **Spaces** overview*:

  :::{note}
  In {{stack}} versions 9.0 and earlier, **Spaces** are referred to as **Places**.

## Prose (inline)

**Use case:** Clarifying or secondary information

**Number to use:** ~ once per section (use your judgement)

**When to use:** Cases where the information isn’t wildly important, but nice to know, or to add basic terminology change info to overviews

Sometimes, you can just preface a paragraph with version info. 

### Example

If you're managing a Stack v9 deployment, then you can also assign roles and define permissions for a space from the **Permissions** tab of the space settings. 

## Prose (explanatory paragraphs and sections)

**Use case**: Differences with a "why"

**When to use:** Comparative overviews

Sometimes, a close explanation of the differences between things helps people to understand how something works, or why something behaves the way it does. Compare and contrast differences in paragraphs when the explanation helps people to use our features effectively.

You also might do this to compare approaches between deployment types, when [sibling bullets](#sibling-bullets) aren't enough.

### Example

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

## Sibling pages

**Use case:**

* Processes that have significant differences **across multiple procedures**
* Chained procedures where not all of the procedures are needed for all contexts / where the flow across procedures is muddied when versioning context is added
* Very complex pages that are already heavily using tabs and other tools we use for versioning differences, making it hard to add another “layer” of content
* Beta to GA transitions? Hide the beta doc and leave it linked to the GA doc, which will presumably be more stable?

**Number to use:** As few as possible. Consider leveraging other ways of communicating versioning differences to reduce the number of sibling pages.

**When to use:**

When version differences are just too large to be handled with any of our other tools. Try to avoid creating sibling pages when you can.

% Down the line, if we need to, we could easily convert version-sensitive sibling pages into “picker” pages

### Example

[Cloud Hosted deployment billing dimensions](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization/billing/cloud-hosted-deployment-billing-dimensions.html)

and its sibling

[Serverless project billing dimensions](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization/billing/serverless-project-billing-dimensions.html)