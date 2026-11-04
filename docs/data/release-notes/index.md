---
navigation_title: Release notes
---

# Release notes

{{dbuild}} provides a changelog-based system for creating structured release documentation. Teams author individual changelog entries as they work, and {{dbuild}} bundles them into polished release notes pages.

## Workflow

The release notes workflow has four steps:

### 1. Configure

Set up changelog configuration in your repository's `docset.yml`. This defines changelog categories (breaking changes, deprecations, enhancements, bug fixes), version patterns, and output structure.

See [configure changelogs](./configure.md) for setup details.

### 2. Create changelogs

Authors create individual changelog entry files as part of their normal development workflow. Each entry is a small YAML file describing the change, its category, and any relevant metadata.

See [create changelogs](./create.md) for the entry format.

### 3. Bundle

At release time, changelog entries are bundled into versioned release notes. The bundling process aggregates entries by category and version, producing structured Markdown output.

See [bundle changelogs](./bundle.md) for the bundling process.

### 4. Publish

Bundled release notes are published as part of the documentation site. The publish step can be automated through CI workflows.

See [publish changelogs](./publish.md) for deployment details.

## Release Notes Explorer

:::{page-card} [Release Notes Explorer](./explorer.md)
A web UI for browsing release notes across products — coming soon.
:::
