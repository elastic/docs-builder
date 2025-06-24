---
navigation_title: Contribute
---

# Elastic Docs contribution guide

Welcome, **contributor**!

Whether you're a technical writer or you've only edited Elastic docs once or twice, you're a valued contributor. Every word matters!

## Contribute to the docs [#contribute]

In April 2025, we released our new documentation site. This site includes documentation for our latest product versions, including {{stack}} 9.0+ and {{serverless-full}}.

The way that you contribute to the docs depends on the product version.

For a list of versions covered on [elastic.co/docs](https://www.elastic.co/docs), refer to [Find docs for your product version](/get-started/versioning-availability.md#find-docs-for-your-product-version). Versions previous to the versions listed on that page are documented on our legacy system, [elastic.co/guide](https://www.elastic.co/guide). 

:::{tip}
Unversioned products, such as {{ech}} and {{serverless-full}}, are documented on [elastic.co/docs](https://www.elastic.co/docs).
:::

### Contribute to elastic.co/guide

To contribute to elastic.co/guide, you must work with our [legacy documentation build system](https://github.com/elastic/docs). This system uses ASCIIDoc as its markup language.

Docs for {{stack}} 8.x, {{ece}} 3.x, and {{eck}} 2.x can all be found on this site.

* For **simple bug fixes and enhancements**: [Contribute on the web](on-the-web.md)
* For **complex or multi-page updates**: See the [legacy documentation build guide](https://github.com/elastic/docs?tab=readme-ov-file#building-documentation)

### Contribute elastic.co/docs

elastic.co/docs uses our new build system, also known as "Docs V3", which uses an extended version of markdown as its markup language. Refer to our [syntax guide](syntax.md) to learn more.

Docs for {{stack}} 9.x, {{ece}} 4.x, and {{eck}} 3.x can all be found on this site. All unversioned products, such as {{ech}} and {{serverless-full}}, are also documented on elastic.co/docs.

This documentation is **cumulative**. This means that a new set of docs is not published for every minor release. Instead, each page stays valid over time and incorporates version-specific changes directly within the content. [Learn how to write cumulative documentation](cumulative-docs.md).

* For **simple bug fixes and enhancements**: [contribute on the web](on-the-web.md)
* For **complex or multi-page updates**: [Contribute locally](locally.md)

#### Branches in V3

In Docs V3, a single branch is published per repository. This branch is set to `main` (or `master`) by default, but it is possible to instead publish a different branch by changing your repository's deployment model. You might want to change your deployment model so you can have more control over when content added for a specific release is published.

[Learn more about deployment models](deployment-models.md).

## Report a bug

* It's a **documentation** problem: [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml) *or* [Fix it myself](locally.md)
* It's a **build tool (docs-builder)** problem: [Open a bug report](https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml)

## Request an enhancement

* Make the **documentation** better: [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml)
* Make our **build tool (docs-builder)** better: [Start a docs-builder discussion](https://github.com/elastic/docs-builder/discussions)

## Work on docs-builder

That sounds great! See [development](../development/index.md) to learn how to contribute to our documentation build system.