# New versioning

As part of the new information architecture, pages with varying versioning schemes are now interwoven, creating the opportunity and necessity to rethink the scope and versioning of each page. The previous approach of creating entirely separate docs sets for every minor version resulted in fragmentation and unnecessary duplication. Consolidating versioning resolves these issues while maintaining clarity and usability.

To ensure a seamless experience for users and contributors, the new versioning approach adheres to the following:

* Context awareness — Each page explicitly states the context it applies to, including relevant deployment types (e.g., Elastic Cloud Hosted and Elastic Cloud Serverless) and versions. Context clarity ensures users know if the content is applicable to their environment. When users land on a Docs page that doesn’t apply to their version or deployment type, clear cues and instructions will guide them to the appropriate content.
* Simplified contributor workflow — For pages that apply to multiple versions or deployment types, we’ve optimized the contributor experience by reducing complexity. Contributors can now manage multi-context content with ease, without duplicating information or navigating confusing workflows.

For versioning plan details, check [Docs Versioning plan](https://docs.google.com/presentation/d/1rHl0ia0ZkLHPLAYE5522CTDoatqwAxwAo29_etStPW8/edit?usp=sharing). To learn how to call out versioning differences in docs-builder, see [product availability](../syntax/applies.md).


## Content Sources

To support multiple deployment models for different repositories, we support the concept of a content source.

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

This allows you as an owner of a repository to choose two different deployment models.

## Deployment models.

The new documentation system supports 2 deployment models.

Continuous deployment. 
:   This is the default where a repositories `main` or `master` branch is continuously deployed to production.

Tagged deployment
:   Allows you to 'tag' a single git reference (typically a branch) as `current` which will be used to deploy to production.
    Allowing you to control the timing of when new documentation should go live.


### Continuous Deployment

This is the default. To get started, follow our [guide](guide/index.md) to set up the new docs folder structure and CI configuration

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

### Tagged Deployment

If you want to have more control over the timing of when your docs go live to production. Configure the repository
in our `assembler.yml` to have a fixed git reference (typically a branch) deploy the `current` content source to production.

```yaml
references:
  my-other-repository:
    next: main
    current: 9.0
```

:::{note}
In order for `9.0` to be onboarded it needs to first follow our [migration guide](guide/index.md) and have our documentation CI integration setup.
Our CI integration checks will block until `current` is successfully configured
:::

#### CI Configuration

To ensure [tagged deployments](#tagged-deployment) can be onboarded correctly, our CI integration needs to have appropriate `push`
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
      pull-requests: read
```

1. Ensure version branches are built and publish their links ahead of time.