---
navigation_title: URLs
---

# URL structure

Codex URLs follow a fixed structure with no further customization:

| Path | Description |
|------|-------------|
| `/g/<group>/` | Group landing page listing all repositories in that group |
| `/r/<repository>/` | Repository root |
| `/r/<repository>/<page-path>` | Individual pages — follows the [isolated URL structure](../isolated/urls.md) from there |

There is no `path_prefix` or URL rewriting. The page paths within a repository are determined entirely by the file structure relative to `docset.yml`, exactly as in isolated builds.
