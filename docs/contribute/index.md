---
navigation_title: Contribute
---

# Elastic Docs contribution guide

Welcome, **contributor**!

Whether you're a technical writer or you've only edited Elastic docs once or twice, you're a valued contributor. Every word matters!

## Contribute to the docs [#contribute]

The version of the docs you want to contribute to determines the tool and syntax you must use to update the docs.

### Contribute to Elastic Stack version 8.x docs and earlier

To contribute to earlier versions of the Elastic Stack, you must work with our [legacy documentation build system](https://github.com/elastic/docs). This system uses AsciiDoc as it's authoring format.

* For **simple bugfixes and enhancements** --> [Contribute on the web](on-the-web.md)
* For **complex or multi-page updates** --> See the [legacy documentation build guide](https://github.com/elastic/docs?tab=readme-ov-file#building-documentation)

### Contribute to Elastic Stack version 9.0 docs and later

* For **simple bugfixes and enhancements** --> [contribute on the web](on-the-web.md)
* For **complex or multi-page updates** --> [Contribute locally](locally.md)

Starting with Elastic Stack version 9.0, a new set of docs is no longer published for every minor release. Instead, each page stays valid over time and incorporates version-specific changes directly within the content using a [cumulative approach](#cumulative-docs).

#### Write cumulative documentation [#cumulative-docs]

Cumulative documentation means that one page can cover multiple product versions, deployment types, and release stages. Instead of creating separate pages for each release, we update the same page with version-specific details. 

Use the [`applies_to`](../syntax/applies.md) tag to indicate which versions, deployment types, or release stages each part of the content is relevant to.

You must always use the `applies_to` tag at the [page](../syntax/applies.md#page-annotations) level. Optionally, you can also use it at the [section](../syntax/applies.md#sections) or [inline](../syntax/applies.md#inline-applies-to) level if certain parts of the content apply only to specific versions, deployment types, or release stages.

## Report a bug

* It's a **documentation** problem --> [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml) *or* [Fix it myself](locally.md)
* It's a **build tool (docs-builder)** problem --> [Open a bug report](https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml)

## Request an enhancement

* Make the **documentation** better --> [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml)
* Make our **build tool (docs-builder)** better --> [Start a docs-builder discussion](https://github.com/elastic/docs-builder/discussions)

## Work on docs-builder

That sounds great! See [development](../development/index.md) to learn how to contribute to our documentation build system.