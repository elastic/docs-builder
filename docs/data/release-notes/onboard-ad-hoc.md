---
navigation_title: Ad hoc releases
---

# Onboard ad hoc releases

Use this path when your repository publishes a versioned GitHub release on its own schedule. Complete the [shared onboarding steps](onboard.md) first.

How you configure the release workflow depends on who publishes the GitHub release.

::::{tab-set}

:::{tab-item} A person publishes the release

Add the `release` trigger to `.github/workflows/release-notes.yml`. Set `bundle-on-release` to `true` so the shared workflow builds and publishes the bundle.

```yaml
name: release-notes
on:
  pull_request:
    types: [opened, synchronize, reopened, edited, labeled, unlabeled]
  push:
    branches: [main]
  release:
    types: [published]
permissions: {}
jobs:
  release-notes:
    permissions:
      contents: read
      id-token: write
      pull-requests: read
      packages: read
    uses: elastic/docs-actions/.github/workflows/release-notes.yml@v1
    with:
      bundle-on-release: true
```

:::

:::{tab-item} A workflow publishes the release

GitHub does not emit the `release` event when a workflow publishes a release with `GITHUB_TOKEN`. Do not add a `release` trigger or the `bundle-on-release` input to `.github/workflows/release-notes.yml`.

```yaml
name: release-notes
on:
  pull_request:
    types: [opened, synchronize, reopened, edited, labeled, unlabeled]
  push:
    branches: [main]
permissions: {}
jobs:
  release-notes:
    permissions:
      contents: read
      id-token: write
      pull-requests: read
      packages: read
    uses: elastic/docs-actions/.github/workflows/release-notes.yml@v1
```

Build and publish the bundle in the workflow that publishes the release. Replace `create-release` with the job that creates your GitHub release.

```yaml
  create-release-notes-bundle:
    needs: create-release
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: read
      pull-requests: read
    outputs:
      bundle-path: ${{ steps.create.outputs.bundle-path }}
    steps:
      - uses: actions/checkout@v7
        with:
          ref: ${{ needs.create-release.outputs.tag }}
          persist-credentials: false

      - name: Create bundle
        id: create
        uses: elastic/docs-actions/changelog/bundle-create-version@v1
        with:
          config: docs/changelog.yml
          version: ${{ needs.create-release.outputs.tag }}
          repo: ${{ github.event.repository.name }}
          owner: ${{ github.repository_owner }}

  publish-release-notes-bundle:
    needs:
      - create-release
      - create-release-notes-bundle
    runs-on: ubuntu-latest
    permissions:
      contents: read
      id-token: write
      packages: read
    steps:
      - uses: actions/checkout@v7
        with:
          ref: ${{ needs.create-release.outputs.tag }}
          persist-credentials: false

      - name: Publish bundle
        uses: elastic/docs-actions/changelog/bundle-publish@v1
        with:
          config: docs/changelog.yml
          bundle-path: ${{ needs.create-release-notes-bundle.outputs.bundle-path }}
```

Keep these as two jobs. Only the publish job needs permission to assume the upload role.

:::{important}
Grant the create job `pull-requests: read`. The bundler uses the GitHub GraphQL API to find the pull requests in the release.
:::

:::

::::

## Check the setup

Run `docs-builder changelog validate-onboarding`.

The command checks that the repository is ready to publish release notes.

## Test the first release

Merge a labelled pull request. Then publish a release and confirm that its release notes bundle appears on the product release notes page.
