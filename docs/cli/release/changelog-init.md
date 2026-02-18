---
navigation_title: "changelog init"
---

# changelog init

Initialize changelog configuration and folder structure for a repository.
Discovers the `docs` folder by locating `docset.yml` using the same heuristics as other `docs-builder` commands (checks root and `docs/` first, then searches recursively).
Fails with an error if no `docs` folder is found. 
Creates a `changelog.yml` configuration file (from the built-in template) in the same directory as `docset.yml` if it does not exist, and creates the `changelog` and `releases` subdirectories there. 
When non-default paths are specified with `--changelog-dir` or `--bundles-dir`, the corresponding `bundle.directory` and `bundle.output_directory` values in the created `changelog.yml` are updated accordingly.

For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog init [options...] [-h|--help]
```

## Options

`--path <string?>`
:   Optional: Root path to search for `docset.yml`.
:   Defaults to the output of `pwd` (current directory).

`--changelog-dir <string?>`
:   Optional: Path to the changelog directory.
:   Defaults to `{docsFolder}/changelog`, where the docs folder is the directory containing `docset.yml`.

`--bundles-dir <string?>`
:   Optional: Path to the bundles output directory.
:   Defaults to `{docsFolder}/releases`.

## Examples

Initialize changelog (creates `changelog.yml` next to `docset.yml`, plus `changelog` and `releases` subdirectories):

```sh
docs-builder changelog init
```

Initialize when run from a subdirectory, specifying the root path to search:

```sh
docs-builder changelog init --path /path/to/my-repo
```

Use custom changelog and bundles directories. The `bundle.directory` and `bundle.output_directory` in the created `changelog.yml` are set to the specified values:

```sh
docs-builder changelog init \
  --changelog-dir ./my-changelogs \
  --bundles-dir ./my-releases
```
