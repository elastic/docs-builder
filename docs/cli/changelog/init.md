---
navigation_title: "changelog init"
---

# changelog init

Initialize changelog configuration and folder structure for a repository.

If a docs folder that contains `docset.yml` exists (in the repository root or `docs/` directory), the command uses that folder.
If a `docs` folder exists without `docset.yml`, the command uses it.
If no docs folder exists, the command creates `{path}/docs` and places `changelog.yml` there.

The command creates a `changelog.yml` configuration file (from the built-in template) and `changelog` and `releases` subdirectories in the `docs` folder.
When `--changelog-dir` or `--bundles-dir` is specified, the corresponding `bundle.directory` and `bundle.output_directory` values in `changelog.yml` are set or updated (whether creating a new file or the file already exists).

## Usage

```sh
docs-builder changelog init [options...] [-h|--help]
```

## Options

`--path <string?>`
:   Optional: Repository root path.
:   Defaults to the output of `pwd` (current directory). The docs folder is `{path}/docs`, created if it does not exist.

`--changelog-dir <string?>`
:   Optional: Path to the changelog directory.
:   Defaults to `{docsFolder}/changelog`.

`--bundles-dir <string?>`
:   Optional: Path to the bundles output directory.
:   Defaults to `{docsFolder}/releases`.

## Examples

Initialize changelog (creates or uses docs folder, places `changelog.yml` there, plus `changelog` and `releases` subdirectories):

```sh
docs-builder changelog init
```

Initialize when run from a subdirectory, specifying the root path:

```sh
docs-builder changelog init --path /path/to/my-repo
```

Use custom changelog and bundles directories.
Sets or updates `bundle.directory` and `bundle.output_directory` in `changelog.yml` (creating the file if it does not exist):

```sh
docs-builder changelog init \
  --changelog-dir ./my-changelogs \
  --bundles-dir ./my-releases
```
