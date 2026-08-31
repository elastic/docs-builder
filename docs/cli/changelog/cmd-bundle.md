Aggregates changelog YAML files matching a filter into a single bundle file. The bundle is the artifact used by the `{changelog}` directive and `docs-builder changelog render` to produce release notes.

The command has **two mutually exclusive modes**. You cannot mix them: supplying a profile name on the command line disables all filter and output flags (refer to [Options](#options) for equivalent changelog configuration settings).

## Profile-based mode

Define reusable profiles in `changelog.yml` and invoke by name. This is the recommended approach for release workflows because the filter, output path, and product metadata are all captured in configuration and don't need to be specified on the command line.

```sh
# Bundle using a named profile (version inferred for {lifecycle} placeholder)
docs-builder changelog bundle elasticsearch-release 9.2.0

# Bundle using a profile with a promotion report as the filter source
docs-builder changelog bundle elasticsearch-release 9.2.0 ./promotion-report.html
```

The second positional argument accepts:
- A version string (e.g. `9.2.0`, `9.2.0-beta.1`) — lifecycle is inferred automatically (`ga`, `beta`, `rc`)
- A promotion report URL or file path
- A plain-text URL list file (one fully-qualified GitHub PR or issue URL per line)
- A plain-text path list file (one changelog YAML path per line, ending in `.yaml` or `.yml`)

When your profile uses `{version}` in its `output_products` pattern (or you want the conventional `{product}-{version}.yaml` bundle name) and you also want to filter by a report or list file, pass both arguments (version first, then the filter file).

Example profile in `changelog.yml`:

```yaml
bundle:
  repo: elasticsearch
  owner: elastic
  directory: docs/changelog
  output_directory: docs/releases
  profiles:
    elasticsearch-release:
      output_products: "elasticsearch {version}"
```

The bundle's file name is derived by convention as `{product}-{version}.yaml` from the profile's primary output product and the version argument (for example, `docs/releases/elasticsearch-9.2.0.yaml`). Setting an explicit `output` pattern on a profile is a hard error, and no two profiles may share a primary output product — they would collide on the same conventional target.

## Option-based mode

Supply filter flags directly when you don't have a profile configured or need a one-off bundle.

Exactly one of the following filter flags is required:

- `--all` — include every changelog in the directory
- `--input-products` — match by product, version or date, and lifecycle (e.g. `"elasticsearch * *"`). A concrete version or date only matches files with `products[].versions` (or a legacy `target`); use `*` in the middle slot for PR-linked files. Local/directory only — not supported when sourcing from the CDN.
- `--prs` — filter by PR URLs or a newline-delimited file of PR URLs
- `--issues` — filter by issue URLs or a newline-delimited file of issue URLs
- `--release-version` — fetch PR references from a GitHub release tag (e.g. `v9.2.0` or `latest`)
- `--report` — filter by PRs referenced in a promotion report (URL or local file)
- `--files` — include specific changelog YAML paths, or a newline-delimited path list file
- `--start-git-ref` + `--end-git-ref` — derive the PR list from a git commit range (see [Commit-range mode](#git-ref-mode))

`--force-local` is not a filter. It forces local entry sourcing for the run (equivalent to `bundle.use_local_changelogs: true` without editing config) and is allowed in both option-based and profile-based modes.

```sh
# Bundle all changelogs in docs/changelog/
docs-builder changelog bundle --all --directory docs/changelog

# Bundle local elasticsearch changelogs (including PR-linked files that omit versions)
docs-builder changelog bundle \
  --input-products "elasticsearch * *" \
  --output docs/releases/9.2.0.yaml

# Bundle from a GitHub release
docs-builder changelog bundle \
  --release-version v9.2.0 \
  --repo elasticsearch \
  --owner elastic

# Bundle an explicit list of changelog files
docs-builder changelog bundle \
  --files "./docs/changelog/a.yaml,./docs/changelog/b.yaml" \
  --output docs/releases/serverless/2026-07-07.yaml \
  --output-products "cloud-serverless 2026-07-07"
```

## Commit-range mode [git-ref-mode]

`--start-git-ref` and `--end-git-ref` cut a bundle from a **git commit range** instead of an externally supplied PR list. This is the mode date-promotion automation (serverless, Cloud ECH/ECE) uses: the promotion system hands off two commit hashes from a protected branch, and the command derives everything else itself.

```sh
docs-builder changelog bundle serverless-release 2026-08-13 \
  --start-git-ref <previous-published-ref> \
  --end-git-ref <current-published-ref>
```

Both refs are always required together — the start ref is never inferred from previous bundles. The command:

1. Enumerates the commits in `start..end` via the GitHub compare API (paginated).
2. Resolves each commit to its merged pull request via GraphQL `associatedPullRequests`. Works for squash and merge commits on protected integration branches; commits with no associated PR are reported, never silently dropped. When a commit is associated with multiple merged PRs, the command warns and picks deterministically (merge-commit match first, then lowest PR number).
3. Sources each PR's changelog entry with a fixed precedence:
   - **A checked-in entry from the entry pool wins.** Pool entries are matched by file-name-derived PR numbers (file names survive scrubbing, so this works for private repos whose `prs` references were removed from public copies) or by the entry's `prs` references.
   - **Otherwise the entry is synthesized from PR metadata** — the same extraction path `changelog add` uses: release-note text from the PR body becomes the description, and labels map to type/areas/products/feature-id via the `pivot.*` configuration. `rules.create` label rules decide inclusion.
   - **PRs whose metadata cannot be fetched are reported as missing** with a warning.
4. Records the end ref in the bundle output as the `git_ref` metadata field.

Commit-range mode works in both profile-based and option-based commands and is mutually exclusive with every other filter. In profile-based commands the profile contributes output metadata only (`output_products`, `repo`, `owner`, `rules`, and so on) — it must not set a `products` pattern or `source: github_release`. The bundle name follows the `{product}-{version}.yaml` convention.

Re-running the same range produces the same bundle content; bundling never overwrites changelog entries.
Commit-range mode does not automatically add changelog notes whose `products[].versions` match `output_products`. Use `--files` or a path list if those files must be in the bundle.

:::{note}
Commit-range mode requires a `GITHUB_TOKEN` environment variable: the GraphQL API used for commit→PR association does not accept anonymous requests.
:::

### Dry run

Pass `--dry-run` to resolve the range and print the run report — the resolved PR list with each PR's entry source (`pool`, `inferred (PR body)`, `inferred (title)`, `excluded (rules)`, or `missing`) plus any commits without an associated PR — as Markdown, without writing a bundle. The report is suitable for a release PR body or a CI job summary.

```sh
docs-builder changelog bundle serverless-release 2026-08-13 \
  --start-git-ref abc123 --end-git-ref def456 --dry-run
```

## Bundles are self-contained

Every bundle embeds the full content of each changelog entry (`title`, `type`, `products`, and so on), plus a `file` block recording the source file name and checksum for provenance. Rendering — via the `{changelog}` directive, `changelog render`, or the CDN pipeline — never reads the original changelog files, so you can clean them up with `docs-builder changelog remove` immediately after bundling.

When you bundle from a PR list or GitHub release and the command is sourcing from the CDN, it also adds uploaded changelogs whose `products[].versions` include that version (from `output_products`). Those files are appended to the same `entries` list; `rules.bundle` applies to them too. This automatic add does not apply to git-range bundles or `--force-local`. For more detail, see [Bundle changelogs](/data/release-notes/bundle.md).

## CI usage

Pass `--plan` to emit GitHub Actions step outputs without generating the bundle. Use this in a planning step to decide whether subsequent steps require a GitHub token or network access.

| Output | Description |
|--------|-------------|
| `mode` | Resolved bundle mode: `gh-release` when no `bundle.profiles` are configured; `bundle` for profile-based bundling |
| `output_path` | Resolved output file path for the bundle |
| `needs_network` | `true` if the bundle step requires network access |
| `needs_github_token` | `true` if the bundle step requires a GitHub token |

When `mode` is `gh-release` (no profiles configured), pass only `--config` and a version — no profile name or filter flags are needed. The plan step resolves `output_path` from `bundle.output_directory` so the bundle-upload step does not need to discover the file separately.

For full configuration reference, see [Bundle changelogs](/data/release-notes/bundle.md).

## Product format [product-format]

The `changelog bundle` command has `--input-products` and `--output-products` options that accept values with the format `"product target lifecycle, ..."` where:

- `product` is the product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `target` is the target version or date (optional)
- `lifecycle` exists in [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs) (optional)

You can further limit the possible values with the [products](/data/release-notes/configure-ref.md#products) and [lifecycles](/data/release-notes/configure-ref.md#lifecycles) options in the changelog configuration file.

For example:

- `"kibana 9.2.0 ga"`
- `"cloud-serverless 2025-08-05"`
- `"cloud-enterprise 4.0.3, cloud-hosted 2025-10-31"`

If you use `"* * *"` in the `--input-products` command option or `bundle.profiles.<name>.products` configuration setting, it's equivalent to the `--all` command option.

For `--input-products` and profile `products`, a concrete version or date in the middle slot only matches files that have `products[].versions` or a legacy `target` (typically files created with `changelog note`). A `*` in that slot still matches every file, including PR-linked changelogs. This filter is local/directory only: when changelog files are sourced from the CDN, `--input-products` and an equivalent versioned profile `products` pattern are not supported. `--output-products` is bundle metadata, not a file filter.

## Lifecycle inference [lifecycle-inference]

The way that lifecycle values are inferred varies between GitHub release profiles and standard profiles.

### GitHub release profiles

For `source: github_release` profiles, the `{lifecycle}` placeholder in `output` and `output_products` is derived from the full release tag name. For example:

| Release tag | `{version}` | `{lifecycle}` |
|-------------|-------------|---------------|
| `v1.2.3` | `1.2.3` | `ga` |
| `v1.2.3-beta.1` | `1.2.3` | `beta` |
| `v1.2.3-preview.1` | `1.2.3` | `preview` |

### Standard profiles

For standard profiles, `{version}` is copied verbatim from your command argument and `{lifecycle}` is derived from that value. For example:

| Version argument | `{version}` | `{lifecycle}` |
|------------------|-------------|---------------|
| `9.2.0` | `9.2.0` | `ga` |
| `9.2.0-beta.1` | `9.2.0-beta.1` | `beta` |
| `9.2.0-preview.1` | `9.2.0-preview.1` | `preview` |
| `2026-07-21` | `2026-07-21` | `ga` |

Profile mode does not accept lifecycle on the command line. For semver and date-based releases alike, set lifecycle in the profile YAML:

- Omit lifecycle from the pattern (for example, `"cloud-serverless {version}"`) to leave the field out of the bundle.
- Use `{lifecycle}` to derive it from the version or date argument (as in the table above).
- Hardcode a lifecycle (for example, `"cloud-serverless {version} ga"` or `"cloud-serverless {version} preview"`) when you need an explicit value. Non-`ga` date-based releases are exceptional and should use a hardcoded lifecycle.

For more information about acceptable product and lifecycle values, go to [Product format](#product-format).

## PR and issue link allowlist [link-allowlist]

A changelog in a public repository might contain links to pull requests or issues in repositories that should not appear in published documentation.

Set `bundle.link_allow_repos` in `changelog.yml` to an explicit list of `owner/repo` strings. When this key is present (including as an empty list), PR and issue references are filtered at bundle time: only links whose resolved repository is in the list are kept; others are rewritten to `# PRIVATE:` sentinel strings in the bundle YAML.

## Examples

### Bundle by GitHub release [changelog-bundle-release-version]

You can use `--release-version` to fetch pull request references directly from GitHub release notes and use them as the bundle filter.
This is equivalent to building a PR list file manually and passing it with `--prs`, but without any file management.

:::{important}
Only automated GitHub release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format) are supported at this time.
:::

```sh
docs-builder changelog bundle \
  --release-version v1.34.0 \ <1>
  --repo apm-agent-dotnet \ <2>
  --owner elastic <3>
  --output-products "apm-agent-dotnet 1.34.0 ga" <4>
```

1. The tag value that is used in the `GET /repos/{owner}/{repo}/releases/tags/{tag}` releases API.
2. You must specify `--repo` or set `bundle.repo` in the changelog configuration file.
3. If you don't specify `--owner`, it uses `bundle.owner` in the changelog configuration or else defaults to `elastic`.
4. The bundle's product metadata is inferred automatically from the release tag and repository name; you can override that behavior with the `--output-products` option.

:::{note}
`--release-version` requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API.
:::

By default all changelogs that match PRs in the GitHub release notes are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/data/release-notes/configure-ref.md#rules-bundle) configuration settings.

:::{tip}
If you are not creating changelogs when you create your pull requests, consider the `docs-builder changelog gh-release` command as a one-shot alternative to the `changelog add` and `changelog bundle` commands.
It parses the release notes, creates one changelog file per pull request found, and creates a `changelog-bundle.yaml` file — all in a single step. Refer to [changelog gh-release](/cli/changelog/gh-release.md).
:::

### Bundle by issues [changelog-bundle-issues]

You can use the `--issues` option to create a bundle of changelogs that relate to those GitHub issues.
Issues can be identified by a full URL (such as `https://github.com/owner/repo/issues/123`), a short format (such as `owner/repo#123`), or just a number (in which case `--owner` and `--repo` are required — or set via `bundle.owner` and `bundle.repo` in the configuration).

```sh
docs-builder changelog bundle --issues "12345,12346" \
  --repo elasticsearch \
  --owner elastic \
  --output-products "elasticsearch 9.2.2 ga"
```

Alternatively, you can specify a path to a newline-delimited file that contains the issue URLS (for example, `--issues /path/to/file.txt`).
In this case, you cannot use short URLs or numbers, each line must have a full URL.

By default all changelogs that match issues in the list are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/data/release-notes/configure-ref.md#rules-bundle) configuration settings.

### Bundle by pull requests [changelog-bundle-pr]

You can use the `--prs` option to create a bundle of the changelogs that relate to those pull requests.

Pull requests can be identified by a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), or just a number.

```sh
docs-builder changelog bundle --prs "108875,135873,136886" \ <1>
  --repo elasticsearch \ <2>
  --owner elastic \ <3>
  --output-products "elasticsearch 9.2.2 ga" <4>
```

1. The comma-separated list of pull request numbers to seek.
2. The repository in the pull request URLs. Not required when using full PR URLs, or when `bundle.repo` is set in the changelog configuration.
3. The owner in the pull request URLs. Not required when using full PR URLs, or when `bundle.owner` is set in the changelog configuration.
4. The product metadata for the bundle. If it is not provided, it will be derived from all the changelog product values.

Alternatively, you can specify a path to a newline-delimited file that contains the PR URLS (for example, `--prs /path/to/file.txt`).
In this case, you cannot use short URLs or numbers, each line must have a full URL.
For example:

```txt
https://github.com/elastic/elasticsearch/pull/108875
https://github.com/elastic/elasticsearch/pull/135873
https://github.com/elastic/elasticsearch/pull/136886
https://github.com/elastic/elasticsearch/pull/137126
```

By default all changelogs that match PRs in the list are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/data/release-notes/configure-ref.md#rules-bundle) configuration settings.

If you have changelog files that reference those pull requests, the command creates a file like this:

```yaml
products:
- product: elasticsearch
  target: 9.2.2
  lifecycle: ga
entries:
- file:
    name: 1765507819-fix-ml-calendar-event-update-scalability-issues.yaml
    checksum: 069b59edb14594e0bc3b70365e81626bde730ab7
  type: bug-fix
  title: Fix ML calendar event update scalability issues
  products:
  - product: elasticsearch
    target: 9.2.2
  prs:
  - https://github.com/elastic/elasticsearch/pull/108875
- file:
    name: 1765507798-convert-bytestransportresponse-when-proxying-respo.yaml
    checksum: c6dbd4730bf34dbbc877c16c042e6578dd108b62
  type: bug-fix
  title: Convert BytesTransportResponse when proxying responses
  products:
  - product: elasticsearch
    target: 9.2.2
  prs:
  - https://github.com/elastic/elasticsearch/pull/135873
```

### Bundle by product [changelog-bundle-product]

You can use the `--input-products` option to create a bundle of changelogs that match the product details.
When using `--input-products`, you must provide all three parts: product, target, and lifecycle.
Each part can be a wildcard (`*`) to match any value.

A concrete target (version or date, including a prefix such as `2025-11-*`) only matches files that declare `products[].versions` or a legacy `target`. Use `"elasticsearch * *"` (or `"* * *"`) when you need PR-linked changelogs that omit `versions`. `--input-products` is not supported when sourcing from the CDN; use `--prs`, `--release-version`, a report, `--files`, or a profile without a versioned `products` pattern instead.

:::{tip}
If you use profile-based bundling, provide this information in the `bundle.profiles.<name>.products` field.
:::

```sh
docs-builder changelog bundle \
  --input-products "cloud-serverless 2025-12-02 ga, cloud-serverless 2025-12-06 beta" <1>
```

1. Include local changelogs that have the `cloud-serverless` product identifier and `products[].versions` (or a legacy `target`) of either December 2 2025 (lifecycle `ga`) or December 6 2025 (lifecycle `beta`). This does not match PR-linked changelogs that omit `versions`. For more information about product values, refer to [Product format](/cli/changelog/bundle.md#product-format).

You can use wildcards in any of the three parts:

```sh
# Bundle any changelogs that have exact matches for either of these clauses
docs-builder changelog bundle --input-products "cloud-serverless 2025-12-02 ga, elasticsearch 9.3.0 beta"

# Bundle all elasticsearch changelogs regardless of target or lifecycle
docs-builder changelog bundle --input-products "elasticsearch * *"

# Bundle all cloud-serverless 2025-12-02 changelogs with any lifecycle
docs-builder changelog bundle --input-products "cloud-serverless 2025-12-02 *"

# Bundle any cloud-serverless changelogs with target starting with "2025-11-" and "ga" lifecycle
docs-builder changelog bundle --input-products "cloud-serverless 2025-11-* ga"

# Bundle all changelogs (equivalent to --all)
docs-builder changelog bundle --input-products "* * *"
```

If you have changelog files that reference those product details, the command creates a file like this:

```yaml
products: <1>
- product: cloud-serverless
  target: 2025-12-02
- product: cloud-serverless
  target: 2025-12-06
entries:
- file:
    name: 1765495972-fixes-enrich-and-lookup-join-resolution-based-on-m.yaml
    checksum: 6c3243f56279b1797b5dfff6c02ebf90b9658464
  type: bug-fix
  title: Fixes enrich and lookup join resolution based on mappings
  products:
  - product: cloud-serverless
    target: 2025-12-02
  prs:
  - https://github.com/elastic/elasticsearch/pull/136001
- file:
    name: 1765507778-break-on-fielddata-when-building-global-ordinals.yaml
    checksum: 70d197d96752c05b6595edffe6fe3ba3d055c845
  type: bug-fix
  title: Break on fielddata when building global ordinals
  products:
  - product: cloud-serverless
    target: 2025-12-06
  prs:
  - https://github.com/elastic/elasticsearch/pull/136002
```

1. By default these values match your `--input-products` (even if the changelogs have more products).
To specify different product metadata, use the `--output-products` option.

:::{note}
When a changelog matches multiple `--input-products` filters, it appears only once in the bundle. This deduplication applies even when using `--all` or `--prs`.
:::

### Bundle by report

You can use `--report` to filter by a promotion report:

```sh
# Extract PRs from a downloaded report and use them as the filter
docs-builder changelog bundle \
  --report ./promotion-report.html \
  --directory ./docs/changelog \
  --output ./docs/releases/bundle.yaml
```

By default all changelogs that match PRs in the promotion report are included in the bundle.
To apply additional filtering by the changelog type, areas, or products, add [rules.bundle](/data/release-notes/configure-ref.md#rules-bundle) configuration settings.

### Bundle by file paths [changelog-bundle-files]

Use `--files` when you know the exact changelog files to include and they may not have `prs` or `issues` metadata.
Path-list / `--files` selection is by file name and is not filtered by `products[].versions`, so listed files may declare different versions or dates.

```sh
docs-builder changelog bundle \
  --files "./docs/changelog/a.yaml,./docs/changelog/b.yaml" \
  --output docs/releases/serverless/2026-07-07.yaml \
  --output-products "cloud-serverless 2026-07-07"
```

You can also pass a newline-delimited path list file:

```sh
docs-builder changelog bundle --files ./docs/temp/changelog_files.txt --output ...
```

In profile mode, pass the same path list as a positional argument:

```sh
docs-builder changelog bundle serverless-release 2026-07-07 ./docs/temp/changelog_files.txt
```

`--files` / path-list selection follows the standard entry-sourcing rules. When entries are sourced from the CDN (the default when `bundle.repo` resolves), the listed paths are matched to CDN pool entries by file name and do not need to exist locally — useful for private repositories whose entries exist only in S3 and whose public copies have PR/issue references scrubbed, so PR-based filters cannot match. With local sourcing (`--force-local`, `--directory`, or `bundle.use_local_changelogs`), the listed files are read from disk and must exist. In either mode, a listed entry that cannot be found fails the run, and `rules.bundle` still applies after selection.

### Force local entry sourcing [changelog-bundle-force-local]

When a repository defaults to CDN entry sourcing, you can use `--force-local` to read changelog YAML files from the local folder.
This option overrides the `bundle.use_local_changelogs` setting in your `changelog.yml` and is useful for ad hoc bundles that include freshly authored local files that are not on the CDN yet.

```sh
docs-builder changelog bundle serverless-release 2026-07-07 ./docs/temp/prs.txt --force-local
```

`--force-local` is allowed in both option-based and profile-based commands.
Use it with path-list / `--files` filters when the listed files should be read from disk instead of matched against the CDN pool.
Because local sourcing skips the CDN note auto-merge, include those files with `--files` or a path list if they belong in the bundle.

### Hide features [changelog-bundle-hide-features]

You can use the `--hide-features` option to embed feature IDs that should be hidden when the bundle is rendered. This is useful for features that are not yet ready for public documentation.

```sh
docs-builder changelog bundle \
  --input-products "elasticsearch * *" \
  --hide-features "feature:hidden-api,feature:experimental" \ <1>
  --output /path/to/bundles/9.3.0.yaml
```

1. Feature IDs to hide. Changelogs with matching `feature-id` values will be commented out when rendered.

The bundle output will include a `hide-features` field:

```yaml
products:
- product: elasticsearch
  target: 9.3.0
hide-features:
  - feature:hidden-api
  - feature:experimental
entries:
- file:
    name: 1765495972-new-feature.yaml
    checksum: 6c3243f56279b1797b5dfff6c02ebf90b9658464
  type: feature
  title: New feature behind a flag
  feature-id: feature:hidden-api
  products:
  - product: elasticsearch
    target: 9.3.0
```

When this bundle is rendered (either via the `changelog render` command or the `{changelog}` directive), changelogs with `feature-id` values matching any of the listed features will be commented out in the output.

:::{note}
The `--hide-features` option on the `render` command and the `hide-features` field in bundles are **combined**. If you specify `--hide-features` on both the `bundle` and `render` commands, all specified features are hidden. The `{changelog}` directive automatically reads `hide-features` from all loaded bundles and applies them.
:::
