# Content sources

To support multiple branching strategies for different repositories, we support the concept of a content source.

Next
:   The source for the upcoming documentation.

Current
:   The source for the active documentation.


Our publish environments are connected to a single content source.

| Publish Environment | Content Source |
|---------------------|----------------|
| Production          | `Current`      |
| Staging             | `Current`      |
| Edge                | `Next`         |

This allows you as an owner of a repository to choose two different branching strategies.

## Branching strategies

The new documentation system supports 2 branching strategies.

### Continuous deployment 

This is the default where a repositories `main` or `master` branch is continuously deployed to production.

### Tagged

Allows you to 'tag' a single git reference (typically a branch) as `current` which will be used to deploy to production. Allowing you to control the timing of when new documentation should go live.

### Add a new content set

To get started:

1. Follow our [guide](/migration/guide/index.md) to set up the new docs folder structure and CI configuration.
1. Configure the repository source in `assembler.yml`. See [](./site/content.md) for details.

  :::{note}
  The tagged branching strategy requires first following our [migration guide](/migration/guide/index.md) and configuring the documentation CI integration.
  Our CI integration checks will block until `current` is successfully configured.
  :::

1. Inject the content set into the global navigation. See [](./site/navigation.md) for details.
1. Optionally set up legacy URL mappings. See [](./site/legacy-url-mappings.md) for details.

#### CI configuration

To ensure repositories that use the [tagged branching strategy](#tagged) can be onboarded correctly, our CI integration needs appropriate `push` branch triggers and matching settings on the reusable workflows in [`elastic/docs-actions`](https://github.com/elastic/docs-actions).

Also add **`docs-deploy.yml`** and **`docs-preview-cleanup.yml`** as described in [How to set up docs previews](../migration/guide/how-to-set-up-docs-previews.md); the snippet below only highlights the tagged-branching-specific parts of **docs-build** (and the same `with:` block must be passed through on your **docs-deploy** consumer job).

```yml
name: docs-build

on:
  pull_request:
    types: [opened, synchronize, reopened]
  push:
    branches:
      - main
      - '\d+.\d+' <1>
  merge_group: ~

permissions:
  contents: read
  pull-requests: read

jobs:
  build:
    uses: elastic/docs-actions/.github/workflows/docs-build.yml@v1
    with:
      path-pattern: docs/**
      use-release-branches: true <2>
```

1. Ensure version branches are built and publish their links ahead of time.
2. Matches **docs-deploy** `with:` (`path-pattern`, `use-release-branches`) so release-line pushes still produce `links.json` when needed even if `docs/**` is unchanged for a long time.
