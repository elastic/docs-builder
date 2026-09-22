---
navigation_title: Onboard a repo
---

# Onboard a repo to automated release notes

To automate your release notes, you need to onboard your repository first. After you finish, every labelled pull request in the repo is included in your product's release notes.

Before starting, make sure your product is registered in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). If it is not, ask in `#docs-eng` to add it.

:::::::{stepper}

::::::{step} Agree on your labels

Choose a pull request label for each change type (for example, `feature`, `bug-fix`, and `breaking-change`), and also one label that means the pull request does not need a release note (for example, `changelog:skip`). 

The supported types are:

- `feature`
- `enhancement`
- `security`
- `bug-fix`
- `breaking-change`
- `deprecation`
- `known-issue`
- `docs`
- `regression`
- `other`

:::{tip}
Add a CI check to your repository that requires exactly one type label on every pull request, so that you don't forget to add a label.
:::
::::::

::::::{step} Add a changelog configuration file

Create the `docs/changelog.yml` file. The following configuration is generally enough:

```yaml
bundle:
  # Input directory containing changelog YAML files
  directory: docs/changelog
  # Output directory for bundled changelog files
  output_directory: docs/releases

pivot:
  # Map labels to standard changelog types
  # At a minimum, feature, bug-fix, and breaking-change must be configured.
  types:
    feature: "feature"
    bug-fix: "bug, fix"
    breaking-change: "breaking"

rules:
  create:
    # Labels that mean this pull request needs no changelog entry
    exclude: "changelog:skip, chore, dependencies, ci"
```

Labels for the `feature`, `bug-fix`, and `breaking-change` types are required. The configuration is rejected if any of them is missing, or if a type key is not a supported type. Add `enhancement`, `docs`, or other types only when you use them.

`rules.create.exclude` is the list of labels that mean no entry is needed. A pull request with one of these labels is recorded as skipped. The check passes, and no entry is created.

:::{tip}
Every list-like value accepts both a comma-separated string and a YAML list. `exclude: "chore, ci"` and a list of `- chore` and `- ci` produce the same result. Use whichever reads better.
:::
::::::

::::::{step} Map your products to labels

Whether you need to map products at all depends on how many the repo publishes:

::::{tab-set}

:::{tab-item} One product

Nothing to configure. The product is inferred from the repository name: any product whose ID or `repository` field matches the repo name is used for every entry.
:::

:::{tab-item} More than one product

Write the product IDs and their pull request labels as a list. A release note entry gets every product whose label is on the pull request:

```yaml
pivot:
  products:
    kibana: "Team:Kibana"
    observability: "Team:Obs"
    security: "Team:Security"
```

Each product must be a product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). An ID that is not in that file is rejected, and the error lists every ID you can use.

If a pull request carries none of these labels, the check fails and asks for one. To accept a fallback instead of failing, set `products.default`:

```yaml
products:
  default:
    - product: kibana
```

`lifecycle` is optional and defaults to `ga`. Set it when the product is not yet generally available:

```yaml
products:
  default:
    - product: kibana
      lifecycle: beta
```

The supported values are `preview`, `beta`, `ga`, and `experimental`.
:::
::::
:::{tip}
For every available setting, refer to [Configuration reference](/data/release-notes/configure-ref.md) and to the annotated [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) template.
:::
::::::

::::::{step} Add the workflow files

The `.github/workflows/release-notes.yml` file is the only workflow file the onboarding check looks for and is the only workflow that is required for the release notes automation to work.

To have the release notes automation comment your PRs, add the `.github/workflows/release-notes-comments.yml` file. Comments are optional, but recommended, since they explain why a check failed.

How you write the first file depends on who publishes the GitHub release.

:::::{tab-set}

::::{tab-item} You publish the releases yourself

Add the `release` trigger, and let the shared workflow build the bundle:

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

::::

::::{tab-item} A workflow publishes the releases

GitHub does not emit the `release` event for a release published with `GITHUB_TOKEN`, so a workflow that listens for `on: release` never runs. Build the bundle in the workflow that publishes the release instead.

Leave the `release` trigger and the `bundle-on-release` input out of the caller:

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

Then add two jobs to the workflow that publishes your release. Replace `create-release` with whichever job creates the GitHub release:

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

      - name: Publish bundle to S3
        uses: elastic/docs-actions/changelog/bundle-publish@v1
        with:
          config: docs/changelog.yml
          bundle-path: ${{ needs.create-release-notes-bundle.outputs.bundle-path }}
```

Keep these as two jobs. The create job holds no AWS credentials and hands the bundle over as an artifact, so only the publish job assumes the upload role.

:::{important}
Grant the create job `pull-requests: read`. The bundler looks up the pull request list through the GitHub GraphQL API, and the job fails without that permission.
:::
::::

:::::

### Add the comments workflow

Use this file in either case:

```yaml
name: release-notes-comments
on:
  workflow_run:
    workflows: [release-notes]
    types: [completed]
permissions: {}
jobs:
  comment:
    if: github.event.workflow_run.event == 'pull_request'
    permissions:
      pull-requests: write
      actions: read
    uses: elastic/docs-actions/.github/workflows/release-notes-comments.yml@v1
```

It has to run as a `workflow_run` job. A pull request job from a fork cannot be granted `pull-requests: write`.
::::::

::::::{step} Check the setup

Run `docs-builder changelog validate-onboarding`.

The command checks that the repository is ready to publish release notes.
::::::

::::::{step} Ship one release

Merge a labelled pull request, then confirm that:

1. The pull request check validates the label, and the comment reports what was recorded.
2. At release time, the bundle is built, uploaded, and shown on your product's release notes page.
::::::

:::::::

## Write good release notes entries

Contributors must follow two rules:

1. Put exactly one change-type label on the pull request (for example, `bug-fix`). If the change does not need a release note, apply the skip label.
2. Write the pull request title as the sentence readers will see. Keep it under 80 characters.

Optionally, add a `## Release note` heading to the pull request description, followed by one paragraph to add detail.

:::{tip}
When one paragraph is not enough, a contributor can commit a changelog entry file on the pull request. The file is validated and synced like any other entry. Refer to [Create changelogs](/data/release-notes/create.md).
:::

## Migrate historical release notes

Release notes published before you onboard are not migrated automatically. Automation only covers pull requests merged after your repo is onboarded.

Older notes can often be converted. The docs team runs a job that reads your product's published release notes page and turns each entry back into changelog data, so your history ends up in the same format as everything published from now on. The job only knows how to read products on a list kept in the docs-builder source, and that list is separate from `products.yml`. Being registered for release notes therefore does not mean your history can be converted. Ask in `#docs-eng` to find out whether your product is on the list, and to have the job run for you.

To rebuild one past release rather than your whole history, add a `release-notes-backfill.yml` workflow in your repo. It works for any tag that already has a GitHub release. Products promoted by date, such as serverless, do not have those releases, so the workflow does not apply to them.
