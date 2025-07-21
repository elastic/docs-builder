---
navigation_title: Contribute locally (Elasticsearch)
---

# Contribute locally: Elasticsearch quickstart

The Elasticsearch APIs are the foundation of the Elastic Stack and the largest API set we maintain. Because the workflow is quite complex, we created this quickstart guide to help you get started.

This is a step-by-step local development workflow. While CI runs these steps automatically on PR branches in the `elasticsearch-specification` repo (see [Makefile](https://github.com/elastic/elasticsearch-specification/blob/main/Makefile)), working locally enables you to validate, preview and debug before submitting your changes. For a complete list of available make targets, run `make help`.

For the official Elasticsearch specification contribution guidance, see [`CONTRIBUTING.md`](https://github.com/elastic/elasticsearch-specification/blob/main/CONTRIBUTING.md#contributing-to-the-elasticsearch-specification).

:::::{stepper}

::::{step} Prepare your environment

Run this command to set up your Node.js environment:

```shell
nvm use 
```
If you don't have Node.js installed, refer to the [setup guide](https://github.com/elastic/elasticsearch-specification/tree/main?tab=readme-ov-file#prepare-the-environment).
::::

::::{step} Clone the specification repo
```shell
git clone https://github.com/elastic/elasticsearch-specification.git
cd elasticsearch-specification
```
:::{warning}
You must [create PRs from a branch](https://github.com/elastic/elasticsearch-specification/blob/main/CONTRIBUTING.md#send-your-pull-request-from-a-branch) in the `elasticsearch-specification` repo, not a fork.
:::
::::

::::{step} Install dependencies
```shell
make setup
```

:::{important}
You should run `make setup` every time you begin work on a contribution, because the `elasticsearch-specification` repository is under active development. This ensures you have the latest dependencies and tools.
:::

::::

::::{step} Make your docs changes
Edit the relevant TypeScript files in the `specification` directory. Use JSDoc comments to describe your API interfaces, following the [guidelines](./guidelines.md). Add or update summaries, descriptions, tags, metadata, links, and examples as needed.

:::{important}
If you're adding a new API, you must first create a REST API specification file in the [`specification/_json_spec`](https://github.com/elastic/elasticsearch-specification/tree/main/specification/_json_spec) directory.
:::

::::{step} Format, generate and validate your changes
```shell
make contrib
```
This command runs multiple steps in sequence:

1. Formats your code (`spec-format-fix`)
2. Generates the schema JSON (`generate`)
3. Transforms to OpenAPI format for language clients (`transform-to-openapi`)
4. Filters for serverless (`filter-for-serverless`)
5. Lints the docs (`lint-docs`)

:::{tip}
If you want to write great docs, you should fix all linter warnings and not just errors. Soon we will make this a requirement!
:::
::::

::::{step} Generate docs-specific OpenAPI files
```shell
make transform-to-openapi-for-docs
```
Generates the OpenAPI files specifically for docs purposes. This step also runs `generate-language-examples` to autogenerate examples for the various language clients and `curl`.

:::{note}
Be careful, the `transform-to-openapi` command (run by `make contrib`) is used for client libraries and does not generate the JSON schema files needed for docs purposes.
:::
::::

::::{step} Apply overlays
```shell
make overlay-docs
```

::::{step} Preview your changes
Generate a preview of how your docs will appear:
```shell
npm install -g bump-cli
bump preview output/openapi/elasticsearch-openapi-docs-final.json # Preview Elasticsearch API docs
bump preview output/openapi/elasticsearch-serverless-openapi-docs-final.json # Preview Elasticsearch serverless API docs
```
This creates a temporary URL hosted by Bump to preview your changes and share with others.
::::

::::{step} Open a pull request

Once you're satisfied with your docs changes:
1. Create a pull request from a branch on your local clone
2. The CI will validate your OpenAPI specs
::::

:::::