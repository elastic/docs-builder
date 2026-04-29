# changelog add

Create a changelog file that describes a single item in the release documentation.
For details and examples, go to [](/contribute/create-changelogs.md).

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

`--concise`
:   Optional: Omit schema reference comments from the generated YAML files. Useful in CI workflows to produce compact output.

`--config <string?>`
:   Optional: Path to the changelog.yml configuration file. Defaults to `docs/changelog.yml`.

`--description <string?>`
:   Optional: Additional information about the change (max 600 characters).
:   If the content contains any special characters such as backquotes, you must precede it with a backslash escape character (`\`).

`--no-extract-release-notes`
:   Optional: Turn off extraction of release notes from PR or issue descriptions.
:   By default, the behavior is determined by the [extract.release_notes](/contribute/configure-changelogs-ref.md#extract) changelog configuration setting.

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
:   Optional: Output directory for the changelog fragment. Falls back to `bundle.directory` in `changelog.yml` when not specified. If that value is also absent, defaults to current directory.

`--owner <string?>`
:   Optional: GitHub repository owner (used when `--prs` or `--issues` contains just numbers, or when using `--release-version`).
:   Falls back to `bundle.owner` in `changelog.yml` when not specified. If that value is also absent, defaults to `elastic`.

`--products <List<ProductInfo>>`
:   Products affected in format "product target lifecycle, ..." (for example, `"elasticsearch 9.2.0 ga, cloud-serverless 2025-08-05"`).
:   The valid product identifiers are listed in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
:   The valid lifecycles are listed in [ChangelogConfiguration.cs](https://github.com/elastic/docs-builder/blob/main/src/services/Elastic.Documentation.Services/Changelog/ChangelogConfiguration.cs).
:   For more information about the valid product and lifecycle values, go to [Product format](#product-format).

`--prs <string[]?>`
:   Optional: Pull request URLs or numbers (comma-separated), or a path to a newline-delimited file containing PR URLs or numbers. Can be specified multiple times.
:   Each occurrence can be either comma-separated PRs (for example `--prs "https://github.com/owner/repo/pull/123,6789"`) or a file path (for example `--prs /path/to/file.txt`).
:   When specifying PRs directly, provide comma-separated values.
:   When specifying a file path, provide a single value that points to a newline-delimited file.
:   If `--owner` and `--repo` are provided, PR numbers can be used instead of URLs.
:   If specified, `--title` can be derived from the PR.
:   If mappings are configured, `--areas`, `--type`, and `--products` can also be derived from the PR labels.
:   Creates one changelog file per PR.
:   If there are `rules.create` definitions in the changelog configuration file and a PR has a blocking label for the resolved products, that PR is skipped and no changelog file is created for it.

`--release-version <string?>`
:   Optional: GitHub release tag to use as a source of pull requests (for example, `"v9.2.0"` or `"latest"`).
:   When specified, the command fetches the release from GitHub, parses PR references from the release notes, and creates one changelog file per PR — without creating a bundle. Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:   Use `docs-builder changelog gh-release` instead if you also want a bundle.
:   Requires `--repo` (or `bundle.repo` in `changelog.yml`).
:   Set to `latest` to use the most recent release.

`--repo <string?>`
:   Optional: GitHub repository name (used when `--prs`, `--issues`, or `--release-version` is specified). Falls back to `bundle.repo` in `changelog.yml` when not specified.

`--strip-title-prefix`
:   Optional: When used with `--prs`, remove square brackets and text within them from the beginning of PR titles, and also remove a colon if it follows the closing bracket.
:   For example, if a PR title is `"[Attack discovery]: Improves Attack discovery hallucination detection"`, the changelog title will be `"Improves Attack discovery hallucination detection"`.
:   Multiple square bracket prefixes are also supported (for example `"[Discover][ESQL] Fix filtering by multiline string fields"` becomes `"Fix filtering by multiline string fields"`).
:   This option applies only when the title is derived from the PR (when `--title` is not explicitly provided).
:   By default, the behavior is determined by the `extract.strip_title_prefix` changelog configuration setting (which defaults to `false`).

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
:   Optional: Use PR numbers for filenames instead of the configured `filename` strategy.
:   Requires `--prs` or `--issues`.
:   Mutually exclusive with `--use-issue-number`.
:   Refer to [](#filenames).

`--use-issue-number`
:   Optional: Use issue numbers for filenames instead of the configured `filename` strategy.
:   Requires `--prs` or `--issues`.
:   Mutually exclusive with `--use-pr-number`.
:   Refer to [](#filenames).

## Filenames

By default, output files are named according to the `filename` strategy in `changelog.yml`:

| Strategy | Example filename | Description |
|---|---|---|
| `timestamp` (default) | `1735689600-fixes-enrich-and-lookup-join-resolution.yaml` | Uses a Unix timestamp with a sanitized title slug. |
| `pr` | `137431.yaml` | Uses the PR number. |
| `issue` | `2571.yaml` | Uses the issue number. |

Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) or [](/contribute/configure-changelogs-ref.md).

You can override those settings with the `--use-pr-number` or `--use-issue-number` CLI flags:

```sh
docs-builder changelog add \
  --prs 1234 \
  --products "elasticsearch 9.2.3" \
  --use-pr-number

docs-builder changelog add \
  --issues 4567 \
  --products "elasticsearch 9.3.0" \
  --use-issue-number
```

:::{important}
`--use-pr-number` and `--use-issue-number` are mutually exclusive; specify only one. Each requires `--prs` or `--issues`. The numbers are extracted from the URLs or identifiers you provide or from linked references in the issue or PR body when extraction is enabled.

**Precedence**: CLI flags (`--use-pr-number` / `--use-issue-number`) > `filename` in `changelog.yml` > default (`timestamp`).
:::

## Product format and resolution [product-format]

The `--products` command option accepts values with the format `"product target lifecycle, ..."` where:

- `product` is a product ID that exists in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `target` is the target version or date (optional)
- `lifecycle` exists in [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs) (optional)

You can further limit the possible values with the [products](/contribute/configure-changelogs-ref.md#products) and [lifecycles](/contribute/configure-changelogs-ref.md#lifecycles) options in the changelog configuration file.

For example:

- `"kibana 9.2.0 ga"`
- `"cloud-serverless 2025-08-05"`
- `"cloud-enterprise 4.0.3, cloud-hosted 2025-10-31"`

The `changelog add` command resolves product values in the following order:

1. The `--products` CLI option always takes priority.
1. If `pivot.products` is defined in the changelog configuration file and the PR or issue has labels that match, those products are used. Multiple matching entries are all applied.
1. If `products.default` is defined in the changelog configuration file, those default products are used.
1. If `--repo` is specified (or `bundle.repo` is set in the changelog configuraiton file), the repository name is matched against known product IDs in `products.yml` and the derived value is used.

If none of these steps yield at least one product, the command returns an error.

## Configuration checks

By default, the command checks the following path for a configuration file: `docs/changelog.yml`.
You can specify a different path with the `--config` command option.

If a configuration file exists, the command validates its values before generating changelog files:

- If the configuration file contains `lifecycles`, `products`, `subtype`, or `type` values that don't match the values in `ChangelogEntryType.cs`, `ChangelogEntrySubtype.cs`, or `Lifecycle.cs`, validation fails.
- If the configuration file contains `areas` values and they don't match what you specify in the `--areas` command option, validation fails.
- If the configuration file contains `lifecycles` or `products` values that are a subset of the available values and you try to create a changelog with values outside that subset, validation fails.

In each of these cases where validation fails, a changelog file is not created.

If the configuration file contains `rules.create` definitions and a PR or issue has a blocking label, that PR is skipped and no changelog file is created for it.
For more information, refer to [Rules for creation and publishing](/contribute/configure-changelogs.md#rules).


## CI auto-detection [ci-auto-detection]

When running inside GitHub Actions, `changelog add` automatically reads the following environment variables to fill in arguments that were not provided on the command line:

| Environment variable | Fills | Set from |
| --- | --- | --- |
| `CHANGELOG_PR_NUMBER` | `--prs` | `github.event.pull_request.number` |
| `CHANGELOG_TITLE` | `--title` | `steps.evaluate.outputs.title` |
| `CHANGELOG_DESCRIPTION` | `--description` | `steps.evaluate.outputs.description` |
| `CHANGELOG_TYPE` | `--type` | `steps.evaluate.outputs.type` |
| `CHANGELOG_PRODUCTS` | `--products` | `steps.evaluate.outputs.products` |
| `CHANGELOG_OWNER` | `--owner` | `github.repository_owner` |
| `CHANGELOG_REPO` | `--repo` | `github.event.repository.name` |

**Precedence**: explicit CLI arguments always take priority over environment variables. Environment variables are only used when the corresponding CLI argument is not provided.

`CHANGELOG_DESCRIPTION` has additional precedence rules related to release note extraction:

- If `--description` is provided on the command line, it always wins.
- If `--no-extract-release-notes` is passed (or `extract.release_notes: false` is set in the changelog configuration), `CHANGELOG_DESCRIPTION` is ignored. This prevents a description that was extracted by `evaluate-pr` from being applied when extraction has been disabled.
- Otherwise, `CHANGELOG_DESCRIPTION` fills `--description` when it is not set on the command line.

The filename strategy is controlled by the `filename` option in `changelog.yml` (defaulting to `timestamp`). Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) for details.

This allows the CI action to invoke `changelog add` with a minimal command line:

```sh
docs-builder changelog add --config docs/changelog.yml --output /tmp/staging --concise --strip-title-prefix
```
