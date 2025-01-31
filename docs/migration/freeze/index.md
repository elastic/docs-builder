# Documentation Freeze

:::{important}
During the documentation freeze, maintaining consistency and avoiding conflicts is key. To discourage documentation changes from merging during the documentation freeze, a [GH Action](./gh-action.md) is being added to all repositories with public-facing documentation.
:::

During the documentation freeze, the Docs team will focus almost entirely on migration tasks to ensure all content is successfully migrated and will handle only emergency documentation requests and release-related activities. When the migration is complete, the docs team will assess any documentation requests that were submitted during the documentation freeze and ensure that they're still relevant in the new information architecture and format.

:::{note}
The documentation freeze does not block you from merging asciidoc changes. However, you are strongly discouraged from merging changes to these files as any changes will not be carried forward in the migration and will be lost forever.
:::

## Timeline

* **29-Jan**: Merge all open Docs PRs by 12AM PST
* **30-Jan**: Documentation freeze begins for all public-facing Docs on main/master
* **20-Feb**: Documentation freeze ends
* **25-Mar**: 9.0.0 Docs + Elastic Docs v3 GA

### Details

:::{important}
We are freezing only the main/master public-facing Docs. You can continue to make changes to the 8.x Docs by creating PRs in the 8.x branches.
:::

Before we migrate on 30-Jan, we will close all unmerged Docs PRs targeting main/master. When the freeze ends, you can open a new PR if the changes are still needed.

We will not close PRs targeting main/master that also include code changes. After the freeze begins, merged PRs targeting main/master branches that include AsciiDoc changes will not be migrated. When the freeze ends, all 9.0.0 Docs changes will be made to the migrated Markdown Docs files.

### During the freeze:

* If you make Docs changes to the main/master branches, GitHub Actions will warn against merging the changes.
* You can make 8.x Docs changes by creating PRs in the relevant 8.x branches.
* For 9.0.0 Docs changes, [open an issue](https://github.com/elastic/docs-content/issues/new?template=internal-request.yaml) in [elastic/docs-content](https://github.com/elastic/docs-content/issues) and we’ll incorporate the changes post migration.
* The Docs Team will focus exclusively on migrating content, with the exception of the following:
    * Stack-versioned release notes, including 8.18.0, 9.0.0-beta1, -rc1, and -rc2
    * Serverless changelog
    * Cloud Hosted and ECE release notes

For any questions and emergency Docs requests, post in the [#docs](https://elastic.slack.com/archives/C0JF80CJZ) Slack channel.
