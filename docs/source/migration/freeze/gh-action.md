---
title: GH Action
---

## Overview
This GitHub Action enforces documentation freezes by adding comments to pull requests that modify `.asciidoc` files. It complements the use of `CODEOWNERS` to ensure changes during a freeze period are reviewed and approved by the `@docs-freeze-team`.

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
    runs-on: ubuntu-latest

    steps:
      - name: Checkout the repository
        uses: actions/checkout@v3

      - name: Check for changes in .asciidoc files
        id: check-files
        run: |
          if git diff --name-only ${{ github.event.before }} ${{ github.sha }} | grep -E '\.asciidoc$'; then
            echo "asciidoc_changed=true" >> $GITHUB_ENV
          else
            echo "asciidoc_changed=false" >> $GITHUB_ENV
          fi

      - name: Add a comment if .asciidoc files changed
        if: env.asciidoc_changed == 'true'
        uses: actions/github-script@v6
        with:
          script: |
            github.rest.issues.createComment({
              owner: context.repo.owner,
              repo: context.repo.repo,
              issue_number: context.payload.pull_request.number,
              body: 'It looks like this PR modifies one or more `.asciidoc` files. The documentation is currently under a documentation freeze. Please do not merge this PR. See [link](link) to learn more.'
            });
```