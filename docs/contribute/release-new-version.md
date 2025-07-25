# Release a new documentation version

When a new version of the Elastic Stack (or another versioned product) is released, the docs site must be updated to recognize it. This process primarily involves updating version metadata in the shared site configuration.

Follow these steps to release a new documentation version.

:::::{stepper}   

::::{step} Update `versions.yml`

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

If you're releasing a version older than the current `base`, or restoring support for a previously removed version, you may need to update the `legacy-url-mappings.yml` file.

This file maps legacy URL paths (like `en/elasticsearch/reference/`) to the list of versions that exist at that path.

For example, to release the 8.19 version of the Elastic Stack, update the `stack` versions array to include the new version number:

```yml
- stack: &stack [ '9.0+', '8.19', '8.18', '8.17', ... ]
```

:::{important}
The first version in the `mappings` list is treated as the "current" version in documentation version dropdown.
:::

See [`legacy-url-mappings.yml`](../configure/site/legacy-url-mappings.md) for more information.

::::

::::{step} Release a new version of docs-builder

Version updates and content set additions require a release of docs-builder.
Contact the Docs Eng team for assistance.

::::

::::{step} Confirm `applies_to` metadata

Cumulative documentation relies on version metadata through `applies_to` blocks, which use version definitions in `versions.yml`.

Check the built output to ensure `applies_to` changes are correctly rendering.

::::
