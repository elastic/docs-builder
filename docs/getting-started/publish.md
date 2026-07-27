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

## Branding

{{dbuild}} is designed to be a general-purpose documentation platform, not tied to any specific organization. By default, it only applies Elastic-specific branding (logo, chrome) when the GitHub organization is `elastic`. For all other organizations, the site renders with neutral styling.

To customize the look of your documentation site, add a `branding:` section to your `docset.yml`:

```yaml
branding:
  icon: images/my-logo.svg
  header-bg: "#1a1a2e"
  og-image: images/social-card.png
  favicon: favicon.ico
  apple-touch-icon: apple-touch-icon.png
```

| Key | Description |
|-----|-------------|
| `icon` | Site icon displayed in the header. Path relative to your docs folder. |
| `header-bg` | CSS colour value for the header background. Defaults to `#000000`. |
| `og-image` | Open Graph image for social sharing. Path relative to your docs folder. |
| `favicon` | Browser favicon. Auto-discovered from `favicon.ico`, `favicon.png`, or `favicon.svg` if not set. |
| `apple-touch-icon` | Apple touch icon. Auto-discovered from `apple-touch-icon.png` if not set. |

When `branding:` is present, all Elastic-specific chrome is suppressed — the site is fully white-labelled.

:::{note}
`branding:` cannot be combined with `features.primary-nav: true`, since the primary nav requires the Elastic global navigation.
:::

## Assembler and codex builds

Getting a repository onboarded to an [assembler build](../documentation/assembler/index.md) or a [codex build](../documentation/codex/index.md) involves additional configuration and coordination with the documentation team. These build modes are covered in their respective sections:

- **[Assembler builds](../documentation/assembler/index.md)** — for public documentation sites with global navigation
- **[Codex builds](../documentation/codex/index.md)** — for knowledge base environments
- **[Add a repository to the docs](../documentation/assembler/add-repo.md)** — step-by-step guide for onboarding to the assembler
