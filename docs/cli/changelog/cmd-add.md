## Description

Create a changelog file that describes a single item in the release documentation.
For details and examples, go to [](/data/release-notes/create.md).

For changelogs that don't need a PR link (for example, known issues or security advisories), use [`changelog note`](/cli/changelog/note.md) instead.

## Options

: `--no-extract-release-notes`
  Turn off extraction of release notes from PR or issue descriptions.
  By default, the behavior is determined by the [extract.release_notes](/data/release-notes/configure-ref.md#extract) changelog configuration setting. Release notes are extracted when using `--prs` or `--report` (and from issues when using `--issues`).

: `--products`
  Products affected in format `"product [lifecycle], ..."` (for example, `"cloud-serverless, kibana"` or `"elasticsearch ga"`).
  Do not include a version or date in the middle slot; that is an error.
  The valid product identifiers are listed in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
  For more information about valid product and lifecycle values, go to [Product format](#product-format-and-resolution).

: `--strict-fetch`
  Treat a failure to fetch any PR or issue from GitHub (when using `--prs`, `--issues`, or `--report`) as an error that exits non-zero, instead of a warning.
  Refer to [Fetch failures](#fetch-failures).

: `--use-pr-number`
  Use PR numbers for filenames instead of the configured `filename` strategy.
  Requires `--prs`, `--issues`, or `--report`. Mutually exclusive with `--use-issue-number`.
  Refer to [Filenames](#filenames).

: `--use-issue-number`
  Use issue numbers for filenames instead of the configured `filename` strategy.
  Requires `--prs` or `--issues`. Mutually exclusive with `--use-pr-number`.
  Refer to [Filenames](#filenames).

## Filenames

By default, output files are named according to the `filename` strategy in `changelog.yml`:

| Strategy | Example filename | Description |
|---|---|---|
| `timestamp` (default) | `1735689600-fixes-enrich-and-lookup-join-resolution.yaml` | Uses a Unix timestamp with a sanitized title slug. |
| `pr` | `137431.yaml` | Uses the PR number. |
| `issue` | `2571.yaml` | Uses the issue number. |

Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) or [](/data/release-notes/configure-ref.md).

You can override those settings with the `--use-pr-number` or `--use-issue-number` flags:

```sh
docs-builder changelog add \
  --prs 1234 \
  --products "elasticsearch ga" \
  --use-pr-number

docs-builder changelog add \
  --issues 4567 \
  --products "elasticsearch" \
  --use-issue-number
```

:::{important}
`--use-pr-number` and `--use-issue-number` are mutually exclusive; specify only one. `--use-pr-number` requires `--prs`, `--issues`, or `--report`. `--use-issue-number` requires `--prs` or `--issues`. The numbers are extracted from the URLs or identifiers you provide or from linked references in the issue or PR body when extraction is enabled.

**Precedence**: CLI flags (`--use-pr-number` / `--use-issue-number`) > `filename` in `changelog.yml` > default (`timestamp`).
:::

## Product format and resolution

The `--products` option accepts values with the format `"product [lifecycle], ..."` where:

- `product` is a product ID that exists in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `lifecycle` exists in [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs) (optional)

You can further limit the possible values with the [products](/data/release-notes/configure-ref.md#products) and [lifecycles](/data/release-notes/configure-ref.md#lifecycles) options in the changelog configuration file.

For example:

- `"kibana"`
- `"kibana ga"`
- `"cloud-serverless, kibana"`

The `changelog add` command resolves product values in the following order:

1. The `--products` CLI option always takes priority.
1. If `pivot.products` is defined in the changelog configuration file and the PR or issue has labels that match, those products are used. Multiple matching entries are all applied.
1. If `products.default` is defined in the changelog configuration file, those default products are used.
1. If `--repo` is specified (or `bundle.repo` is set in the changelog configuration file), the repository name is matched against known product IDs in `products.yml` and the derived value is used.

The same order applies when using `--report` (after PR URLs are resolved from the promotion report), and when using batch `--prs` with multiple pull requests.

If none of these steps yield at least one product, the command returns an error.

## Feature ID resolution

The optional `feature-id` field associates a changelog with a feature flag or project.
The `changelog add` command resolves it in the following order:

1. The `--feature-id` CLI option always takes priority.
1. If `pivot.features` is defined in the changelog configuration file and the PR or issue has labels that match, that feature ID is used.
   If the PR or issue has multiple labels that map to different feature IDs, the first match is used and a warning is emitted.

When you map unreleased features in `pivot.features`, also list those feature IDs under the relevant bundle profile's [`hide_features`](/data/release-notes/configure-ref.md#bundle-profiles) setting so the entries stay commented out until the feature is ready to publish.
For more information, refer to [](/data/release-notes/bundle.md#changelog-bundle-hide-features).

## Configuration checks

By default, the command checks `docs/changelog.yml` for a configuration file. You can specify a different path with `--config`.

If a configuration file exists, the command validates its values before generating changelog files:

- If the configuration file contains `lifecycles`, `products`, `subtype`, or `type` values that don't match the known valid values, validation fails.
- If the configuration file contains `areas` values and they don't match what you specify in `--areas`, validation fails.
- If the configuration file contains `lifecycles` or `products` values that are a subset of the available values and you try to create a changelog with values outside that subset, validation fails.

In each of these cases where validation fails, a changelog file is not created.

If the configuration file contains `rules.create` definitions and a PR or issue has a blocking label, that PR is skipped and no changelog file is created for it.
For more information, refer to [](/data/release-notes/create.md#rules).

## Fetch failures

`rules.create` label filtering and automatic `title`/`type` derivation both depend on fetching each PR or issue from GitHub.
When a fetch fails (for example, a missing or unauthorized `GITHUB_TOKEN`, a private or cross-repository reference, or API rate limiting), the affected entry **bypasses `rules.create` filtering** and is written with its `title` and `type` commented out.
Such entries later cause `changelog bundle` to fail with `missing required field: title`.

By default, each fetch failure is a warning and a single summary is emitted at the end of bulk creation (for example, `3 of 225 pull request(s) could not be fetched from GitHub`).
The command still exits `0` so a best-effort changelog is produced for offline or partial-access workflows.

Pass `--strict-fetch` to escalate fetch failures to an error so the command exits non-zero.
Use this in CI so a token or rate-limit problem fails the run loudly instead of silently producing unfiltered changelogs with missing titles.
The generated files are still written so you can inspect them.

```sh
docs-builder changelog add --report ./promotion-report.html --strict-fetch
```

:::{tip}
If you hit this, verify that `GITHUB_TOKEN` is set and can access every repository referenced by your PRs or promotion report, then delete the generated changelog files and re-run.
:::

## CI auto-detection

When running inside GitHub Actions, `changelog add` automatically reads the following environment variables to fill in arguments not provided on the command line:

| Environment variable | Fills | Set from |
| --- | --- | --- |
| `CHANGELOG_PR_NUMBER` | `--prs` | `github.event.pull_request.number` |
| `CHANGELOG_TITLE` | `--title` | `steps.evaluate.outputs.title` |
| `CHANGELOG_DESCRIPTION` | `--description` | `steps.evaluate.outputs.description` |
| `CHANGELOG_TYPE` | `--type` | `steps.evaluate.outputs.type` |
| `CHANGELOG_PRODUCTS` | `--products` | `steps.evaluate.outputs.products` |
| `CHANGELOG_OWNER` | `--owner` | `github.repository_owner` |
| `CHANGELOG_REPO` | `--repo` | `github.event.repository.name` |

Explicit CLI arguments always take priority over environment variables.

`CHANGELOG_DESCRIPTION` has additional precedence rules related to release note extraction:

- If `--description` is provided on the command line, it always wins.
- If `--no-extract-release-notes` is passed (or `extract.release_notes: false` is set in the changelog configuration), `CHANGELOG_DESCRIPTION` is ignored.
- Otherwise, `CHANGELOG_DESCRIPTION` fills `--description` when it is not set on the command line.

This allows the CI action to invoke `changelog add` with a minimal command line:

```sh
docs-builder changelog add --config docs/changelog.yml --output /tmp/staging --concise --strip-title-prefix
```
