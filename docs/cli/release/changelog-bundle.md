# changelog bundle

Bundle changelog files.

To create the changelogs, use [](/cli/release/changelog-add.md).
<!--
For details and examples, go to [](/contribute/changelog.md).
-->

## Usage

```sh
docs-builder changelog bundle [options...] [-h|--help]
```

## Options

`--all`
:   Include all changelogs from the directory.
:   Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.

`--directory <string?>`
:   Optional: The directory that contains the changelog YAML files.
:   Defaults to the current directory.

`--input-products <List<ProductInfo>?>`
:   Filter by products in format "product target lifecycle, ..."
:   When specified, all three parts (product, target, lifecycle) are required but can be wildcards (`*`).
:   Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.
:   Examples:
:   - `"cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta"` - exact matches
:   - `"cloud-serverless 2025-12-02 *"` - match cloud-serverless 2025-12-02 with any lifecycle
:   - `"elasticsearch * *"` - match all elasticsearch changelogs
:   - `"* 9.3.* *"` - match any product with target starting with "9.3."
:   - `"* * *"` - match all changelogs (equivalent to `--all`)

`--output <string?>`
:   Optional: The output file path for the bundle.
:   Defaults to `changelog-bundle.yaml` in the input directory.

`--output-products <List<ProductInfo>?>`
:   Optional: Explicitly set the products array in the output file in format "product target lifecycle, ...".
:   This value replaces information that would otherwise by derived from changelogs.

`--owner <string?>`
:   The GitHub repository owner, which is required when pull requests are specified as numbers.

`--prs <string[]?>`
:   Filter by pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times.
:   Only one filter option can be specified: `--all`, `--input-products`, or `--prs`.
:   Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`).
:   When specifying PRs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.

`--repo <string?>`
:   The GitHub repository name, which is required when PRs are specified as numbers.

`--resolve`
:   Optional: Copy the contents of each changelog file into the entries array.
:   By default, the bundle contains only the file names and checksums.
