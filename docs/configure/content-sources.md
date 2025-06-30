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


### Continuous deployment

This is the default. To get started, follow our [guide](/migration/guide/index.md) to set up the new docs folder structure and CI configuration.

Once setup ensure the repository is added to our `assembler.yml`  under `references`. 

For example say you want to onboard `elastic/my-repository` into the production build:

```yaml
references:
  my-repository:
```

Is equivalent to specifying.

```yaml
references:
  my-repository:
    next: main
    current: main
```

% TODO we need navigation.yml docs
Once the repository is added, its navigation still needs to be injected into to global site navigation.

### Tagged

If you want to have more control over the timing of when your docs go live to production. Configure the repository
in our `assembler.yml` to have a fixed git reference (typically a branch) deploy the `current` content source to production.

```yaml
references:
  my-other-repository:
    next: main
    current: 9.0
```

:::{note}
In order for `9.0` to be onboarded it needs to first follow our [migration guide](/migration/guide/index.md) and have our documentation CI integration setup.
Our CI integration checks will block until `current` is successfully configured
:::

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