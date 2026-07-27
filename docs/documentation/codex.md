---
navigation_title: Codex builds
---

# Codex builds

Codex builds create knowledge base environments where multiple repositories publish documentation independently under a shared domain. Unlike assembler builds, there is no centralized navigation composition — each repository maintains its own navigation and publishes on its own schedule.

## How it works

A codex environment is a shared publishing target. Repositories opt into an environment by declaring it in their `docset.yml`:

```yaml
registry: my-environment
```

Once configured, each repository builds and publishes independently. The codex environment aggregates all participating repositories into a unified landing page.

## URL structure

Repositories appear under the environment domain with predictable URL paths:

- **Repositories**: `/r/<repo-name>/` — each repo gets its own URL prefix
- **Groups**: `/g/<group-name>/` — repos can optionally be grouped together

## Grouping repositories

Repos can declare a group in their `docset.yml` to appear grouped on the landing page:

```yaml
codex:
  group: my-group
```

Groups are defined in the environment's `config.yml`, which controls display name, ordering, and other group-level settings.

## Cross-linking

Repositories within a codex environment can link to each other using cross-link syntax:

```markdown
See the [setup guide](repo-name://path/to/file.md) for details.
```

Links are validated against the [link index](../development/link-infrastructure.md), ensuring references between repositories stay correct even though each repo builds independently.

## Key differences from assembler

| | Assembler | Codex |
|---|---|---|
| Navigation | Centralized global nav | Each repo owns its own nav |
| Publishing | All repos built and deployed together | Repos publish independently |
| Setup | Requires assembler configuration files | Each repo just sets `registry` in `docset.yml` |
| Use case | Public documentation sites | Knowledge bases, internal docs |

## Preview workflows

Repositories in a codex environment can set up CI workflows for:

- **PR previews** — deploy a preview build when a pull request is opened
- **Automatic deployment** — publish documentation automatically when changes merge

See [docs-actions](../integrations/docs-actions.md) for reusable GitHub Actions workflows that automate these flows.
