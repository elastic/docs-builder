# Publish changelogs

You can use release bundles to generate documentation in multiple formats:

- [Asciidoc](#asciidoc) for Elastic 8.x documentation
- [Elastic Docs V3](#docs-v3) for Elastic 9.x and later documentation
- [GFM](#gfm) for GitHub release notes

:::{note}
In the short term, the goal is to create docs that can be included in [existing release note pages](https://www.elastic.co/docs/release-notes).
In the longer term, the goal is to move to more filterable, dynamic pages.
:::

## Before you begin

1. [Create a changelog configuration file](/contribute/configure-changelogs.md) to define all the default behavior and optional profiles and rules.
1. [Create changelogs](/contribute/create-changelogs.md) that describe all the notable changes.
1. [Bundle the changelogs](/contribute/bundle-changelogs.md) for product releases.

## Create asciidoc output [asciidoc]

If you need Asciidoc output (for example, for [Elastic 8.x and earlier docs](https://www.elastic.co/guide/index.html)), you can use the `docs-builder changelog render` command with  `--file-type asciidoc`.
For example:

```sh
docs-builder changelog render \
  --input "./docs/releases/changelog-bundle.yaml" \
  --output ./docs/release-notes \
  --file-type asciidoc \
  --subsections <1>
```

1. You can choose to group the changelogs by their `areas`. Otherwise, they are grouped only by `type`.

The command generates a single output file that includes all types of changelogs.
For up-to-date details, use the `-h` command option or refer to [](/cli/changelog/render.md).

## Create Elastic Docs V3 output [docs-v3]

If you need [Elastic Docs V3](/index.md) output, you have two options:

- [Publish bundles directly](#changelog-directive) with the `{changelog}` directive
- [Create markdown output](#render-changelogs) for each release bundle

The first option is simplest since it requires only a one-time change to your existing release docs.

### Publish bundles directly [changelog-directive]

You can use the [`{changelog}` directive](/syntax/changelog.md) to derive docs from your release bundles.
For example, update your existing release note page to include a directive like this:

```md
:::{changelog} /path/to/bundles
:type: all <1>
:subsections: <2>
:::
```

1. If you have separate release note pages for each type, you can edit this option to show the appropriate subset on each page.
2. You can choose to group the changelogs by their `areas`. Otherwise, they are grouped only by `type`.

There are also options that affect whether to use dropdowns, include descriptions, hide links, and more.
For full documentation and examples, refer to the [{changelog} directive syntax reference](/syntax/changelog.md).

### Create markdown output [render-changelogs]

If you need Markdown output (in particular, the [custom syntax](/syntax/index.md) used in Elastic documentation), you can use the `docs-builder changelog render` command with `--file-type markdown`.
For example:

```sh
docs-builder changelog render \
--input "./kibana/docs/releases/cloud-serverless/2026-05-12.yaml" \
--output ./docs-content/release-notes/elastic-cloud-serverless/_snippets \
--config ./docs-content/changelog.yml \
--file-type markdown
```

The command generates multiple output files:

- `index.md` — features, enhancements, bug fixes, security updates, documentation changes, regressions, and other changes
- `breaking-changes.md` — breaking changes
- `deprecations.md` — deprecations
- `known-issues.md` — known issues
- `highlights.md` — highlighted entries (only created when at least one entry has `highlight: true`)

For up-to-date details, use the `-h` command option or refer to [](/cli/changelog/render.md).

If you are adding this content to existing release note pages, use [file inclusions](/syntax/file_inclusion.md).
For example, each time you create a new set of markdown files, you must include it into the existing docs like this:

```md
:::{include} _snippets/2026-05-12/index.md
:::
```

Since this method requires generating files and adding file inclusions for each release, it's preferable to [publish bundles directly](#changelog-directive).

## Create GFM output [gfm]

If you need GitHub Flavored Markdown (GFM) output, you can use the `docs-builder changelog render` command with  `--file-type gfm`.
For example:

```sh
docs-builder changelog render \
  --input "./docs/bundles/1.10.0.yaml" \
  --output ./docs/github-release \
  --file-type gfm
```

The command generates a single GitHub Flavored Markdown file that includes all types of changelogs.
It has clean section headings and is suitable for copying and pasting into GitHub releases.

For up-to-date details, use the `-h` command option or refer to [](/cli/changelog/render.md).