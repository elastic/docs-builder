---
navigation_title: "changelog init"
---

# changelog init

Initialize changelog configuration and folder structure for a repository. Creates a `changelog.yml` configuration file (from the built-in template) in the docs folder if it does not exist, and creates the `docs/changelog` and `docs/releases` directories if they do not exist. When non-default paths are specified with `--changelog-dir` or `--bundles-dir`, the corresponding `bundle.directory` and `bundle.output_directory` values in the created `changelog.yml` are updated accordingly.

For details and examples, go to [](/contribute/changelog.md).

## Usage

```sh
docs-builder changelog init [options...] [-h|--help]
```

## Options

`--repository <string?>`
:   Optional: Repository root path.
:   Defaults to the current directory.

`--docs <string?>`
:   Optional: Docs folder path.
:   Defaults to `{repository}/docs`.

`--config <string?>`
:   Optional: Path to the changelog.yml configuration file.
:   Defaults to `{docs}/changelog.yml`.

`--changelog-dir <string?>`
:   Optional: Path to the changelog directory.
:   Defaults to `{docs}/changelog`.

`--bundles-dir <string?>`
:   Optional: Path to the bundles output directory.
:   Defaults to `{docs}/releases`.

## Examples

Initialize changelog in the current directory (creates `docs/changelog.yml`, `docs/changelog`, and `docs/releases`):

```sh
docs-builder changelog init
```

Initialize in a specific repository:

```sh
docs-builder changelog init --repository /path/to/my-repo
```

Override the docs folder location:

```sh
docs-builder changelog init --docs ./documentation
```

Use custom paths for all locations. The `bundle.directory` and `bundle.output_directory` in the created `changelog.yml` are set to the specified values:

```sh
docs-builder changelog init \
  --repository . \
  --config ./my-config/changelog.yml \
  --changelog-dir ./my-changelogs \
  --bundles-dir ./my-releases
```
