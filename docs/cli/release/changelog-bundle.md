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

`--directory <string?>`
:   Optional: The directory that contains the changelog YAML files.
:   Defaults to the current directory.

`--input-products <List<ProductInfo>?>`
:   Filter by products in format "product target lifecycle, ..."
:   For example, `cloud-serverless 2025-12-02, cloud-serverless 2025-12-06`.

`--output <string?>`
:   Optional: The output file path for the bundle.
:   Defaults to `changelog-bundle.yaml` in the input directory.

`--output-products <List<ProductInfo>?>`
:   Explicitly set the products array in the output file in format "product target lifecycle, ...".
:   This value replaces information that would otherwise by derived from changelogs.

`--owner <string?>`
:   Optional: The GitHub repository owner, which is required when pull requests are specified as numbers.

`--prs <string[]?>`
:   Filter by pull request URLs or numbers (can specify multiple times).

`--prs-file <string?>`
:   The path to a newline-delimited file containing PR URLs or numbers.

`--repo <string?>`
:   Optional: The GitHub repository name, which is required when PRs are specified as numbers.

`--resolve`
:   Copy the contents of each changelog file into the entries array.
