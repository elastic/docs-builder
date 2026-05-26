## Description

Generate markdown or asciidoc files from changelog bundle files.

To create bundle files, use [](/cli/changelog/bundle.md).
For details and examples, go to [](/contribute/publish-changelogs.md).

The `render` command automatically discovers and merges `.amend-*.yaml` files with their parent bundle. For more information, go to [](bundle-amend.md).

The `changelog render` command does **not** use `rules.publish` for filtering. Filtering must be done at bundle time using `rules.bundle`. For how the directive differs, see the [{changelog} directive syntax reference](/syntax/changelog.md).

## Options

: `--dropdowns`
  Render separated types (breaking changes, deprecations, known issues, highlights) as MyST dropdowns. Defaults to false (flattened bulleted lists). When used, each entry in separated files is rendered as a collapsible dropdown section using MyST syntax (`::::{dropdown}`). When it's not used, entries are rendered as flattened bulleted lists with PR/issue links inline and `Impact` and `Action` sections indented. This flag affects only markdown output; AsciiDoc output always uses its standard format.

: `--no-descriptions`
  Hide changelog entry descriptions from output. Entry titles, PR/issue links, and `Impact` / `Action` sections remain visible. Bundle-level descriptions are unaffected. Applies to all output formats (markdown, asciidoc, gfm). Defaults to false.

: `--title`
  The title to use for section headers, directories, and anchors in output files. Defaults to the version in the first bundle. When omitted, ISO date targets are formatted for display the same way as the `{changelog}` directive (for example, `2026-05-04` becomes "May 4, 2026", `2026-05` becomes "May 2026"), while directory names and heading anchors continue to use the raw target slug. If the string contains spaces, they are replaced with dashes when used in directory names and anchors.

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

# Render as GitHub Flavored Markdown
docs-builder changelog render \
  --input "./docs/changelog/bundles/9.3.0.yaml" \
  --output ./release-notes \
  --file-type gfm

# Render without entry descriptions (titles, links, Impact/Action still shown)
docs-builder changelog render \
  --input "./docs/changelog/bundles/9.3.0.yaml" \
  --output ./release-notes \
  --no-descriptions
```
