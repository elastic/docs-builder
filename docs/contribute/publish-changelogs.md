# Publish changelogs

## Create documentation

### Control changelog publishing [example-rules-publishing]

:::{warning}
This functionality is deprecated. Perform the filtering at bundle time instead. Using `rules.publish` emits a deprecation warning during configuration loading.
:::

You can use rules in the changelog configuration file to refrain from publishing changelogs based on their areas and types.

For example, your configuration file can contain a `rules` section like this:

```yaml
rules:
  # Global match default for multi-valued fields (labels, areas).
  #   any (default) = match if ANY item matches the list
  #   all           = match only if ALL items match the list
  # match: any
  # Publish — controls which changelogs appear in rendered output.
  publish:
    exclude_types:
      - docs
    exclude_areas:
      - "Internal"
    products:
      'cloud-serverless':
        exclude_areas:
          - "Internal"
          - "Autoscaling"
          - "Watcher"
      'elasticsearch, kibana':
        exclude_types:
          - docs
          - other
```

For example, if you run the `docs-builder changelog render` command for a Cloud Serverless bundle, any changelogs that have "Internal", "Autoscaling", or "Watcher" areas are commented out.

You can also use **include** mode instead of **exclude** mode.
For example, to only publish changelogs with specific areas:

```yaml
rules:
  publish:
    include_areas:
      - "Search"
      - "Monitoring"
```

When subsections are enabled (`:subsections:` in the `{changelog}` directive or `--subsections` in the `changelog render` command), these `include_areas` and `exclude_areas` rules also affect which area label is used for grouping.
Changelogs with multiple areas are grouped under the first area that aligns with the rules — the first included area for `include_areas`, or the first non-excluded area for `exclude_areas`.

### Include changelogs inline [changelog-directive]

You can use the [`{changelog}` directive](/syntax/changelog.md) to render all changelog bundles directly in your documentation pages.

```markdown
:::{changelog}
:::
```

By default, the directive renders all bundles from `changelog/bundles/` (relative to the docset root), ordered by semantic version (newest first).
For full documentation and examples, refer to the [{changelog} directive syntax reference](/syntax/changelog.md).

:::{note}
All product-specific filtering must be configured at bundle time via `rules.bundle`. Unlike the `changelog render` command, the directive does not apply `rules.publish`.
:::

### Generate markdown or asciidoc [render-changelogs]

The `docs-builder changelog render` command creates markdown or asciidoc files from changelog bundles for documentation purposes.
For up-to-date details, use the `-h` command option or refer to [](/cli/changelog/render.md).

Before you can use this command you must create changelog files and collect them into bundles.
For example, the `docs-builder changelog bundle` command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
entries:
- file:
    name: 1765581721-convert-bytestransportresponse-when-proxying-respo.yaml
    checksum: d7e74edff1bdd3e23ba4f2f88b92cf61cc7d490a
- file:
    name: 1765581721-fix-ml-calendar-event-update-scalability-issues.yaml
    checksum: dfafce50c9fd61c3d8db286398f9553e67737f07
- file:
    name: 1765581651-break-on-fielddata-when-building-global-ordinals.yaml
    checksum: 704b25348d6daff396259216201053334b5b3c1d
```

To create markdown files from this bundle, run the `docs-builder changelog render` command:

```sh
docs-builder changelog render \
  --input "/path/to/changelog-bundle.yaml|/path/to/changelogs|elasticsearch|keep-links,/path/to/other-bundle.yaml|/path/to/other-changelogs|kibana|hide-links" \ <1>
  --title 9.2.2 \ <2>
  --output /path/to/release-notes \ <3>
  --subsections <4>
```

1. Provide information about the changelog bundle(s). The format for each bundle is `"<bundle-file-path>|<changelog-file-path>|<repository>|<link-visibility>"` using pipe (`|`) as delimiter. To merge multiple bundles, separate them with commas (`,`). Only the `<bundle-file-path>` is required for each bundle. The `<changelog-file-path>` is useful if the changelogs are not in the default directory and are not resolved within the bundle. The `<repository>` is necessary if your changelogs do not contain full URLs for the pull requests or issues. The `<link-visibility>` can be `hide-links` or `keep-links` (default) to control whether PR/issue links are hidden for changelogs from private repositories.
2. The `--title` value is used for an output folder name and for section titles in the output files. If you omit `--title` and the first bundle contains a product `target` value, that value is used. Otherwise, if none of the bundles have product `target` fields, the title defaults to "unknown".
3. By default the command creates the output files in the current directory.
4. By default the changelog areas are not displayed in the output. Add `--subsections` to group changelog details by their `areas`. For breaking changes that have a `subtype` value, the subsections will be grouped by subtype instead of area.

:::{important}
Paths in the `--input` option must be absolute paths or use environment variables. Tilde (`~`) expansion is not supported.
:::

For example, the `index.md` output file contains information derived from the changelogs:

```md
## 9.2.2 [elastic-release-notes-9.2.2]

### Fixes [elastic-9.2.2-fixes]

**Network**
* Convert BytesTransportResponse when proxying response from/to local node. [#135873](https://github.com/elastic/elastic/pull/135873) 

**Machine Learning**
* Fix ML calendar event update scalability issues. [#136886](https://github.com/elastic/elastic/pull/136886) [#136900](https://github.com/elastic/elastic/pull/136900)

**Aggregations**
* Break on FieldData when building global ordinals. [#108875](https://github.com/elastic/elastic/pull/108875) 
```

When a changelog includes multiple values in its `prs` or `issues` arrays, all its links are rendered inline, as shown in the Machine Learning example above.

To comment out the pull request and issue links, for example if they relate to a private repository, add `hide-links` to the `--input` option for that bundle.
This allows you to selectively hide links per bundle when merging changelogs from multiple repositories.
When `hide-links` is set, all PR and issue links for affected changelogs are hidden together.

If you have changelogs with `feature-id` values and you want them to be omitted from the output, use the `--hide-features` option. Feature IDs specified via `--hide-features` are **merged** with any `hide-features` already present in the bundle files. This means both CLI-specified and bundle-embedded features are hidden in the output.

To create an asciidoc file instead of markdown files, add the `--file-type asciidoc` option:

```sh
docs-builder changelog render \
  --input "./changelog-bundle.yaml,./changelogs,elasticsearch" \
  --title 9.2.2 \
  --output ./release-notes \
  --file-type asciidoc \ <1>
  --subsections
```

1. Generate a single asciidoc file instead of multiple markdown files.

#### Release highlights

The `highlight` field allows you to mark changelogs that should appear in a dedicated highlights page.
Highlights are most commonly used for major or minor version releases to draw attention to the most important changes.

When you set `highlight: true` in a changelog:

- It appears in both the highlights page (`highlights.md`) and its normal type section (for example "Features and enhancements")
- The highlights page is only created when at least one changelog has `highlight: true` (unlike other special pages like `known-issues.md` which are always created)
- Highlights can be any type of changelog (features, enhancements, bug fixes, etc.)

Example changelog with highlight:

```yaml
type: feature
products:
- product: elasticsearch
  target: 9.3.0
  lifecycle: ga
title: New Cloud Connect UI for self-managed installations
description: Adds Cloud Connect functionality to Kibana, which allows you to use cloud solutions like AutoOps and Elastic Inference Service in your self-managed Elasticsearch clusters.
highlight: true
```

When rendering, changelogs with `highlight: true` are collected from all types and rendered in a dedicated highlights section.
In markdown output, this creates a separate `highlights.md` file.
In asciidoc output, highlights appear as a dedicated section in the single asciidoc file.
