---
navigation_title: "Example scenarios"
---

# Example scenarios

## Code block content varies [code-block]

### Solution A: Use a code callout [code-block-solution-a]

**When to use a code callout**:

**Best practices**:

**Example**:

### Solution B: Use tabs [code-block-solution-b]

**When to use tabs**:

**Best practices**:

**Example**:

## Workflows vary [workflow]

When one or more steps in a process differs.

### Solution A: Use tabs [workflow-solution-a]

Tabs are the least disruptive ...
Use tabs to include workflows that vary between deployment types, versions, and more.

**When to use tabs**:

* If all the tabs fit horizontally on a single row on a typical laptop screen.
  This is usually around a maximum of four tabs.
* If the tab with the most content fits vertically on a typical laptop screen.
  This is usually around of 20 lines.

**Best practices**:

* Try to minimize the number of tabs where possible. Try to work around small differences by
  rewording or adding context in prose or in `note` style admonitions.
* Try not to include information surrounding a procedure in the tabs.
  Make the tab content as small as possible apart from the procedure itself.
* Consider breaking up procedures into sets of procedures if only one section differs between contexts.

**Example**:

:::::{tab-set}
:group: serverless-stack

::::{tab-item} {{serverless-short}}
:sync: serverless

1. Click **Create space** or select the space you want to edit.
2. Provide:

    * A meaningful name and description for the space.
    * A URL identifier. The URL identifier is a short text string that becomes part of the Kibana URL. Kibana suggests a URL identifier based on the name of your space, but you can customize the identifier to your liking. You cannot change the space identifier later.

3. Customize the avatar of the space to your liking.
4. Save the space.
::::

::::{tab-item} {{stack}} v9
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

### Solution B: Use headings [workflow-solution-b]

**When to use headings**:

**Best practices**:

**Example**:

### Solution C: Use sibling pages [workflow-solution-c]

**When to use sibling pages**:

**Best practices**:

**Example**:

## Screenshots vary [screenshot]

### Solution A: Use tabs [screenshot-solution-a]

**When to use tabs**:

**Best practices**:

**Example**:

### Solution B: Add a note [screenshot-solution-b]

**When to use a note**:

**Best practices**:

**Example**:

## Multiple adjacent block elements vary [multiple-block]

### Solution A: Use headings [multiple-block-solution-a]

**When to use headings**:

**Best practices**:

**Example**:

### Solution B: Use tabs [multiple-block-solution-b]

**When to use tabs**:

**Best practices**:

**Example**:

## Feature in beta or technical preview is removed [beta-removed]

### Solution [beta-removed-solution]

% ## Tabs
%
% **Use case:** When one or more steps in a process differs, put the steps into tabs - one for each process.
%
% **Number to use:** Max 4 or 5 (in a deployment type / versioning context - might be different for other situations)
%
% Try to minimize the number of tabs where you can - try to work around small differences by rewording or adding context in prose or in `note` style admonitions. Check out the [prose](#prose) guidelines.
%
% Try not to include information surrounding a procedure in the tabs. Make the tab content as small as possible apart from the procedure itself.
%
% Consider breaking up procedures into sets of procedures if only one section differs between contexts.
%
% :::{tip}
% For consistency, use [substitutions](/syntax/substitutions.md) for the tab labels.
% :::
%
% ### Examples
%
% To create a space:
%



You can edit all of the space settings you just defined at any time, except for the URL identifier.

% ## Sibling bullets
%
% **Use case:** Requirements, limits, other simple, mirrored facts.
%
% **Number to use:** Ideal is one per option (e.g. one per deployment type). You might consider nested bullets in limited cases.
%
% ### Examples
%
% ::::{tab-set}
% :group: one-two
%
% :::{tab-item} One
% :sync: one
%
% #### Required permissions
%
% * **{{serverless-short}}**: `Admin` or equivalent
% * **{{stack}} v9**: `kibana_admin` or equivalent
%
% :::
% :::{tab-item} Two
% :sync: two
%
% #### Create a space
%
% The maximum number of spaces that you can have differs by [what do we call this]:
%
% * **{{serverless-short}}**: Maximum of 100 spaces.
% * **{{stack}} v9**: Controlled by the `xpack.spaces.maxSpaces` setting. Default is 1000.
% :::
% ::::
%
% ## Callouts
%
% **Use case:** Happy differences, clarifications
%
% Some sections don’t apply to contexts like serverless, because we manage the headache for you. Help people to feel like they’re not getting shortchanged with a callout.
%
% If there’s a terminology change or other minor change (especially where x equates with y), consider handling as a note for simplicity.
%
% ### Examples
%
% * *In **Manage TLS certificates** section*:
%
%   :::{tip}
%   Elastic Cloud manages TLS certificates for you.
%   :::
%
% * *In a **Spaces** overview*:
%
%   :::{note}
%   In {{stack}} 9.0.0 and earlier, **Spaces** are referred to as **Places**.
%
%
% ## Prose
%
% **Use cases:**
% * When features in a list of features are exclusive to a specific context, or were introduced in a specific version
% * Requirements, limits, other simple, mirrored facts
% * Cases where the information isn’t wildly important, but nice to know, or to add basic terminology change info to overviews
% * Comparative overviews
% * Differences that are small enough or not significant enough to warrant an admonition or tabs or separate sections with front matter
%
% In some cases, you might want to add a paragraph specific to one version or another in prose to clarify behavior or terminology.
%
% In cases where there are significant differences between contexts, close explanation of the differences helps people to understand how something works, or why something behaves the way it does. Compare and contrast differences in paragraphs when the explanation helps people to use our features effectively.
%
% You also might do this to compare approaches between deployment types, when [sibling bullets](#sibling-bullets) aren't enough.
%
% ### Examples
%
% ::::{tab-set}
% :group: five-six-four-one-three
%
% :::{tab-item} Unique features
% :sync: five
%
% * Each space has its own saved objects.
% * Users can only access the spaces that they have been granted access to. This access is based on user roles, and a given role can have different permissions per space.
% * In {{stack}} 9.0.0+, each space has its own navigation.
%
% :::
%
% :::{tab-item} Unique reqs / limits
% :sync: six
%
% * In serverless, use `Admin` or equivalent
% * In {{stack}} 9.0.0+, use `kibana_admin` or equivalent
%
% OR
%
% The maximum number of spaces that you can have differs by [what do we call this]:
%
% * In serverless, you can have a maximum of 100 spaces.
% * In {{stack}} 9.0.0+, the maximum is controlled by the `xpack.spaces.maxSpaces` setting. Default is 1000.
% :::
%
% :::{tab-item} Nice-to-know
% :sync: four
%
% In {{stack}} 9.1.0 and earlier, **Spaces** were referred to as **Places**.
%
% OR
%
% If you're managing a {{stack}} v9 deployment, then you can also assign roles and define permissions for a space from the **Permissions** tab of the space settings.
% :::
%
% :::{tab-item} Comparative overviews
% :sync: one
%
% The way that TLS certificates are managed depends on your deployment type:
%
% In self-managed Elasticsearch, you manage both of these certificates yourself.
%
% In {{eck}}, you can manage certificates for the HTTP layer. Certificates for the transport layer are managed by ECK and can’t be changed. However, you can set your own certificate authority, customize certificate contents, and provide your own certificate generation tools using transport settings.
%
% In {{ece}}, you can use one or more proxy certificates to secure the HTTP layer. These certificates are managed at the ECE installation level. Transport-level encryption is managed by ECE and certificates can’t be changed.
% :::
%
% :::{tab-item} Comparative overviews II
% :sync: three
%
% **Managed security in Elastic Cloud**
%
% Elastic Cloud has built-in security. For example, HTTPS communications between Elastic Cloud and the internet, as well as inter-node communications, are secured automatically, and cluster data is encrypted at rest.
%
% You can augment Elastic Cloud security features in the following ways:
%
% * Configure traffic filtering to prevent unauthorized access to your deployments.
% * Encrypt your deployment with a customer-managed encryption key.
% * Secure your settings with the Elasticsearch keystore.
% * Allow or deny Cloud IP ranges using Elastic Cloud static IPs.
%
% [Learn more about security measures in Elastic Cloud](https://www.elastic.co/cloud/security).
%
% :::
% ::::


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

### Examples

[Cloud Hosted deployment billing dimensions](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization/billing/cloud-hosted-deployment-billing-dimensions)

and its sibling

[{{serverless-short}} project billing dimensions](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/deploy-manage/cloud-organization/billing/serverless-project-billing-dimensions)
