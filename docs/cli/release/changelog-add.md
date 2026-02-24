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
:   If the content contains any special characters such as backquotes(`), you must precede it with a backslash escape character (`\`).

`--areas <string[]?>`
:   Optional: Areas affected (comma-separated or specify multiple times).

`--config <string?>`
:   Optional: Path to the changelog.yml configuration file. Defaults to `docs/changelog.yml`.

`--description <string?>`
:   Optional: Additional information about the change (max 600 characters).
:   If the content contains any special characters such as backquotes, you must precede it with a backslash escape character (`\`).

`--no-extract-release-notes`
:   Optional: Turn off extraction of release notes from PR descriptions.
:   The extractor looks for content in various formats in the PR description:
:   - `Release Notes: ...`
:   - `Release-Notes: ...`
:   - `release notes: ...`
:   - `Release Note: ...`
:   - `Release Notes - ...`
:   - `## Release Note` (as a markdown header)
:   Short release notes (≤120 characters, single line) are used as the changelog title (only if `--title` is not explicitly provided).
:   Long release notes (>120 characters or multi-line) are used as the changelog description (only if `--description` is not explicitly provided).
:   By default, the behavior is determined by the `extract.release_notes` changelog configuration setting.

`--feature-id <string?>`
:   Optional: Feature flag ID

`--highlight <bool?>`
:   Optional: Include in release highlights.

`--impact <string?>`
:   Optional: How the user's environment is affected.
:   If the content contains any special characters such as backquotes, you must precede it with a backslash escape character (`\`).

`--issues <string[]?>`
:   Optional: Issue URL(s) or number(s) (comma-separated), or a path to a newline-delimited file containing issue URLs or numbers. Can be specified multiple times.
:   Each occurrence can be either comma-separated issues (for example `--issues "https://github.com/owner/repo/issues/123,456"`) or a file path (for example `--issues /path/to/file.txt`).
:   When specifying issues directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.
:   If `--owner` and `--repo` are provided, issue numbers can be used instead of URLs.
:   If specified, `--title` can be derived from the issue.
:   Creates one changelog file per issue.

`--no-extract-issues`
:   Optional: Turn off extraction of linked references.
:   When using `--prs`: turns off extraction of linked issues from the PR body (for example, "Fixes #123").
:   When using `--issues`: turns off extraction of linked PRs from the issue body (for example, "Fixed by #123").
:   By default, the behavior is determined by the `extract.issues` changelog configuration setting.

`--output <string?>`
:   Optional: Output directory for the changelog fragment. Defaults to current directory.

`--owner <string?>`
:   Optional: GitHub repository owner (used when `--prs` or `--issues` contains just numbers).

`--products <List<ProductInfo>>`
:   Required: Products affected in format "product target lifecycle, ..." (for example, `"elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05"`).
:   The valid product identifiers are listed in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
:   The valid lifecycles are listed in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).

`--prs <string[]?>`
:   Optional: Pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times.
:   Each occurrence can be either comma-separated PRs (for example `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (for example `--prs /path/to/file.txt`).
:   When specifying PRs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.
:   If `--owner` and `--repo` are provided, PR numbers can be used instead of URLs.
:   If specified, `--title` can be derived from the PR.
:   If mappings are configured, `--areas` and `--type` can also be derived from the PR.
:   Creates one changelog file per PR.
:   If there are `block ... create` definitions in the changelog configuration file and a PR has a blocking label for any product in `--products`, that PR is skipped and no changelog file is created for it.

`--repo <string?>`
:   Optional: GitHub repository name (used when `--prs` or `--issues` contains just numbers).

`--strip-title-prefix`
:   Optional: When used with `--prs`, remove square brackets and text within them from the beginning of PR titles, and also remove a colon if it follows the closing bracket.
:   For example, if a PR title is `"[Attack discovery]: Improves Attack discovery hallucination detection"`, the changelog title will be `"Improves Attack discovery hallucination detection"`.
:   Multiple square bracket prefixes are also supported (for example `"[Discover][ESQL] Fix filtering by multiline string fields"` becomes `"Fix filtering by multiline string fields"`).
:   This option applies only when the title is derived from the PR (when `--title` is not explicitly provided).

`--subtype <string?>`
:   Optional: Subtype for breaking changes (for example, `api`, `behavioral`, or `configuration`).
:   The valid subtypes are listed in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).

`--title <string>`
:    A short, user-facing title (max 80 characters)
:    Required if neither `--prs` nor `--issues` is specified.
:    If both `--prs` and `--title` are specified, the latter value is used instead of what exists in the PR.
:    If the content contains any special characters such as backquotes, you must precede it with a backslash escape character (`\`).

`--type <string>`
:   Required if neither `--prs` nor `--issues` is specified. Type of change (for example, `feature`, `enhancement`, `bug-fix`, or `breaking-change`).
:   If mappings are configured, type can be derived from the PR or issue.
:   The valid types are listed in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).

`--use-pr-number`
:   Optional: Use the PR number(s) as the filename instead of generating it from a timestamp and title.
:   With multiple PRs, uses hyphen-separated numbers (for example, `137431-137432.yaml`).
:   Requires `--prs`. Mutually exclusive with `--use-issue-number`.

`--use-issue-number`
:   Optional: Use the issue number(s) as the filename instead of generating it from a timestamp and title.
:   With multiple issues, uses hyphen-separated numbers (for example, `12345-12346.yaml`).
:   Requires `--issues`. When both `--issues` and `--prs` are specified, still uses the issue number for the filename if this flag is set. Mutually exclusive with `--use-pr-number`.

:::{important}
`--use-pr-number` and `--use-issue-number` are mutually exclusive; specify only one.
:::
