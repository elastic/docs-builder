# Configure changelogs

Before you can use the `docs-builder changelog` commands in your development workflow, you must make some decisions and do some setup steps:

1. Ensure that your products exist in [products.yml](https://github.com/elastic/docs-builder/blob/main/config/products.yml). Products that only need release notes (not public documentation) can be added with `features: { public-reference: false }`. For more information, refer to [Products](/configure/site/products.md).
1. Add labels to your GitHub pull requests that map to [changelog types](https://github.com/elastic/docs-builder/blob/main/src/Elastic.Documentation/ChangelogEntryType.cs). At a minimum, create labels for the `feature`, `bug-fix`, and `breaking-change` types.
1. Optional: Choose areas or components that your changes affect and add labels to your GitHub pull requests (such as `:Analytics/Aggregations`).
1. Optional: Add labels to your GitHub pull requests to indicate that they are not notable and should not generate changelogs. For example, `non-issue` or `release_notes:skip`. Alternatively, you can assume that all PRs are *not* notable unless a specific label is present (for example, `@Public`).

After you collect all this information, you can use it to make the changelog process more automated and repeatable by setting up a changelog configuration file.

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
