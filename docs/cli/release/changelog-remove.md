# changelog remove

Remove changelog YAML files from a directory.

You can remove changelogs based their issues, pull requests, or product metadata.
Alternatively, remove all changelogs from the specified directory.
Exactly one filter option must be specified.

Before deleting anything, the command checks whether any of the matching files are referenced by unresolved bundles, to prevent silently breaking the `{changelog}` directive.

For more context, go to [](/contribute/changelog.md#changelog-remove).

## Usage

```sh
docs-builder  changelog remove [options...] [-h|--help]
```

## Options

`--all`
:   Remove all changelog files in the directory.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, or `--issues`.

`--bundles-dir <string?>`
:   Optional: Override the directory scanned for bundles during the dependency check.
:   When not specified, the directory is discovered automatically from config or fallback paths.

`--config <string?>`
:   Optional: Path to the changelog configuration file.
:   Defaults to `docs/changelog.yml`.

`--directory <string?>`
:   Optional: The directory that contains the changelog YAML files.
:   When not specified, uses `bundle.directory` from the changelog configuration if set, otherwise the current directory.

`--dry-run`
:   Print the files that would be removed and any bundle dependency conflicts, without deleting anything.

`--force`
:   Proceed with removal even when files are referenced by unresolved bundles.
:   Emits a warning per dependency instead of blocking.

`--issues <string[]?>`
:   Filter by issue URLs or numbers (comma-separated), or a path to a newline-delimited file containing issue URLs or numbers.
:   Can be specified multiple times.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, or `--issues`.

`--owner <string?>`
:   The GitHub repository owner, which is required when pull requests or issues are specified as numbers.

`--products <List<ProductInfo>?>`
:   Filter by products in format `"product target lifecycle, ..."`
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, or `--issues`.
:   All three parts (product, target, lifecycle) are required but can be wildcards (`*`). Multiple comma-separated values are combined with OR: a changelog is removed if it matches any of the specified product/target/lifecycle combinations. For example:

- `"elasticsearch 9.3.0 ga"` — exact match
- `"cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"` — remove changelogs for either cloud-serverless 2025-12-02 ga or cloud-serverless 2025-12-06 beta
- `"elasticsearch * *"` — all elasticsearch changelogs
- `"* 9.3.* *"` — any product with a target starting with `9.3.`
- `"* * *"` — all changelogs (equivalent to `--all`)

`--prs <string[]?>`
:   Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers.
:   Can be specified multiple times.
:   Exactly one filter option must be specified: `--all`, `--products`, `--prs`, or `--issues`.

`--repo <string?>`
:   The GitHub repository name, which is required when pull requests or issues are specified as numbers.
