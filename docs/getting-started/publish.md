---
navigation_title: Publish
---

# Publish your docs

Once you've written and previewed your documentation locally, there are several ways to publish it depending on your use case.

## Isolated builds: GitHub Pages

For standalone documentation sites, you can publish directly to [GitHub Pages](https://pages.github.com/) using the {{dbuild}} GitHub Action. This is the simplest path — no assembler or codex configuration needed.

### Set up the workflow

Add this workflow to `.github/workflows/gh-pages.yml` in your repository:

```yaml
name: Build the docs

on:
  push:
    branches:
      - main

permissions:
  contents: read

jobs:
  docs:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      pages: write
      id-token: write
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}

    steps:
      - name: Check out the repo
        uses: actions/checkout@v7

      - name: Publish Github
        uses: elastic/docs-builder/actions/publish@main
        id: deployment
        with:
          continue-on-error: "true"
```

This single action builds, validates, and publishes your documentation to GitHub Pages.

### Configure GitHub Pages

In your repository settings, configure GitHub Pages to deploy from GitHub Actions:

1. Go to **Settings** → **Pages**
2. Under **Build and deployment**, set **Source** to **GitHub Actions**

![GitHub Pages settings](images/github-pages-settings.png)

Your documentation will be published to `https://<org>.github.io/<repo>/` on every push to `main`.

## Assembler and codex builds

Getting a repository onboarded to an [assembler build](../documentation/assembler.md) or a [codex build](../documentation/codex.md) involves additional configuration and coordination with the documentation team. These build modes are covered in their respective sections:

- **[Assembler builds](../documentation/assembler.md)** — for public documentation sites with global navigation
- **[Codex builds](../documentation/codex.md)** — for knowledge base environments
- **[Add a repository to the docs](../documentation/how-to/add-repo.md)** — step-by-step guide for onboarding to the assembler
