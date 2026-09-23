---
navigation_title: Onboard a repo
---

# Onboard a repo to automated release notes

To automate your release notes, you need to onboard your repository first. After you finish, every labelled pull request in the repo is included in your product's release notes.

Before starting, make sure your product is registered in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). If it is not, add it before you continue.

:::::::{stepper}

::::::{step} Agree on your labels

Choose a pull request label for each required change type:

- `feature`
- `bug-fix`
- `breaking-change`

You can also map labels to other supported types, such as `enhancement`, `security`, or `deprecation`. Choose one label that means the pull request does not need a release note, such as `changelog:skip`.

:::{tip}
Add a CI check to your repository that requires exactly one type label on every pull request, so that you don't forget to add a label.
:::
::::::

::::::{step} Add a changelog configuration file

Create a `docs` directory in your repository if it does not exist. Then create the `docs/changelog.yml` file and copy the following configuration into it. Replace the example labels with labels from your repository.

This configuration is generally enough for automated changelog entry creation:

```yaml
pivot:
  # Map labels to standard changelog types
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

You can also map pull request labels to release note areas. Areas are optional:

```yaml
pivot:
  areas:
    Search: "area:search"
    Security: "area:security"
```

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
    cloud-enterprise: "@Product:ECE"
    cloud-hosted: "@Product:ECH"
```

Each product must be a product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). An ID that is not in that file is rejected, and the error lists every ID you can use.

If a pull request carries none of these labels, the check fails and asks for one. To accept a fallback instead of failing, set `products.default`:

```yaml
products:
  default:
    - product: cloud-enterprise
    - product: cloud-hosted
```

Every product in this list is added when no product label matches.

`lifecycle` is optional and defaults to `ga`. Set it when the product is not yet generally available:

```yaml
products:
  default:
    - product: cloud-enterprise
      lifecycle: beta
```

The supported values are `preview`, `beta`, `ga`, and `experimental`.
:::
::::
:::{tip}
For more ways to create and maintain this file, refer to [Create a changelog configuration file](/data/release-notes/configure.md#changelog-settings). For every available setting, refer to [Configuration reference](/data/release-notes/configure-ref.md) and to the annotated [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) template.
:::
::::::

::::::{step} Add the workflow files

Add the workflow files that your repository needs:

- `.github/workflows/release-notes.yml` is required. It validates pull requests and synchronizes release note data.
- `.github/workflows/release-notes-comments.yml` is optional. Add it if you want the automation to explain validation results in pull request comments.
- `.github/workflows/release-notes-changelog-file.yml` is optional. Add it only if you enable `require-changelog-file` and require each pull request to include a `changelog/*.yml` file.

Use the following table to decide whether to require a changelog file:

| Choice | Benefits | Costs |
| --- | --- | --- |
| Do not require a file (recommended) | Contributors only add a label and write a clear pull request title. This option also works well for forked pull requests. | The pull request title and optional `## Release note` section provide the published text. |
| Require a file | Contributors can review and edit the complete release note as version-controlled YAML. | Every pull request needs an extra file. Automation that writes to pull request branches needs more permissions and cannot write to branches in forks. |

Most repositories should not require a file. Contributors can still add one when they need a longer description.

Your release process determines the contents of `release-notes.yml`. Choose that process in the next step.
::::::

::::::{step} Choose your release moment

Continue with the page that matches how your product releases:

- [Ad hoc releases](onboard-ad-hoc.md): Your repository publishes a versioned GitHub release on its own schedule.
- [Unified releases](onboard-unified.md): Your product publishes as part of a coordinated Elastic release.
- [Serverless releases](onboard-serverless.md): Your product publishes from a date-based serverless promotion.

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

Older notes can often be converted. A backfill job reads a product's published release notes page and turns each entry back into changelog data. The job supports a fixed list of products in the docs-builder source. This list is separate from `products.yml`. Product registration therefore does not guarantee support for historical migration.

To rebuild one past release rather than your whole history, add a `release-notes-backfill.yml` workflow in your repo. It works for any tag that already has a GitHub release. Products promoted by date, such as serverless, do not have those releases, so the workflow does not apply to them.
