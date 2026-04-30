# list-dependents

Resolves the markdown pages that transitively include the given files.

A page-level dependent is any non-snippet markdown file that — directly or through a
chain of `_snippets/`-to-`_snippets/` includes — pulls in the input file via an
`{{include}}` or `{{csv-include}}` directive.

This is intended for the docs preview workflow: when a PR only edits snippets or
CSV data, the preview comment would otherwise have no specific pages to link to.
Feeding the changed files through `list-dependents` produces the list of pages
that would re-render, so the comment can point at them instead.

## Usage

```
docs-builder list-dependents --files <paths> [options...] [-h|--help] [--version]
```

## Options

`--files <string>`
:   Comma-separated list of file paths to resolve dependents for. Paths can be
    git-relative (for example `docs/_snippets/foo.md`) or absolute.

`-p|--path <string>`
:   Defaults to the `{pwd}/docs` folder (optional)

`--format <string>`
:   Output format: `json` (default) or `text`.

## Output

`json` (default) emits a single object on stdout:

```json
{
  "results": [
    {
      "input": "docs/_snippets/applies-switch.md",
      "resolved": "_snippets/applies-switch.md",
      "found": true,
      "reason": null,
      "dependents": [
        "testing/index.md"
      ]
    },
    {
      "input": "docs/_snippets/missing.md",
      "resolved": "_snippets/missing.md",
      "found": false,
      "reason": "no consumers found",
      "dependents": []
    }
  ]
}
```

`text` emits a human-readable summary, one input per block.

An entry with `found: false` means either the file has no consumers in the
documentation set, or its path resolves outside the documentation source
directory.
