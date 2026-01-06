# changelog render

Generate markdown files from changelog bundle files.

To create the bundle files, use [](/cli/release/changelog-bundle.md).

For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog render [options...] [-h|--help]
```

## Options

`--input <List<BundleInput>>`
:   One or more bundle input files.
:   Each item can be specified as "bundle-file-path, changelog-file-path, repo" to accommodate files coming from multiple locations.
:   For example, `--input "./changelog-bundle.yaml,./changelogs,elasticsearch"`.
:   Only `bundle-file-path` is required.
:   Use `repo` if your changelogs do not contain full URLs for the pull requests or issues; otherwise they will be incorrectly derived with "elastic/elastic" in the URL by default.

`--output <string?>`
:   Optional: The output directory for rendered markdown files.
:   Defaults to current directory.

`--title <string?>`
:   Optional: The title to use for section headers, directories, and anchors in output markdown files.
:   Defaults to the version in the first bundle.
:   If the string contains spaces, they are replaced with dashes when used in directory names and anchors.

`--subsections`
:   Optional: Group entries by area in subsections.
:   Defaults to false.

`--hide-private-links`
:   Optional: Hide private links by commenting them out in the markdown output.
:   This option is useful when rendering changelog bundles in private repositories.
:   Defaults to false.

`--hide-features <string[]?>`
:   Optional: Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times.
:   Each occurrence can be either comma-separated feature IDs (e.g., `--hide-features "feature:new-search-api,feature:enhanced-analytics"`) or a file path (e.g., `--hide-features /path/to/file.txt`).
:   When specifying feature IDs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file. The file should contain one feature ID per line.
:   Entries with matching `feature-id` values will be commented out in the markdown output and a warning will be emitted.

You can configure `render_blockers` in your `changelog.yml` configuration file to automatically block changelog entries from being rendered based on their products, areas, and/or types.
For more information, refer to [](/contribute/changelog.md#render-blockers).
