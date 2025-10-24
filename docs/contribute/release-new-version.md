# Release a new documentation version

When a new version of the Elastic Stack (or another versioned product) is released, the docs site must be updated to recognize it. This process primarily involves updating version metadata in the shared site configuration.

Follow these steps to release a new documentation version.

:::::{stepper}   

::::{step} Update `versions.yml`

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

Refer to [`versions.yml`](../configure/site/versions.md) for more information.

::::

::::{step} (Optional) Update legacy URL mappings

_This action can be performed by any member of the docs team._

If you're releasing a version older than the current `base`, or restoring support for a previously removed version, you may need to update the `legacy-url-mappings.yml` file.

This file maps legacy URL paths (like `en/elasticsearch/reference/`) to the list of versions that exist at that path.

For example, to release the 8.19 version of the Elastic Stack, update the `stack` versions array to include the new version number:

```yml
- stack: &stack [ '8.19', '8.18', '8.17', ... ]
```

See [`legacy-url-mappings.yml`](../configure/site/legacy-url-mappings.md) for more information.

::::

::::{step} Approve and merge the config change

_This action must be performed by docs engineering._

Merge the `versions.yml` changes and any legacy URL mapping changes.

Optionally, invoke the [Synchronize version & config updates](https://github.com/elastic/docs-internal-workflows/actions/workflows/update-assembler-version.yml) action manually on `docs-internal-workflows`, which opens two configuration update PRs: `staging` and `prod`.

This action also runs on a cron job, but you can trigger it manually if needed.

:::{important}
Do not merge the production PR until release day!
:::

::::

::::{step} After feature freeze: merge the config change to staging

_This action must be performed by docs engineering._

Merge [the `staging` configuration update PR](https://github.com/elastic/docs-internal-workflows/pulls).

:::{important}
Do not merge the production PR until release day!
:::

::::

::::{step} Release day: merge the config change to prod and release to production

_This action must be performed by docs engineering. For most products, this change must be merged on release day._

1. Merge [the `prod` configuration update PR](https://github.com/elastic/docs-internal-workflows/pulls).
2. Manually [invoke the release automation to production](https://github.com/elastic/docs-internal-workflows/actions/workflows/assembler-build.prod.yml).
3. Let the requester or docs release coordinator know the docs have been updated.

::::

::::{step} Confirm `applies_to` metadata

Cumulative documentation relies on version metadata through `applies_to` blocks, which use version definitions in `versions.yml`.

Check the built output to ensure `applies_to` changes are correctly rendering.

::::
