---
title: Reference docs guidelines
---

## Engineering ownership of reference documentation

As part of the transition to Elastic Docs v3, responsibility for maintaining reference documentation will reside with Engineering teams so that code and corresponding documentation remain tightly integrated, allowing for easier updates and greater accuracy.

After migration, all narrative and instructional documentation actively maintained by writers will move to the [elastic/docs-content](https://github.com/elastic/docs-content) repository. Reference documentation, such as API specifications, settings, and language references, will remain in the respective product repositories so that Engineering teams can manage both the code and its related documentation in one place.

## API documentation guidelines

To improve consistency and maintain high-quality reference documentation, all API documentation must adhere to the following standards:

* **OpenAPI source files**: Engineering teams should stop creating AsciiDoc-based API documentation. All API documentation will be derived from OpenAPI documents and published on [elastic.co/docs/api](https://www.elastic.co/docs/api/).
* **Accurate and complete content**: OpenAPI documents must include:
  * API descriptions
  * Request, response, parameter, and property descriptions
  * Valid examples
* **Stylistically consistent content**: Refer to API guidelines for consistency (reach out to #docs team for the current internal link).
Fix linting warnings: Address all new and existing linting warnings in OpenAPI files to maintain clean and consistent documentation.
* **Automatically linted**: Use the shared linting rules and address all new and existing linting warnings to maintain clean and consistent documentation.
