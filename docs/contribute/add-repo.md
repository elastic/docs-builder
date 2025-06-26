# Add a new repository to the docs

Elastic documentation is built from many assembled repositories using `docs-assembler`. Adding a new repository requires making the assembly process aware of its existence.

Follow these instructions to add a new docs repository.

## Prerequisites

The new docs repository needs to satisfy these requirements:

- The repository must have a `docs` folder in the root.
- The `docs` folder must contain a valid [`docset.yml` file](../configure/content-set/navigation.md) and Markdown files.
- Markdown files within the `docs` folder that follow the V3 format. Refer to [Syntax](../syntax/index.md).
- The repository must be within the Elastic organization on `GitHub` and public.

## Add the repository

Follow these instructions to add a new repository to the docs.

:::::{stepper}   

::::{step} Add the repo to docs-infra

Add the repo to the list of repositories that can upload to the Link index by editing the [repositories.yml](https://github.com/elastic/docs-infra/blob/main/modules/aws-github-actions-oidc-roles/repositories.yml) file.

For example, to add the fictitious `elastic/yadda-docs` repository:

```yaml
repositories:
    - name: elastic/yadda-docs # Added for testing purposes
```

::::

::::{step} Add the workflow actions to the repository

Add the following actions to the `.github/workflows` directory of your repo:

- https://github.com/elastic/docs-builder/blob/main/.github%2Fworkflows%2Fpreview-build.yml
- https://github.com/elastic/docs-builder/blob/main/.github/workflows/preview-cleanup.yml

Then, successfully run a docs build on the `main` branch. This is a requirement. For example, you can merge a docs pull request to `main` after adding the workflow actions.

::::

::::{step} Add the repository to the assembler and navigation configs

Edit the [assembler.yml](https://github.com/elastic/docs-builder/blob/main/src/tooling/docs-assembler/assembler.yml) file to add the repository. Refer to [assembler.yml](../configure/site/content.md) for more information.

For example, to add the `elastic/yadda-docs` repository:

```yaml
references:
  yadda-docs:
```

Then, edit the [navigation.yml](https://github.com/elastic/docs-builder/blob/main/src/tooling/docs-assembler/navigation.yml) file to add the repository to the navigation.

For example, to add the `elastic/yadda-docs` repository under **Reference**:

```yaml
  #############
  # REFERENCE #
  #############
  - toc: reference
    path_prefix: reference
    children:
      # Yadda
      # ✅ https://github.com/elastic/yadda-docs/blob/main/docs/toc.yml
      - toc: yadda-docs://
        path_prefix: reference/yadda
```

::::
:::::

## Add .artifacts to .gitignore

For a more comfortable local `docs-builder` experience, add the following line to the `.gitignore` file of the repo:

```
docs/.artifacts 
```
