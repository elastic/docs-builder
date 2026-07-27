---
navigation_title: Isolated builds
---

# Isolated builds

An isolated build processes a single repository's documentation set. This is the default mode when you run {{dbuild}} locally — no assembly, no global navigation, just your repo's content rendered and served for review.

## When to use isolated builds

- **Local development** — preview your documentation as you write
- **PR previews** — generate a preview build in CI for pull request review
- **Quick iteration** — fast feedback loop without cloning other repositories

## Getting started

1. [Install docs-builder](../getting-started/installation.md)
2. Navigate to a repository that contains a `docset.yml`
3. Run:

```bash
docs-builder serve
```

This starts a local development server with live reload. Changes to your Markdown files are reflected immediately in the browser.

## Cross-link validation

Even in isolated mode, cross-repository links are validated. When your documentation links to another repository using cross-link syntax:

```markdown
See the [getting started guide](docs-content://get-started/introduction.md).
```

{{dbuild}} resolves these links against published link indexes from the link service. This ensures your cross-references are valid without needing to clone other repositories locally.

## Relationship to other build modes

Isolated builds use the same [syntax](./syntax/index.md) and [configuration](./configure/index.md) as assembler and codex builds. Content authored for an isolated preview will render identically when assembled into a full site or published to a codex environment.
