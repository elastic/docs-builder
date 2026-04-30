# Configure changelogs

Before you can use the `docs-builder changelog` commands, you must evaluate your GitHub label strategy and verify that the necessary product metadata exists.
You can then set up a changelog configuration file to make the content workflow more automated and repeatable.

## Before you begin

1. Ensure that your products exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Products that only need release notes (not public documentation) can be added with `features: { public-reference: false }`. For more information, refer to [Products](/configure/site/products.md).

1. Optional: Choose the GitHub labels that you'll use to automatically derive some changelog fields.

    1. Create labels for _types_. The supported types are defined in [ChangelogEntryType.cs](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs). At a minimum, add labels for the `feature`, `bug-fix`, and `breaking-change` types.

    1. Create labels for _products_. If your repo only pertains to a single product or you don't want to use labels to accomplish this classification, this step is not required.

    1. Create labels for _areas_ (or "features" or "components") of your products. If you don't want to show categories (other than the default "type" categories) in your documentation, this step is not required.

    1. Create labels to opt in or out of changelogs. For example, create `non-issue` or `release_notes:skip` labels for PRs that _shouldn't_ have changelogs. Alternatively, create a `@Public` label to identify PRs that _should_ have changelogs. You can only choose one label strategy for this behavior: exclusion or inclusion.

    1. Create labels for _highlights_. This step is not required unless you want to publish release highlights.

1. If you'll be accessing GitHub from the command line, set up the necessary privileges as described in the following section.

### Authorization

Any of the `docs-builder changelog` commands that access GitHub require authority to view your pull requests and issues.
For example, this authority is required if you'll be running any of these [changelog commands](/cli/changelog/index.md) from the command line: `changelog add`, `changelog bundle`, `changelog gh-release`.

You must log into GitHub or set the `GITHUB_TOKEN` (or `GH_TOKEN` ) environment variable with a sufficient personal access token (PAT) to pull information from your repository.
Otherwise, there will be fetch failures when you access private repositories and you might also encounter GitHub rate limiting errors.

For example, to create a new token with the minimum authority to read pull request details:

   1. Go to **GitHub Settings** > **Developer settings** > **Personal access tokens** > [Fine-grained tokens](https://github.com/settings/personal-access-tokens).
   1. Click **Generate new token**.
   1. Give your token a descriptive name (such as "docs-builder changelog").
   1. Under **Resource owner** if you're an Elastic employee, select **Elastic**.
   1. Set an expiration date.
   1. Under **Repository access**, select **Only select repositories** and choose the repositories you want to access.
   1. Under **Permissions** > **Repository permissions**, set **Pull requests** to **Read-only**. If you want to be able to read issue details, do the same for **Issues**.
   1. Click **Generate token**.
   1. Copy the token to a safe location and use it in the `GITHUB_TOKEN` environment variable.

## Create a changelog configuration file [changelog-settings]

The changelog configuration file:

- Defines acceptable product, type, subtype, and lifecycle values.
- Sets default options, such as whether to extract issues and release note text from pull requests.
- Defines profiles for simplified bundle creation.
- Prevents the creation of changelogs when certain labels are present.
- Excludes changelogs from bundles based on their areas, types, or products.

:::{tip}
Only one configuration file is required for each repository.
You must maintain the file if your repo labels change over time.
:::

You can use the [changelog init](/cli/changelog/init.md) command to create the changelog configuration file and folder structure automatically.

For example, run the following command in your GitHub repo's root directory:

```sh
docs-builder changelog init
```

By default, it creates `docs/changelog.yml` file that contains settings like this:

```yml
filename: timestamp
products:
  available: []
extract:
  release_notes: true
  issues: true
  strip_title_prefix: false
lifecycles:
  - preview
  - beta
  - ga
pivot:
  types:
    breaking-change:
      labels: ">breaking, >bc"
    bug-fix: ">bug"
    feature:
```

For the most up-to-date changelog configuration options, refer to [changelog.example.yml](https://github.com/elastic/docs-builder/blob/main/config/changelog.example.yml).

For descriptions of all the settings, refer to [Changelog configuration reference](/contribute/configure-changelogs-ref.md)

## Rules for creation and bundling [rules]

If you have pull request labels that indicate a changelog is not required (such as `>non-issue` or `release_note:skip`), you can declare these in the `rules.create` section of the changelog configuration.

When you run the `docs-builder changelog add` command with the `--prs` or `--issues` options and the pull request or issue has one of the identified labels, the command does not create a changelog.

Likewise, if you want to exclude changelogs with certain products, areas, or types from the release bundles, you can declare these in the `rules.bundle` section of the changelog configuration.
For example, you might choose to omit `other` or `docs` changelogs.
Or you might want to omit all autoscaling-related changelogs from the Cloud Serverless release bundles.

You can define rules at the global level (applies to all products) or for specific products.
Product-specific rules override the global rules entirely—they do not merge.
For details, refer to [Rules](/contribute/configure-changelogs-ref.md#rules).

## Use a changelog configuration file

After you've created a config file, all subsequent changelog commands can use it.
By default, they look for `docs/changelog.yml` but you can specify a different path with the `--config` command option.

For specific details about the usage and impact of the configuration file, refer to the [changelog commands](/cli/changelog/index.md).

The [changelog directive](/syntax/changelog.md) also uses the changelog configuration file and you can specify a non-default path if necessary.
