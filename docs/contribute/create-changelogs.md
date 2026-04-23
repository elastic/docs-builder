# Create changelogs

You can use `docs-builder changelog` commands to create data files ("changelogs") for each notable change in your GitHub repository.
These files are ultimately used to generate release documentation.

This page describes how to create these files both from the [command line](#command-line) and from [GitHub actions](#github-actions).

## Before you begin

Create a changelog configuration file to define all the default behavior and PR label mappings.
Refer to [](/contribute/configure-changelogs.md).

## Create changelog files from command line [command-line]

These steps describe how to use the [changelog add](/cli/changelog/add.md) command.
If you already have automated release notes for GitHub releases, you can use the [changelog gh-release](/cli/changelog/gh-release.md) command instead.

1. If you're accessing private repositories or creating a large number of changelogs, log into GitHub or set the `GITHUB_TOKEN` (or `GH_TOKEN` ) environment variable with a sufficient personal access token. Refer to [Authorization](/contribute/configure-changelogs.md#authorization).

1. Run the `changelog add` command in your GitHub repo's root directory.
   For example:

    ```sh
    docs-builder changelog add \
    --title "Improve SAML error handling by adding metadata"
    --type enhancement
    --products "elasticsearch 9.2.8, cloud-serverless 2026-02-02"
    ```

    Title, type, and products are the minimal details required for each changelog file.

    :::{tip}
    Any special characters (such as backquotes) must be preceded with a backslash escape character (`\`).
    :::

    Alternatively, pull the information from GitHub:

    ```sh
    docs-builder changelog add \
    --prs https://github.com/elastic/elasticsearch/pull/137598
    ```

    The `--prs` value can be a full URL (such as `https://github.com/owner/repo/pull/123`), a short format (such as `owner/repo#123`), just a number, or a path to a file containing newline-delimited PR URLs. Multiple PRs can be provided comma-separated. One changelog is created for each PR.

    When you specify `--prs` or `--issues`, the command tries to fetch information from GitHub.
    It derives the title from the pull request or issue title, extracts linked references, and derives the areas, products, and type from labels (if mappings are defined in the configuration file).

    For the most up-to-date command syntax, use the `-h` option or refer to [](/cli/changelog/add.md).

1. [Review the output file](#review).

## Create changelogs from GitHub actions [github-actions]

When automated via the [changelog GitHub Actions](https://github.com/elastic/docs-actions/tree/main/changelog), changelog creation is a two-step process:

1. `changelog evaluate-pr` inspects the PR (title, labels, body) and produces outputs such as `title`, `type`, `description`, and `products`.
2. `changelog add` reads those outputs from `CHANGELOG_*` environment variables and generates the changelog YAML file.

The `description` output from step 1 contains the release note extracted from the PR body (when `extract.release_notes` is enabled).
If extraction is disabled (either by setting `extract.release_notes: false` in `changelog.yml` or by passing `--no-extract-release-notes` to `changelog add`), the `CHANGELOG_DESCRIPTION` environment variable is ignored and the extracted description is not written to the changelog.

Refer to [CI auto-detection](/cli/changelog/add.md#ci-auto-detection) for the full list of environment variables and precedence rules.

## Review the content [review]

1. Find the files that were created by the command or GitHub action.

   You can specify the file location with command options (`--output`) or configuration options (`bundle.directory`).
   Likewise you can control the file names with command options (`--use-issue-number` or `--use-pr-number`) or the `filename` configuration option.
   Refer to the [Filenames](/cli/changelog/add.md#filenames).

1. Verify that the files contain content that is accurate and user-friendly.
   This review is especially important when you're pulling content from GitHub, since there might be some missing or extraneous information.

Changelog files use the following schema:

:::{dropdown} Changelog schema
::::{include} /contribute/_snippets/changelog-fields.md
::::
:::

For content guidelines, go to [Changelogs](https://www.elastic.co/docs/contribute-docs/content-types/changelogs).

:::{note}
Some of the fields in the schema accept only a specific set of values:

- Product values must exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml).
- Type, subtype, and lifecycle values must match the available values defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs), [ChangelogEntrySubtype.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntrySubtype.cs), and [Lifecycle.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/Lifecycle.cs) respectively.
:::

## Examples

### Create a changelog with PR label mappings [example-map-label]

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

In this case, the changelog file derives the title, type, areas, and products from the pull request.
If none of the PR's labels match `pivot.products`, the command falls back to `products.default` or repository name inference from `--repo` (refer to [Products resolution](/cli/changelog/add.md#products-resolution) for more details).
The command also looks for patterns like `Fixes #123`, `Closes owner/repo#456`, `Resolves https://github.com/.../issues/789` in the pull request to derive its issues. Similarly, when using `--issues`, the command extracts linked PRs from the issue body (for example, "Fixed by #123"). You can turn off this behavior in either case with the `--no-extract-issues` flag or by setting `extract.issues: false` in the changelog configuration file. The `extract.issues` setting applies to both directions: issues extracted from PR bodies (when using `--prs`) and PRs extracted from issue bodies (when using `--issues`).

The `--strip-title-prefix` option in this example means that if the PR title has a prefix in square brackets (such as `[ES|QL]` or `[Security]`), it is automatically removed from the changelog title. Multiple square bracket prefixes are also supported (for example `[Discover][ESQL] Title` becomes `Title`). If a colon follows the closing bracket, it is also removed.

By default, `--strip-title-prefix` is disabled. You can enable it globally by setting `extract.strip_title_prefix: true` in the changelog configuration file, which will apply the prefix stripping to all `changelog add` and `changelog gh-release` commands without requiring the CLI flag. The CLI flag `--strip-title-prefix` overrides the configuration setting.

:::{note}
The `--strip-title-prefix` option only applies when the title is derived from the PR (when `--title` is not explicitly provided). If you specify `--title` explicitly, that title is used as-is without any prefix stripping.
:::

### Extract release notes from PR descriptions [example-extract-release-notes]

When you use the `--prs` option, by default the `docs-builder changelog add` command automatically extracts text from the PR descriptions and use it in your changelog.

In particular, it looks for content in these formats in the PR description:

- `Release Notes: This is the extracted sentence.`
- `Release-Notes: This is the extracted sentence.`
- `release notes: This is the extracted sentence.`
- `Release Note: This is the extracted sentence.`
- `Release Notes - This is the extracted sentence.`
- `## Release Note` (as a markdown header)

How the extracted text is used:

- **Release note found**: The extracted text is used as the changelog description (only if `--description` is not explicitly provided). The changelog title comes from `--title` or the PR title, not from the release note section.
- **No release note found**: No description is filled from the PR body; the title still comes from `--title` or the PR title as usual.

:::{note}
If you explicitly provide `--title` or `--description`, those values take precedence over extracted release notes.
You can turn off the release note extraction in the changelog configuration file or by using the `--no-extract-release-notes` option.
:::

### Control changelog creation [example-block-label]

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

### Create changelogs from a file [example-file-add]

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

### Create changelogs from GitHub release notes [changelog-add-release-version]

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