---
navigation_title: Git diff
---

# Git diff exporter

The Git diff exporter maps the current branch diff onto published documentation pages and writes `changed-pages.json` to the build output. CI workflows use this file to post preview links that match the URLs the builder generates.

## How it works

During the HTML build, the exporter collects every published page (source path, navigation URL, title) and the include graph from `{include}` and `{csv-include}` directives. At the end of the build it reads the git diff against a base ref and writes a JSON artifact.

Changed snippet or data files map to the pages that include them. Changed configuration files set `config_changed: true` so workflows can link to the full preview instead of listing every page.

## Output

The exporter writes `changed-pages.json` next to `links.json`:

```json
{
  "base": "origin/main",
  "config_changed": false,
  "pages": [
    {
      "source_path": "guides/start.md",
      "url": "/_preview/org/repo/pull/1/guides/start",
      "title": "Get started",
      "change": "modified",
      "included_from": []
    }
  ],
  "deleted": [{ "source_path": "guides/old.md" }]
}
```

URLs are path-only. Workflows prepend the preview host (for example `https://codex.elastic.dev`).

## Enabling

The exporter is **not** part of the default exporter set. Enable it explicitly:

```bash
docs-builder --exporters default,gitdiff
```

On CI (`GITHUB_ACTIONS` set), isolated builds enable it automatically.

## Diff base resolution

If `ADDED_FILES`, `MODIFIED_FILES`, `DELETED_FILES`, or `RENAMED_FILES` are set (GitHub Actions changed-file lists), the exporter uses those and does not run git.

Otherwise it resolves the git diff base in this order:

1. `DOCS_DIFF_BASE` environment variable
2. `GITHUB_BASE_REF` → `origin/<ref>`
3. `main`, then `master`, then `origin/HEAD`
4. `HEAD^1` when that first parent exists

Then it runs `git diff --name-status -z <base> HEAD`.

## Failure behavior

Git errors do not fail the build. The exporter logs a warning and writes an empty `pages` array.
