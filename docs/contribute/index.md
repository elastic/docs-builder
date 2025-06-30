---
navigation_title: Contribute
---

# Elastic Docs contribution guide

Welcome, **contributor**!

Whether you're a technical writer or you've only edited Elastic docs once or twice, you're a valued contributor. Every word matters!

## Contribute to the docs [#contribute]

In April 2025, we released our new documentation site. This site includes documentation for our latest product versions, including {{stack}} 9.0+ and {{serverless-full}}.

:::{include} _snippets/two-systems.md
:::

### Contribute elastic.co/docs

:::{include} _snippets/docs-intro.md
:::

* For **simple bug fixes and enhancements**: [Contribute on the web](on-the-web.md)
* For **complex or multi-page updates**: [Contribute locally](locally.md)

#### Branches in V3

In Docs V3, a single branch is published per repository. This branch is set to `main` by default, but it is possible to instead publish a different branch by changing your repository's deployment model. You might want to change your deployment model so you can have more control over when content added for a specific release is published.

[Learn more about branching strategies](branching-strategy.md).

### Contribute to elastic.co/guide

:::{include} _snippets/guide-intro.md
:::

* For **simple bug fixes and enhancements**: [Contribute on the web](on-the-web.md)
* For **complex or multi-page updates**: See the [legacy documentation build guide](https://github.com/elastic/docs?tab=readme-ov-file#building-documentation)

## Report a bug

* It's a **documentation** problem: [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml) *or* [Fix it myself](locally.md). You can open sensitive issues in our [internal repo](https://github.com/elastic/docs-content-internal/issues/new/choose).
* It's a **build tool (docs-builder)** problem: [Open a bug report](https://github.com/elastic/docs-builder/issues/new?template=bug-report.yaml)

## Request an enhancement or documentation for a new feature

* Make the **documentation** better: [Open a docs issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml). You can open sensitive issues in our [internal repo](https://github.com/elastic/docs-content-internal/issues/new/choose).
* Make our **build tool (docs-builder)** better: [Start a docs-builder discussion](https://github.com/elastic/docs-builder/discussions)

## Work on docs-builder

That sounds great! See [development](../development/index.md) to learn how to contribute to our documentation build system.