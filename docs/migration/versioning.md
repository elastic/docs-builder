# New versioning

As part of the new information architecture, pages with varying versioning schemes are now interwoven, creating the opportunity and necessity to rethink the scope and versioning of each page. The previous approach of creating entirely separate docs sets for every minor version resulted in fragmentation and unnecessary duplication. Consolidating versioning resolves these issues while maintaining clarity and usability.

To ensure a seamless experience for users and contributors, the new versioning approach adheres to the following:

* **Context awareness**: Each page explicitly states the context it applies to, including relevant deployment types (e.g., Elastic Cloud Hosted and Elastic Cloud Serverless) and versions. This is communicated using [`applies_to` tags](/syntax/applies.md). Context clarity ensures users know if the content is applicable to their environment. When users land on a Docs page that doesn’t apply to their version or deployment type, clear cues and instructions will guide them to the appropriate content.
* **Version awareness**: The documentation team tracks and maintains released versions for all products in a central configuration file.
* **Simplified contributor workflow**: For pages that apply to multiple versions or deployment types, we’ve optimized the contributor experience by reducing complexity. Contributors can now manage multi-context content with ease, without duplicating information or navigating confusing workflows.
  
In this new system, documentation is written **cumulatively**. This means that a new set of docs is not published for every minor release. Instead, each page stays valid over time and incorporates version-specific changes directly within the content. [Learn how to write cumulative documentation](/contribute/cumulative-docs.md).

## Tag processing

:::{include} /contribute/_snippets/tag-processing.md
:::

## Branching strategy

In Docs V3, a single branch is published per repository. This branch is set to `main` by default, but it is possible to instead publish a different branch by changing your repository's branching strategy. You might want to change your branching strategy so you can have more control over when content added for a specific release is published. 

* [Learn how to choose the right branching strategy for your repository](/contribute/branching-strategy.md)
* [Learn how to set up your selected branching strategy](/configure/content-sources.md)
