---
title: Reference docs guidelines
---

## Engineering ownership of reference documentation

As part of the transition to Elastic Docs v3, responsibility for maintaining reference documentation will reside with Engineering teams so that code and corresponding documentation remain tightly integrated, allowing for easier updates and greater accuracy.

After migration, all narrative and instructional documentation actively maintained by writers will move to the elastic/docs-content repository. Reference documentation, such as API specifications, will remain in the respective product repositories so that Engineering teams can manage both the code and its related documentation in one place.

## API documentation guidelines

To improve consistency and maintain high-quality reference documentation, all API documentation must adhere to the following standards:

* **Switch to OAS (OpenAPI specification)**: Engineering teams should stop creating AsciiDoc-based API documentation. All API documentation should now use OAS files, alongside our API documentation that lives at elastic.co/docs/api.
* **Comprehensive API descriptions**: Ensure that OAS files include:
  * API descriptions
  * Request descriptions
  * Response descriptions
* **Fix linting warnings**: Address all new and existing linting warnings in OAS files to maintain clean and consistent documentation.
