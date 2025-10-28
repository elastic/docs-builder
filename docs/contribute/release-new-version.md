# Release a new documentation version

When a new version of the Elastic Stack (or another versioned product) is released, the docs site must be updated to recognize it. This process primarily involves updating version metadata in the shared site configuration.

## Who needs to be involved

Generally, each release requires the following people:

* A member of the **docs team** to make the config changes in a docs-builder PR
* A member of the **docs engineering** or **docs tech leads** team to support publishing those changes to staging and prod.

Before you start your release, you should identify who from each of these teams will facilitate the release.

## Release process

Follow these steps to release a new documentation version.

:::{tip}
The docs-builder PR steps can be bundled into a single PR.
:::

:::::{stepper}

::::{step} [docs-builder PR] Update `versions.yml`

_This action can be performed by any member of the docs team. It's also [automated](https://github.com/elastic/docs-builder/actions/workflows/updatecli.yml) for many products._

The `versions.yml` file defines the **base** (minimum) and **current** (latest) versions of each versioned product family.

Example:

```yaml
versioning_systems:
  stack: &stack
    base: 9.0
    current: 9.1.0
```

- Update the `current` version to reflect the newly released version.
- Only update the `base` version if you're dropping support for an older version.

Refer to [`versions.yml`](/configure/site/versions.md) for more information.

::::

::::{step} [docs-builder PR] (Optional) Bump the version branch
_This action can be performed by any member of the docs team._

If you use the [tagged branching strategy](/contribute/branching-strategy.md), and your release corresponds with a new branch in the repository that holds your documentation, then you also need to bump the `current` and `next` branch in the docs configuration.

This step is not always required, depending on your branching strategy. For example, if you only have branches for major versions of your product (e.g. 1 and 2), and you're already publishing your docs from the `1` branch, then you don't need to bump the version branch to release version 1.2 or 1.2.3 of your documentation.

1. In `assembler.yml`, specifying the new `current` and `next` branches for your repository:
   
    ```yml
    your-product:
    current: 1.1
    next: 1.2
    ```

    Some people use `main` or `master` for their `next` branch. In this case, the `next` value doesn't need to be changed.

2. Tag the PR with the `ci` label. After CI runs, confirm that the intended version branches are publishing to the link service. When links are being published as intended, they can be found at the following URL, where repo is your repo name and branch is your newly configured branch:

    ```
    elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/<repo>/<branch>/links.json
    ```

3. Rerun the `validate-assembler` check on the PR.


[Learn more about changing the published branch](/contribute/branching-strategy.md#how-to-change-the-published-branch).
::::

::::{step} [docs-builder PR] (Optional) Update legacy URL mappings

_This action can be performed by any member of the docs team._

If you're releasing a version older than the current `base`, or restoring support for a previously removed version, you may need to update the `legacy-url-mappings.yml` file.

This file maps legacy URL paths (like `en/elasticsearch/reference/`) to the list of versions that exist at that path.

For example, to release the 8.19 version of the Elastic Stack, update the `stack` versions array to include the new version number:

```yml
- stack: &stack [ '8.19', '8.18', '8.17', ... ]
```

See [`legacy-url-mappings.yml`](../configure/site/legacy-url-mappings.md) for more information.

::::

::::{step} [docs-builder PR] Merge the config change

Merge the `versions.yml` changes and any assembler and legacy URL mapping changes. Anyone from the docs team can merge the PR, but it must be approved by docs engineering or docs tech leads.

Optionally, docs engineering can invoke the [Synchronize version & config updates](https://github.com/elastic/docs-internal-workflows/actions/workflows/update-assembler-version.yml) action manually on `docs-internal-workflows`, which opens two configuration update PRs: `staging` and `prod`.

This action also runs on a cron job, but can be triggered manually if the change is time-sensitive.

:::{important}
Do not merge the production PR until release day!
:::

::::

::::{step} After feature freeze: merge the config change to staging

_This action must be performed by docs engineering or docs tech leads._

Approve and merge [the `staging` configuration update PR](https://github.com/elastic/docs-internal-workflows/pulls).

:::{important}
Do not merge the production PR until release day!
:::

::::

::::{step} Release day: merge the config change to prod and release to production

_This action must be performed by docs engineering or docs tech leads. For most products, this change must be merged on release day._

1. Approve and merge [the `prod` configuration update PR](https://github.com/elastic/docs-internal-workflows/pulls).
2. Manually [invoke the release automation to production](https://github.com/elastic/docs-internal-workflows/actions/workflows/assembler-build.prod.yml).
3. Let the requester or docs release coordinator know the docs have been updated.

::::

::::{step} Confirm `applies_to` metadata

Cumulative documentation relies on version metadata through `applies_to` blocks, which use version definitions in `versions.yml`.

Check the built output to ensure `applies_to` changes are correctly rendering.

::::
:::::
