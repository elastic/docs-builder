# New versioning

As part of the new information architecture, pages with varying versioning schemes are now interwoven, creating the opportunity and necessity to rethink the scope and versioning of each page. The previous approach of creating entirely separate docs sets for every minor version resulted in fragmentation and unnecessary duplication. Consolidating versioning resolves these issues while maintaining clarity and usability.

To ensure a seamless experience for users and contributors, the new versioning approach adheres to the following:

* **Context awareness**: Each page explicitly states the context it applies to, including relevant deployment types (e.g., Elastic Cloud Hosted and Elastic Cloud Serverless) and versions. This is communicated using [`applies_to` tags](/syntax/applies.md). Context clarity ensures users know if the content is applicable to their environment. When users land on a Docs page that doesn’t apply to their version or deployment type, clear cues and instructions will guide them to the appropriate content.
* **Version awareness**: The documentation team tracks and maintains released versions for all products in a central configuration file.
* **Simplified contributor workflow**: For pages that apply to multiple versions or deployment types, we’ve optimized the contributor experience by reducing complexity. Contributors can now manage multi-context content with ease, without duplicating information or navigating confusing workflows.
  
In this new system, documentation is written **cumulatively**. This means that a new set of docs is not published for every minor release. Instead, each page stays valid over time and incorporates version-specific changes directly within the content. [Learn how to write cumulative documentation](cumulative-docs.md).

## Tag processing

`applies_to` tags are rendered as badges in the documentation output. They reproduce the "key + lifecycle status + version" indicated in the content sources.

Specifically for versioned products, badges will display differently when the applies_to key specifies a product version that has not been released to our customers yet.

* `Planned` (if the lifecycle is preview, beta, or ga)
* `Deprecation planned` (if the lifecycle is deprecated)
* `Removal planned` (if the lifecycle is removed) 

This is computed at build time (there is a docs build every 30 minutes). The documentation team tracks and maintains released versions for these products centrally.

% todo: link

## Deployment models

In Docs V3, a single branch is published per repository. This branch is set to `main` (or `master`) by default, but it is possible to instead publish a different branch by changing your repository's deployment model. You might want to change your deployment model so you can have more control over when content added for a specific release is published. 

* [Learn how to choose the right deployment model for your repository](/contribute/deployment-models.md)
* [Learn how to set up your selected deployment model](/configure/deployment-models.md)