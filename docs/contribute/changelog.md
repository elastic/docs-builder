# Create release notes from changelogs

By adding a file for each notable change in your GitHub repository and grouping the files into bundles, you can ultimately generate release documention with a consistent layout for all your products.

To use the `docs-builder changelog` commands in your development workflow:

1. [Configure changelogs](/contribute/configure-changelogs.md): Create a configuration file, map labels, and define rules for creation and bundling.
1. [Create changelogs](/contribute/create-changelogs.md) with the `docs-builder changelog add` command.
   - Alternatively, if you already have automated release notes for GitHub releases, you can use the `docs-builder changelog gh-release` command to create changelog files and a bundle from your GitHub release notes. Refer to [](/cli/changelog/gh-release.md).
1. [Bundle changelogs](/contribute/bundle-changelogs.md) with the `docs-builder changelog bundle` command. For example, create a bundle for the pull requests that are included in a product release. When changelogs are no longer needed in the repo, [remove changelog files](/contribute/bundle-changelogs.md#changelog-remove) with `docs-builder changelog remove`.
1. [Publish changelogs](/contribute/publish-changelogs.md): Use the `{changelog}` directive in docs or `docs-builder changelog render` to produce release documentation.

For more information about running `docs-builder`, go to [Contribute locally](https://www.elastic.co/docs/contribute-docs/locally).
