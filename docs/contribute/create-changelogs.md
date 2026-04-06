# Create changelogs

The changelogs associated with the `docs-builder changelog` commands use the following schema:

:::{dropdown} Changelog schema
::::{include} /contribute/_snippets/changelog-fields.md
::::
:::

:::{important}
Some of the fields in the schema accept only a specific set of values:

- Product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Invalid products will cause the `docs-builder changelog add` command to fail.
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs), [ChangelogEntrySubtype.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntrySubtype.cs), and [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs) respectively. Invalid values will cause the `docs-builder changelog add` command to fail.
:::

## Create changelog files [changelog-add]

You can use the `docs-builder changelog add` command to create a changelog file.

If you specify `--prs` or `--issues`, the command tries to fetch information from GitHub. It derives the changelog `title` from the pull request or issue title, maps labels to areas, products, and type (if configured), and extracts linked references.
With `--issues`, it extracts linked PRs from the issue body (for example, "Fixed by #123").
With `--prs`, it extracts linked issues from the PR body (for example, "Fixes #123").

When `--repo`, `--owner`, or `--output` are not specified, the command reads them from the `bundle` section of `changelog.yml` (`bundle.repo`, `bundle.owner`, `bundle.directory`). This applies to all modes — `--prs`, `--issues`, and `--release-version` alike. If no config value is available, `--owner` defaults to `elastic` and `--output` defaults to the current directory.

:::{tip}
Ideally this task will be automated such that it's performed by a bot or GitHub action when you create a pull request.
If you run it from the command line, you must precede any special characters (such as backquotes) with a backslash escape character (`\`).
:::

For up-to-date command usage information, use the `-h` option or refer to [](/cli/changelog/add.md).

### Authorization

If you use the `--prs`, `--issues`, or `--release-version` options, the `docs-builder changelog add` command interacts with GitHub services.
The `--release-version` option on the `docs-builder changelog add`, `bundle`, and `remove` commands also interacts with GitHub services.
Log into GitHub or set the `GITHUB_TOKEN` (or `GH_TOKEN` ) environment variable with a sufficient personal access token (PAT).
Otherwise, there will be fetch failures when you access private repositories and you might also encounter GitHub rate limiting errors.

For example, to create a new token with the minimum authority to read pull request details:

1. Go to **GitHub Settings** > **Developer settings** > **Personal access tokens** > [Fine-grained tokens](https://github.com/settings/personal-access-tokens).
2. Click **Generate new token**.
3. Give your token a descriptive name (such as "docs-builder changelog").
4. Under **Resource owner** if you're an Elastic employee, select **Elastic**.
5. Set an expiration date.
6. Under **Repository access**, select **Only select repositories** and choose the repositories you want to access.
7. Under **Permissions** > **Repository permissions**, set **Pull requests** to **Read-only**. If you want to be able to read issue details, do the same for **Issues**.
8. Click **Generate token**.
9. Copy the token to a safe location and use it in the `GITHUB_TOKEN` environment variable.

### Product format

The `docs-builder changelog add` has a `--products` option and the `docs-builder changelog bundle` has `--input-products` and `--output-products` options that all use the same format.

They accept values with the format `"product target lifecycle, ..."` where:

- `product` is the product ID from [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml) (required)
- `target` is the target version or date (optional)
- `lifecycle` is one of: `preview`, `beta`, or `ga` (optional)

Examples:

- `"kibana 9.2.0 ga"`
- `"cloud-serverless 2025-08-05"`
- `"cloud-enterprise 4.0.3, cloud-hosted 2025-10-31"`

### Filenames

The `docs-builder changelog add` command names generated files according to the `filename` strategy in `changelog.yml`:

| Strategy | Example filename | Description |
|---|---|---|
| `timestamp` (default) | `1735689600-fixes-enrich-and-lookup-join-resolution.yaml` | Uses a Unix timestamp with a sanitized title slug. |
| `pr` | `137431.yaml` | Uses the PR number. |
| `issue` | `2571.yaml` | Uses the issue number. |

Set the strategy in your changelog configuration file:

```yaml
filename: timestamp
```

Refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml) for full documentation.

You can override the configured strategy per invocation with the `--use-pr-number` or `--use-issue-number` CLI flags:

```sh
docs-builder changelog add \
  --prs https://github.com/elastic/elasticsearch/pull/137431 \
  --products "elasticsearch 9.2.3" \
  --use-pr-number

docs-builder changelog add \
  --issues https://github.com/elastic/docs-builder/issues/2571 \
  --products "elasticsearch 9.3.0" \
  --config docs/changelog.yml \
  --use-issue-number
```

:::{important}
`--use-pr-number` and `--use-issue-number` are mutually exclusive; specify only one. Each requires `--prs` or `--issues`. The numbers are extracted from the URLs or identifiers you provide, or from linked references in the issue or PR body when extraction is enabled.

**Precedence**: CLI flags (`--use-pr-number` / `--use-issue-number`) > `filename` in `changelog.yml` > default (`timestamp`).
:::

### Examples

#### Create a changelog for multiple products [example-multiple-products]

```sh
docs-builder changelog add \
  --title "Fixes enrich and lookup join resolution based on minimum transport version" \ <1>
  --type bug-fix \ <2>
  --products "elasticsearch 9.2.3, cloud-serverless 2025-12-02" \ <3>
  --areas "ES|QL"
  --prs "https://github.com/elastic/elasticsearch/pull/137431" <4>
```

1. This option is required only if you want to override what's derived from the PR title.
2. The type values are defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs).
3. The product values are defined in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
4. The `--prs` value can be a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), just a number (in which case you must also provide `--owner` and `--repo` options), or a path to a file containing newline-delimited PR URLs or numbers. Multiple PRs can be provided comma-separated, or you can specify a file path. You can also mix both formats by specifying `--prs` multiple times. One changelog file will be created for each PR.

#### Create a changelog with PR label mappings [example-map-label]

You can configure label mappings in your changelog configuration file:

```yaml
pivot:
  # Keys are type names, values can be:
  #   - simple string: comma-separated label list (e.g., ">bug, >fix")
  #   - empty/null: no labels for this type
  #   - object: { labels: "...", subtypes: {...} } for breaking-change type only
  types:
    # Example mappings - customize based on your label naming conventions
    breaking-change:
      labels: ">breaking, >bc"
    bug-fix: ">bug"
    enhancement: ">enhancement"
  
  # Area definitions with labels
  # Keys are area display names, values are label strings
  # Multiple labels can be comma-separated
  areas:
    # Example mappings - customize based on your label naming conventions
    Autoscaling: ":Distributed Coordination/Autoscaling"
    "ES|QL": ":Search Relevance/ES|QL"

  # Product definitions with labels (optional)
  # Keys are product spec strings; values are label strings or lists.
  # A product spec string is: "<product-id> [<target-version>] [<lifecycle>]"
  products:
    'elasticsearch':
      - ":stack/elasticsearch"
    'kibana':
      - ":stack/kibana"
    # Include a target version if known:
    # 'cloud-serverless 2025-06 ga':
    #   - ":cloud/serverless"
```

When you use the `--prs` option to derive information from a pull request, it can make use of those mappings. Similarly, when you use the `--issues` option (without `--prs`), the command derives title, type, areas, and products from the GitHub issue labels using the same mappings.

The following example omits `--products`, so the command derives them from the PR labels:

```sh
docs-builder changelog add \
  --prs https://github.com/elastic/elasticsearch/pull/139272 \
  --config test/changelog.yml \
  --strip-title-prefix
```

In this case, the changelog file derives the title, type, areas, and products from the pull request. If none of the PR's labels match `pivot.products`, the command falls back to `products.default` or repository name inference from `--repo` (refer to [Products resolution](/cli/changelog/add.md#products-resolution) for more details).
The command also looks for patterns like `Fixes #123`, `Closes owner/repo#456`, `Resolves https://github.com/.../issues/789` in the pull request to derive its issues. Similarly, when using `--issues`, the command extracts linked PRs from the issue body (for example, "Fixed by #123"). You can turn off this behavior in either case with the `--no-extract-issues` flag or by setting `extract.issues: false` in the changelog configuration file. The `extract.issues` setting applies to both directions: issues extracted from PR bodies (when using `--prs`) and PRs extracted from issue bodies (when using `--issues`).

The `--strip-title-prefix` option in this example means that if the PR title has a prefix in square brackets (such as `[ES|QL]` or `[Security]`), it is automatically removed from the changelog title. Multiple square bracket prefixes are also supported (for example `[Discover][ESQL] Title` becomes `Title`). If a colon follows the closing bracket, it is also removed.

By default, `--strip-title-prefix` is disabled. You can enable it globally by setting `extract.strip_title_prefix: true` in the changelog configuration file, which will apply the prefix stripping to all `changelog add` and `changelog gh-release` commands without requiring the CLI flag. The CLI flag `--strip-title-prefix` overrides the configuration setting.

:::{note}
The `--strip-title-prefix` option only applies when the title is derived from the PR (when `--title` is not explicitly provided). If you specify `--title` explicitly, that title is used as-is without any prefix stripping.
:::

#### Extract release notes from PR descriptions [example-extract-release-notes]

When you use the `--prs` option, by default the `docs-builder changelog add` command automatically extracts text from the PR descriptions and use it in your changelog.

In particular, it looks for content in these formats in the PR description:

- `Release Notes: This is the extracted sentence.`
- `Release-Notes: This is the extracted sentence.`
- `release notes: This is the extracted sentence.`
- `Release Note: This is the extracted sentence.`
- `Release Notes - This is the extracted sentence.`
- `## Release Note` (as a markdown header)

The extracted content is handled differently based on its length:

- **Short release notes (≤120 characters, single line)**: Used as the changelog title (only if `--title` is not explicitly provided)
- **Long release notes (>120 characters or multi-line)**: Used as the changelog description (only if `--description` is not explicitly provided)
- **No release note found**: No changes are made to the title or description

:::{note}
If you explicitly provide `--title` or `--description`, those values take precedence over extracted release notes.
You can turn off the release note extraction in the changelog configuration file or by using the `--no-extract-release-notes` option.
:::

#### Control changelog creation [example-block-label]

You can prevent changelog creation for certain PRs based on their labels.

If you run the `docs-builder changelog add` command with the `--prs` option and a PR has a blocking label for any of the resolved products (from `--products`, `pivot.products` label mapping, or `products.default`), that PR will be skipped and no changelog file will be created for it.
A warning message will be emitted indicating which PR was skipped and why.

For example, your configuration file can contain a `rules` section like this:

```yaml
rules:
  # Global match default for multi-valued fields (labels, areas).
  #   any (default) = match if ANY item matches the list
  #   all           = match only if ALL items match the list
  # match: any

  # Create — controls which PRs generate changelogs.
  create:
    # Labels that block changelog creation (comma-separated string)
    exclude: ">non-issue"
    # Product-specific overrides
    products:
      'cloud-serverless':
        exclude: ">non-issue, >test"
```

Those settings affect commands with the `--prs` or `--issues` options, for example:

```sh
docs-builder changelog add --prs "1234, 5678" \
  --products "cloud-serverless" \
  --owner elastic --repo elasticsearch \
  --config test/changelog.yml
```

If PR 1234 has the `>non-issue` or `>test` labels, it will be skipped and no changelog will be created.
If PR 5678 does not have any blocking labels, a changelog is created.

You can also use **include** mode instead of **exclude** mode.
For example, to only create changelogs for PRs with specific labels:

```yaml
rules:
  create:
    include: "@Public, @Notable"
```

#### Create changelogs from a file [example-file-add]

You can create multiple changelogs in a single command by providing a newline-delimited file that contains pull requests or issues.
For example:

```sh
# Create a file with PRs (one per line)
cat > prs.txt << EOF
https://github.com/elastic/elasticsearch/pull/1234
https://github.com/elastic/elasticsearch/pull/5678
EOF

# Use the file with --prs
docs-builder changelog add --prs prs.txt \
  --products "elasticsearch 9.2.0 ga" \
  --config test/changelog.yml
```

In this example, the command creates one changelog for each pull request in the list.

#### Create changelogs from GitHub release notes [changelog-add-release-version]

If you have GitHub releases with automated release notes (the default format or [Release Drafter](https://github.com/release-drafter/release-drafter) format), the changelog commands can derive the PR list from those release notes with the `--release-version` option.
For example:

```sh
docs-builder changelog add \
  --release-version v1.34.0 \
  --repo apm-agent-dotnet --owner elastic
```

This command creates one changelog file per PR found in the `v1.34.0` GitHub release notes.
The product, target version, and lifecycle in each changelog are inferred automatically from the release tag and the repository name.
For example, a tag of `v1.34.0` in the `apm-agent-dotnet` repo creates changelogs with `product: apm-agent-dotnet`, `target: 1.34.0`, and `lifecycle: ga`.

:::{note}
`--release-version` requires `--repo` (or `bundle.repo` set in `changelog.yml`) and is mutually exclusive with `--prs` and `--issues`.
The option precedence is: CLI option > `changelog.yml` bundle section > built-in default. This applies to `--repo`, `--owner`, and `--output` for all `changelog add` modes.
:::

You can use the `docs-builder changelog gh-release` command as a one-shot alternative to `changelog add` and `changelog bundle` commands.
The command parses the release notes, creates one changelog file per pull request found, and creates a `changelog-bundle.yaml` file — all in a single step. Refer to [](/cli/changelog/gh-release.md)

:::{note}
This command requires a `GITHUB_TOKEN` or `GH_TOKEN` environment variable (or an active `gh` login) to fetch release details from the GitHub API. Refer to [Authorization](#authorization) for details.
:::
