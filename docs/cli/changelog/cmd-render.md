## Description

Generate markdown or asciidoc files from changelog bundle files.

To create bundle files, use [](/cli/changelog/bundle.md).
For details and examples, go to [](/contribute/publish-changelogs.md).

The `render` command automatically discovers and merges `.amend-*.yaml` files with their parent bundle. For more information, go to [](bundle-amend.md).

The `changelog render` command does **not** use `rules.publish` for filtering. Filtering must be done at bundle time using `rules.bundle`. For how the directive differs, see the [{changelog} directive syntax reference](/syntax/changelog.md).

## Options

: `--dropdowns`
  Render separated types (breaking changes, deprecations, known issues, highlights) as MyST dropdowns. Defaults to false (flattened bulleted lists). When used, each entry in separated files is rendered as a collapsible dropdown section using MyST syntax (`::::{dropdown}`). When it's not used, entries are rendered as flattened bulleted lists with PR/issue links inline and `Impact` and `Action` sections indented. This flag affects only markdown output; AsciiDoc output always uses its standard format.

: `--title`
  The title to use for section headers, directories, and anchors in output files. Defaults to the version in the first bundle. When omitted, ISO date targets are formatted for display the same way as the `{changelog}` directive (for example, `2026-05-04` becomes "May 4, 2026", `2026-05` becomes "May 2026"), while directory names and heading anchors continue to use the raw target slug. If the string contains spaces, they are replaced with dashes when used in directory names and anchors.

## Output formats

### Markdown

The default output (`--file-type markdown`) generates multiple files:

- `index.md` — features, enhancements, bug fixes, security updates, documentation changes, regressions, and other changes
- `breaking-changes.md` — breaking changes
- `deprecations.md` — deprecations
- `known-issues.md` — known issues
- `highlights.md` — highlighted entries (only created when at least one entry has `highlight: true`)

### Asciidoc

When `--file-type asciidoc` is specified, the command generates a single asciidoc file with all sections:

- Security updates
- Bug fixes
- Highlights (only included when at least one entry has `highlight: true`)
- New features and enhancements
- Breaking changes
- Deprecations
- Known issues
- Documentation
- Regressions
- Other changes

The asciidoc output uses attribute references for links (for example, `{repo-pull}NUMBER[#NUMBER]`).

AsciiDoc output ignores the `--dropdowns` flag and always uses a standardized format with the following characteristics:

- Multi-block entries (containing description, Impact, and Action sections) use proper list continuation markers (`+`) to maintain list structure
- Strong text formatting uses idiomatic single asterisk syntax (`*Impact:*`, `*Action:*`) following AsciiDoc best practices
- All content blocks are properly attached to their parent list items for correct rendering.

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
