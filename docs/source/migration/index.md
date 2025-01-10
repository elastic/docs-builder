---
title: "Migration to docs-builder: Key updates & timeline"
navigation_title: Migration
---

:::{tip}
Want to learn _how_ to migrate? See the [migration guide](./guide.md).
:::

## Documentation Freeze

During the documentation freeze, the Docs team will focus almost entirely on migration tasks to ensure all content is successfully migrated and will handle only emergency documentation requests and release-related activities. When the migration is complete, writers will address documentation requests needed during the documentation freeze, ensuring that updates align with the new information architecture and format.

To make the transition to Elastic Docs v3 as smooth as possible, we’ve established a process to track and manage documentation requests during the migration:

* Open an issue in [elastic/docs-content](https://github.com/elastic/docs-content/issues), including details about the documentation requirements, links to resources, and drafts of documentation in GDocs.
* Post in [#docs](https://elastic.slack.com/archives/C0JF80CJZ) for emergency documentation requests.

During the documentation freeze, maintaining consistency and avoiding conflicts is key. To prevent documentation changes from merging during the documentation freeze, [codeowners](./codeowner.md) are being added to /docs directories for public-facing documentation in all relevant repositories.

## Improved information architecture

The improved information architecture fundamentally transforms how we organize and present Elastic Docs. By addressing longstanding challenges—such as fragmented content across many books and duplicated information—this new structure introduces a cohesive framework that emphasizes clarity, usability, and alignment with user goals. These updates are designed to create a seamless experience for both readers and contributors, fostering greater understanding of our products and their benefits.

The new IA design does the following:

* Provides a clear narrative pathway for users to follow, including new topics that compare similar technologies and features.
* Organizes content by user goal and role.
* Consolidates content previously duplicated across our books, including serverless and stateful content, and many tasks that are common across deployment types and solutions.
* Explains the context a topic applies to (deployment type, version) - see Consolidated versioning below for more information.
* Separates reference content into its own section for easy access.

To learn more:

* Explore the new IA in detail in our [working doc](https://docs.google.com/spreadsheets/d/1LfPI3TZqdpONGxOmL8B8V-Feo1flLwObz9_ibCEMkIQ/edit?gid=502629814#gid=502629814).
* Learn about the general shape of the IA in our [expandable-collapsible view](https://checkvist.com/p/Nur1EAtMopm5gxry5AncM5) (does not represent all pages or guarantee final page locations).
* For more context on our IA design, including our guiding principles, refer to our [IA plan deck](https://docs.google.com/presentation/d/1e1QtEtLVCoFX0kCj02mkwxBrLSaCFyd8Nu6UlJkLP2c/edit#slide=id.g217776b7fee_0_916).

## Consolidated versioning

As part of the new information architecture, pages with varying versioning schemes are now interwoven, creating the opportunity and necessity to rethink the scope and versioning of each page. The previous approach of creating entirely separate docs sets for every minor version resulted in fragmentation and unnecessary duplication. Consolidating versioning resolves these issues while maintaining clarity and usability.

To ensure a seamless experience for users and contributors, the new versioning approach adheres to the following:

Context awareness — Each page explicitly states the context it applies to, including relevant deployment types (e.g., Elastic Cloud Hosted and Elastic Cloud Serverless) and versions. Context clarity ensures users know if the content is applicable to their environment. When users land on a Docs page that doesn’t apply to their version or deployment type, clear cues and instructions will guide them to the appropriate content.
Simplified contributor workflow — For pages that apply to multiple versions or deployment types, we’ve optimized the contributor experience by reducing complexity. Contributors can now manage multi-context content with ease, without duplicating information or navigating confusing workflows.

For versioning plan details, check [Docs Versioning plan](https://docs.google.com/presentation/d/1fX8YBGcFlHJPi1kVfB9tC-988iUvxZJAZiH21kE4A5M/edit#slide=id.g319e4ce75b5_0_0).

To learn how to callout versioning differences in docs-builder, see [product availability](../syntax/applies.md).

## Transition from AsciiDoc to Markdown

With the migration to Elastic Docs v3, the primary format for all Elastic Docs is transitioning from AsciiDoc to Markdown. Why Markdown? Markdown is already an industry standard across the industry, and 90% of Elastic developers are comfortable working with Markdown syntax [[source](https://docs.google.com/presentation/d/1morhFX4tyVB0A2f1_fnySzeJvPYf0kXGjVVYU_lVRys/edit#slide=id.g13b75c8f1f3_0_463)].

## How does this impact teams with automatically generated Docs?

For teams that generate documentation programmatically, the transition means automatically generated files must now be output in Markdown format instead of AsciiDoc. This adjustment will require updating documentation generation pipelines, but it aligns with the broader benefits of a simpler and more extensible documentation framework.

See our [syntax guide](../syntax/index.md) to learn more about the flavor of Markdown that we support. In addition, we're refining support for including YAML files directly in the docs. See [automated settings](../syntax/automated_settings.md) to learn more.

## Engineering ownership of reference documentation

As part of the transition to Elastic Docs v3, responsibility for maintaining reference documentation will reside with Engineering teams so that code and corresponding documentation remain tightly integrated, allowing for easier updates and greater accuracy.

After migration, all narrative and instructional documentation actively maintained by writers will move to the elastic/docs-content repository. Reference documentation, such as API specifications, will remain in the respective product repositories so that Engineering teams can manage both the code and its related documentation in one place.

## Guidelines for API documentation
To improve consistency and maintain high-quality reference documentation, all API documentation must adhere to the following standards:

* **Switch to OAS (OpenAPI specification)**: Engineering teams should stop creating AsciiDoc-based API documentation. All API documentation should now use OAS files, alongside our API documentation that lives at elastic.co/docs/api.
* **Comprehensive API descriptions**: Ensure that OAS files include:
  * API descriptions
  * Request descriptions
  * Response descriptions
* **Fix linting warnings**: Address all new and existing linting warnings in OAS files to maintain clean and consistent documentation.