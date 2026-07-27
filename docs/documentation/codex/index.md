---
navigation_title: Codex
---

# Codex builds

Codex builds compose many [isolated builds](../isolated/index.md) into a knowledge base environment where repositories publish independently under a shared domain. Unlike assembler builds, there is no centralized navigation composition — each repository maintains its own navigation and publishes on its own schedule.

## How it works

A codex environment is a shared publishing target. Repositories opt in by setting `registry:` in their `docset.yml`. Each repository builds in isolation and publishes independently — the codex environment aggregates all participating repositories into a unified landing page.

## URL structure

- **Repositories**: `/r/<repo-name>/` — each repo gets its own URL prefix
- **Groups**: `/g/<group-name>/` — repos can optionally be grouped together

## Cross-linking

Repositories within a codex environment can link to each other using cross-link syntax:

```markdown
See the [setup guide](repo-name://path/to/file.md).
```

Links are validated against the [link index](../../development/link-infrastructure.md), ensuring references stay correct even though each repo builds independently.

## Key differences from assembler

| | Assembler | Codex |
|---|---|---|
| Navigation | Centralized global nav | Each repo owns its own nav |
| Publishing | All repos built and deployed together | Repos publish independently |
| Setup | Requires assembler configuration files | Each repo just sets `registry` in `docset.yml` |
| Use case | Public documentation sites | Knowledge bases, internal docs |
