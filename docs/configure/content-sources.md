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

Continuous deployment 
:   This is the default where a repositories `main` or `master` branch is continuously deployed to production.

Tagged
:   Allows you to 'tag' a single git reference (typically a branch) as `current` which will be used to deploy to production.
    Allowing you to control the timing of when new documentation should go live.

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

To ensure repositories that use the [tagged branching strategy](#tagged) can be onboarded correctly, our CI integration needs to have appropriate `push`
 branch triggers.

```yml
name: docs-build

on:
  push:
    branches:
      - main
      - '\d+.\d+' <1>
  pull_request_target: ~
  merge_group: ~

jobs:
  docs-preview:
    uses: elastic/docs-builder/.github/workflows/preview-build.yml@main
    with:
      path-pattern: docs/**
    permissions:
      deployments: write
      id-token: write
      contents: read
      pull-requests: write
```

1. Ensure version branches are built and publish their links ahead of time.
