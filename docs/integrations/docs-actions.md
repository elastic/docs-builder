---
navigation_title: docs-actions
---

# docs-actions

[`elastic/docs-actions`](https://github.com/elastic/docs-actions) is a collection of reusable GitHub Actions workflows for documentation CI/CD. These workflows automate common documentation tasks so individual repositories don't need to maintain their own build pipelines.

## Available workflows

| Workflow | Description |
|----------|-------------|
| `codex-preview.yml` | Deploy a documentation preview when a pull request is opened or updated |
| `codex-preview-cleanup.yml` | Clean up preview deployments when a pull request is closed |

## What it automates

- **PR preview deployments** — automatically build and deploy a preview of documentation changes for every pull request
- **Preview cleanup** — remove preview deployments when PRs are merged or closed
- **Link index updates** — publish updated link indexes after documentation changes merge
- **Build automation** — trigger documentation builds as part of your repository's CI pipeline

## Getting started

Add the workflows to your repository's `.github/workflows/` directory. See the [docs-actions repository](https://github.com/elastic/docs-actions) for usage examples and configuration options.
