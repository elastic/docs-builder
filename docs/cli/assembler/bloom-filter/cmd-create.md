Generates a bloom filter that gets embedded into the `docs-builder` binary.

This bloom filter is used to determine whether a document's `mapped_page` frontmatter field exists in the [legacy-url-mappings](../../../configure/site/legacy-url-mappings.md) project. The result controls how the document history selector is populated.
