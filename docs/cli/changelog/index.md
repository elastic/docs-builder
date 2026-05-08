The `changelog` commands manage a file-per-change workflow that produces release notes with a consistent layout across all your products. Each developer creates a small YAML file per pull request; you later bundle those files into a release artifact and render it into Markdown or AsciiDoc.

## Typical workflow

1. **Configure** — create `docs/changelog.yml` with label mappings and bundle profiles: `docs-builder changelog init`
2. **Create** — add a changelog YAML for each notable PR: `docs-builder changelog add`
3. **Bundle** — aggregate entries for a release: `docs-builder changelog bundle`
4. **Publish** — render the bundle to a release notes page: `docs-builder changelog render`

When working in CI, `docs-builder changelog evaluate-pr` inspects an open pull request and decides whether it needs a changelog file, then sets GitHub Actions outputs so your workflow can gate on the result.

See [Create release notes from changelogs](/contribute/changelog.md) for the end-to-end guide.
