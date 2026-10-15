## Description

Initialize changelog configuration and folder structure for a repository.

The command locates the docs folder using the following priority:

1. A `docs` folder containing `docset.yml` in the repository root or `docs/` directory.
2. A `docs` folder without `docset.yml`.
3. If no docs folder exists, creates `{path}/docs`.

The command creates a `changelog.yml` configuration file and `changelog` and `releases` subdirectories in the docs folder. The generated `changelog.yml` contains the minimal configuration for the automated release notes path: type mappings for `feature`, `bug-fix`, and `breaking-change`, and a skip-label rule.

When `changelog.yml` already exists, the command updates only the directory paths if `--changelog-dir` or `--bundles-dir` are specified, and leaves all other content unchanged.

## Examples

```sh
# Standard initialization
docs-builder changelog init

# From a subdirectory, specifying the repo root
docs-builder changelog init --path /path/to/my-repo

# Custom changelog and bundles directories
docs-builder changelog init \
  --changelog-dir ./my-changelogs \
  --bundles-dir ./my-releases
```
