# changelog render

Generate markdown or asciidoc files from changelog bundle files.

To create the bundle files, use [](/cli/release/changelog-bundle.md).
For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog render [options...] [-h|--help]
```

## Options

`--config <string?>`
:   Optional: Path to the changelog.yml configuration file.
:   Defaults to `docs/changelog.yml`.
:   This configuration file is where the command looks `block ... publish` definitions.

`--hide-features <string[]?>`
:   Optional: Filter by feature IDs (comma-separated), or a path to a newline-delimited file containing feature IDs. Can be specified multiple times.
:   Each occurrence can be either comma-separated feature IDs (e.g., `--hide-features "feature:new-search-api,feature:enhanced-analytics"`) or a file path (e.g., `--hide-features /path/to/file.txt`).
:   When specifying feature IDs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file. The file should contain one feature ID per line.
:   Entries with matching `feature-id` values will be commented out in the output and a warning will be emitted.

`--input <string[]>`
:   One or more bundle input files.
:   Each bundle is specified as "bundle-file-path|changelog-file-path|repo|link-visibility" using pipe (`|`) as delimiter.
:   To merge multiple bundles, separate them with commas: `--input "bundle1|dir1|repo1|keep-links,bundle2|dir2|repo2|hide-links"`.
:   For example, `--input "/path/to/changelog-bundle.yaml|/path/to/changelogs|elasticsearch|keep-links"`.
:   Only `bundle-file-path` is required for each bundle.
:   Use `repo` if your changelogs do not contain full URLs for the pull requests or issues; otherwise they will be incorrectly derived with "elastic/elastic" in the URL by default.
:   Use `link-visibility` to control whether PR/issue links are shown or hidden for entries from this bundle. Valid values are `keep-links` (default) or `hide-links`. Use `hide-links` for bundles from private repositories.
:   **Important**: Paths must be absolute or use environment variables. Tilde (`~`) expansion is not supported.

:::{note}
The `render` command automatically discovers and merges `.amend-*.yaml` files with their parent bundle. For more information about amended bundles, go to [](changelog-bundle-amend.md).
:::

`--file-type <string>`
:   Optional: Output file type. Valid values: `"markdown"` or `"asciidoc"`.
:   Defaults to `"markdown"`.
:   When `"markdown"` is specified, the command generates multiple markdown files (index.md, breaking-changes.md, deprecations.md, known-issues.md).
:   When `"asciidoc"` is specified, the command generates a single asciidoc file with all sections.

`--output <string?>`
:   Optional: The output directory for rendered files.
:   Defaults to current directory.

`--subsections`
:   Optional: Group entries by area in subsections.
:   Defaults to false.

`--title <string?>`
:   Optional: The title to use for section headers, directories, and anchors in output files.
:   Defaults to the version in the first bundle.
:   If the string contains spaces, they are replaced with dashes when used in directory names and anchors.

You can configure `block` definitions in your `changelog.yml` configuration file to automatically comment out changelog entries  based on their products, areas, and/or types.
For more information, refer to [](/contribute/changelog.md#example-block-label).

## Output formats

### Markdown format

When `--file-type markdown` is specified (the default), the command generates multiple markdown files:

- `index.md` - Contains features, enhancements, bug fixes, security updates, documentation changes, regressions, and other changes
- `breaking-changes.md` - Contains breaking changes
- `deprecations.md` - Contains deprecations
- `known-issues.md` - Contains known issues

### Asciidoc format

When `--file-type asciidoc` is specified, the command generates a single asciidoc file with all sections:

- Security updates
- Bug fixes
- New features and enhancements
- Breaking changes
- Deprecations
- Known issues
- Documentation
- Regressions
- Other changes

The asciidoc output uses attribute references for links (for example, `{repo-pull}NUMBER[#NUMBER]`).
