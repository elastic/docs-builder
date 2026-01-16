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

`--owner <string?>`
:   Optional: GitHub repository owner (used when `--pr` is just a number).

`--products <List<ProductInfo>>`
:   Required: Products affected in format "product target lifecycle, ..." (for example, `"elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05"`).
:   The valid product identifiers are listed in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
:   The valid lifecycles are listed in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).

`--prs <string[]?>`
:   Optional: Pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times.
:   Each occurrence can be either comma-separated PRs (e.g., `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (e.g., `--prs /path/to/file.txt`).
:   When specifying PRs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.
:   If `--owner` and `--repo` are provided, PR numbers can be used instead of URLs.
:   If specified, `--title` can be derived from the PR.
:   If mappings are configured, `--areas` and `--type` can also be derived from the PR.
:   Creates one changelog file per PR.
:   If `add_blockers` are configured in the changelog configuration file and a PR has a blocking label for any product in `--products`, that PR is skipped and no changelog file is created for it.

`--repo <string?>`
:   Optional: GitHub repository name (used when `--pr` is just a number).

`--strip-title-prefix`
:   Optional: When used with `--prs`, remove square brackets and text within them from the beginning of PR titles.
:   For example, if a PR title is `"[Attack discovery] Improves Attack discovery hallucination detection"`, the changelog title will be `"Improves Attack discovery hallucination detection"`.
:   This option applies only when the title is derived from the PR (when `--title` is not explicitly provided).

`--extract-release-notes`
:   Optional: When used with `--prs`, extract release notes from PR descriptions and use them in the changelog.
:   The extractor looks for content in various formats in the PR description:
:   - `Release Notes: ...`
:   - `Release-Notes: ...`
:   - `release notes: ...`
:   - `Release Note: ...`
:   - `Release Notes - ...`
:   - `## Release Note` (as a markdown header)
:   Short release notes (≤120 characters, single line) are used as the changelog title (only if `--title` is not explicitly provided).
:   Long release notes (>120 characters or multi-line) are used as the changelog description (only if `--description` is not explicitly provided).
:   If no release note is found, no changes are made to the title or description.

`--subtype <string?>`
:   Optional: Subtype for breaking changes (for example, `api`, `behavioral`, or `configuration`).
:   The valid subtypes are listed in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).

`--title <string>`
:    A short, user-facing title (max 80 characters)
:    Required if `--pr` is not specified.
:    If both `--pr` and `--title` are specified, the latter value is used instead of what exists in the PR.

`--type <string>`
:   Required: Type of change (for example, `feature`, `enhancement`, `bug-fix`, or `breaking-change`).
:   The valid types are listed in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).

`--use-pr-number`
:   Optional: Use the PR number as the filename instead of generating it from a unique ID and title.
:   When using this option, you must also provide the `--pr` option.
