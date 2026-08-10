# Implementation Plan: #718 api: schema (stacked over #711 drift)

## Overview

Introduce the strict RFC `api:` schema with required `product:`, optional `repository:` and
`children:`, and migrate the docs-builder docset. Remote version-index resolution is deferred to
#719 on branch `issue-719-version-index-client`.

## Architecture Decisions

- Breaking migration of the only consumer (`docs/_docset.yml`); legacy string/object/intro-outro
  shapes throw with migration guidance.
- `repository: org/repo` is part of the schema because the shipped global `index.json` keys by
  publishing repo, not `products.yml` id.
- `spec:` basename is the index lookup key; it is not a CloudFront URL.
- #718 skips API generation with a warning when no local spec exists; #719 adds remote fetch.

## Task List

### Phase 1: #718 config schema (this PR)

- [x] Rebase WIP; preserve full stack on `issue-719-version-index-client`
- [x] Strict `ApiProductEntry` + converter
- [x] `ConfigurationFile` resolve/validate
- [x] Children nav + generator consumers
- [x] Migrate `_docset.yml`, author docs, tests

### Phase 2: #719 version index client (stacked PR)

- [ ] Rewrite client against live root `index.json` (`{ "version": "…" }`, monikers `main`/`9`/`8`)
- [ ] Wire `OpenApiGenerator` remote resolution
- [ ] Restore remote smoke `api:` keys in docset (optional)

## Risks

| Risk | Mitigation |
|---|---|
| WIP client used wrong index shape | Kept off #718; rewrite on #719 branch |
| Missing local spec | Warn + skip until #719 |
