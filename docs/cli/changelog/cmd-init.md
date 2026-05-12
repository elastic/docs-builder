## Description

Initialize changelog configuration and folder structure for a repository.

The command locates the docs folder using the following priority:

1. A `docs` folder containing `docset.yml` in the repository root or `docs/` directory.
2. A `docs` folder without `docset.yml`.
3. If no docs folder exists, creates `{path}/docs`.

The command creates a `changelog.yml` configuration file and `changelog` and `releases` subdirectories in the docs folder.

When the template is written for the first time, the command seeds `bundle.owner`, `bundle.repo`, and `bundle.link_allow_repos` from your `git` remote `origin` (when it points at `github.com`) and/or from `--owner` and `--repo`. CLI values override values inferred from `git`. If neither source provides enough information, the placeholder lines are removed from the template for manual editing.

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

# Explicitly set GitHub owner and repo (useful in CI without git)
docs-builder changelog init --owner elastic --repo kibana
```
