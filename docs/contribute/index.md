---
navigation_title: Contribute
---

# Elastic Docs contribution guide

Welcome, **contributor**!

Whether you're a technical writer or you've only edited Elastic docs once or twice, you're a valued contributor. Every word matters!

## Contribute to the docs [#contribute]

The version of the docs you want to contribute to determines the tool and syntax you must use to update the docs. The easiest way to find the source file for the page you want to update is to navigate to the page on the web and click **Edit this page**. This will take you to the source file in the correct repo. For more detailed instructions, see [Contribute on the web](on-the-web.md).

### Contribute to version `8.18`, `ECE 3.8`, and `ECK 2.0` docs and earlier

To contribute to earlier versions of the documentation, you must work with our [legacy documentation build system](https://github.com/elastic/docs). This system uses the [AsciiDoc](https://asciidoc.org) markup language. 

* For **simple bugfixes and enhancements** --> [Contribute on the web](on-the-web.md)
* For **complex or multi-page updates** --> See the [legacy documentation build guide](https://github.com/elastic/docs?tab=readme-ov-file#building-documentation)

If you need to update documentation for both 8.x and 9.x, you will have to work in two systems. See [What if I need to update both 8.x and 9.x docs?](https://docs-v3-preview.elastic.dev/elastic/docs-builder/tree/main/contribute/on-the-web#what-if-i-need-to-update-both-8.x-and-9.x-docs) for the processes on updating docs across different versions.

### Contribute to version `9.0+`, `ECE 4.0+`, and `ECK 3.0+` docs and later

To contribute to `9.0+`, `ECE 4.0+`, and `ECK 3.0+`  content, you need to work with our new documentation system. This system uses custom [Markdown syntax](../syntax/index.md).
This content lives in one of three places:

1. **Reference/settings content** lives in the new Markdown files in the code repo as it did pre 9.0. This enables folks to make updates that touch code and low-level docs in the same PR.  
2. **Narrative/conceptual and overview content** lives in [elastic/docs-content](https://github.com/elastic/docs-content). For example, the [ES|QL overview](https://docs-v3-preview.elastic.dev/elastic/docs-content/tree/main/explore-analyze/query-filter/languages/esql) lives in the **Explore & Analyze** section of the narrative docs. But the reference documentation lives in the Elasticsearch reference section of the docs: [ES|QL reference](https://docs-v3-preview.elastic.dev/elastic/elasticsearch/tree/main/reference/query-languages/esql)  
3. **API reference docs** live in the different OpenAPI spec repositories. This is where you need to update API docs published in the new API docs system.

When you are ready to edit the content:

* For **simple bugfixes and enhancements** --> [contribute on the web](on-the-web.md)
* For **complex or multi-page updates** --> [Contribute locally](locally.md)

## Report a bug

* It's a **documentation** problem --> [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml) *or* [Fix it myself](locally.md)
* It's a **build tool (docs-builder)** problem --> [Open a bug report](https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml)

## Request an enhancement

* Make the **documentation** better --> [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml)
* Make our **build tool (docs-builder)** better --> [Start a docs-builder discussion](https://github.com/elastic/docs-builder/discussions)

## Work on docs-builder

That sounds great! See [development](../development/index.md) to learn how to contribute to our documentation build system.

