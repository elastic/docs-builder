# GH Action

## Overview

The documentation team will use a GitHub Action to enforce the content freeze by adding comments to pull requests that modify `.asciidoc` files. It complements the use of `CODEOWNERS` to ensure changes during a freeze period are reviewed and approved by the `@docs-freeze-team`.

## How It Works
1. **Trigger**: The Action is triggered on pull request events (`opened`, `reopened`, or `synchronize`).
2. **Check Changes**: It checks the diff between the latest commits to detect modifications to `.asciidoc` files.
3. **Add Comment**: If changes are detected, the Action posts a comment in the pull request, reminding the contributor of the freeze.

```yaml
name: Comment on PR for .asciidoc changes

on:
  pull_request:
    types:
      - synchronize
      - opened
      - reopened
    branches:
      - main
      - master
      - "9.0"

jobs:
  comment-on-asciidoc-change:
    permissions:
      contents: read
      pull-requests: write
    uses: elastic/docs-builder/.github/workflows/comment-on-asciidoc-changes.yml@main
```
