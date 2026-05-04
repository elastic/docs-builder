## Description

Generate markdown or asciidoc files from changelog bundle files.

To create bundle files, use [](/cli/changelog/bundle.md).
For details and examples, go to [](/contribute/publish-changelogs.md).

The `render` command automatically discovers and merges `.amend-*.yaml` files with their parent bundle. For more information, go to [](bundle-amend.md).

The `changelog render` command does **not** use `rules.publish` for filtering. Filtering must be done at bundle time using `rules.bundle`. For how the directive differs, see the [{changelog} directive syntax reference](/syntax/changelog.md).

## Output formats

### Markdown

The default output (`--file-type markdown`) generates multiple files:

- `index.md` — features, enhancements, bug fixes, security updates, documentation changes, regressions, and other changes
- `breaking-changes.md` — breaking changes
- `deprecations.md` — deprecations
- `known-issues.md` — known issues
- `highlights.md` — highlighted entries (only created when at least one entry has `highlight: true`)

### Asciidoc

`--file-type asciidoc` generates a single file with all sections in order: security updates, bug fixes, highlights, new features and enhancements, breaking changes, deprecations, known issues, documentation, regressions, and other changes. The asciidoc output uses attribute references for links (for example, `{repo-pull}NUMBER[#NUMBER]`).

### Multiple PR and issue links

Changelog entries can reference multiple pull requests and issues via the `prs` and `issues` array fields. All links are rendered inline:

```md
* Fix ML calendar event update scalability issues. [#136886](https://github.com/elastic/elastic/pull/136886) [#136900](https://github.com/elastic/elastic/pull/136900)
```

## Examples

```sh
# Render a single bundle
docs-builder changelog render \
  --input "./docs/changelog/bundles/9.3.0.yaml" \
  --output ./release-notes

# Render with explicit changelog dir and repo
docs-builder changelog render \
  --input "~/docs/changelog/bundles/9.3.0.yaml|~/docs/changelog|elasticsearch" \
  --output ~/release-notes

# Merge multiple bundles
docs-builder changelog render \
  --input "./bundles/elasticsearch-9.3.0.yaml|./changelog|elasticsearch,./bundles/kibana-9.3.0.yaml|./changelog|kibana" \
  --output ./merged-release-notes

# Hide links from a private repository bundle
docs-builder changelog render \
  --input "./public-bundle.yaml|./changelog|elasticsearch|keep-links,./private-bundle.yaml|./private-changelog|internal-repo|hide-links" \
  --output ./release-notes

# Render with subsections and flattened format (default)
docs-builder changelog render \
  --input "./docs/changelog/bundles/9.3.0.yaml" \
  --output ./release-notes \
  --subsections

### Render with dropdown format
docs-builder changelog render \
  --input "./docs/changelog/bundles/9.3.0.yaml" \
  --output ./release-notes \
  --dropdowns
```
