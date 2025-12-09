# changelog add

Create a changelog file that describes a single item in the release documentation.
For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog add [options...] [-h|--help]
```

## Options

`--action <string?>`
:   Optional: What users must do to mitigate.

`--areas <string[]?>`
:   Optional: Areas affected (comma-separated or specify multiple times).

`--config <string?>`
:   Optional: Path to the changelog.yml configuration file. Defaults to `docs/changelog.yml`.

`--description <string?>`
:   Optional: Additional information about the change (max 600 characters).

`--feature-id <string?>`
:   Optional: Feature flag ID

`--highlight <bool?>`
:   Optional: Include in release highlights.

`--impact <string?>`
:   Optional: How the user's environment is affected.

`--issues <string[]?>`
:   Optional: Issue numbers (comma-separated or specify multiple times).

`--output <string?>`
:   Optional: Output directory for the changelog fragment. Defaults to current directory.

`--products <List<ProductInfo>>`
:   Required: Products affected in format "product target lifecycle, ..." (for example, `"elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05"`).

`--pr <string?>`
:   Optional: Pull request number.

`--subtype <string?>`
:   Optional: Subtype for breaking changes (for example, `api`, `behavioral`, or `configuration`).

`--title <string>`
:    Required: A short, user-facing title (max 80 characters)

`--type <string>`
:   Required: Type of change (for example, `feature`, `enhancement`, `bug-fix`, or `breaking-change`)
