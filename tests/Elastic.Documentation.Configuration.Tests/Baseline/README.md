# Configuration baseline

A frozen snapshot of configuration files as they existed before the domain-model work (documented internally in `elastic/docs-eng-team`, under `docs/domain-model/`). **Never edit these files.** When configuration formats change, the new loaders must still accept every file here and produce the same result as today's loaders, which act as the test oracle. That's how we guarantee no breaking config changes.

If a file here needs to change, the format change is breaking. Add a new fixture next to it instead.

## Sources

| Folder | Source | Revision |
|---|---|---|
| `config/` | `config/*.yml` in `elastic/docs-builder` | `845a6c16e59353a175eeb0c06f18a94e81f7c098` |
| `docset/` | `docs/_docset.yml`, `docs/_redirects.yml` and every `docs/**/toc.yml` in `elastic/docs-builder`, paths relative to `docs/` | `845a6c16e59353a175eeb0c06f18a94e81f7c098` |
| `docset/files.txt` | Every path under `docs/` (`git ls-tree -r --name-only`), relative to `docs/`. Tests create empty stand-ins for these, so TOC resolution sees the same file tree | `845a6c16e59353a175eeb0c06f18a94e81f7c098` |
| `docset/cli-schema.json` | `docs/cli-schema.json`, the schema the docset's `cli:` TOC entry reads | `845a6c16e59353a175eeb0c06f18a94e81f7c098` |
| `changelog/docs-builder.changelog.yml` | `elastic/docs-builder` `docs/changelog.yml` | `845a6c16e59353a175eeb0c06f18a94e81f7c098` |
| `changelog/docs-content.changelog.yml` | `elastic/docs-content` `changelog.yml` | `d112ca4af9314217a7cc80435e54a7e4571a970c` |
| `changelog/elasticsearch.changelog.yml` | `elastic/elasticsearch` `docs/changelog.yml` | `57d29cccb42315e41fa09514d2fa3b377be61ff9` |
| `changelog/kibana.changelog.yml` | `elastic/kibana` `docs/changelog.yml` | `371824e5831c8144cb3757a3cb07160d7cb309c6` |
| `changelog/elastic-otel-java.changelog.yml` | `elastic/elastic-otel-java` `docs/changelog.yml` | `f5d62f333590845dda2adf050391b619c04a7ef6` |

The changelog samples are every `changelog.yml` / `docs/changelog.yml` found on the default branch of the public repositories in `config/assembler.yml`, as of 2026-09-29. Only public repositories are sampled, because this repository is public.

For docs-builder the revision is the snapshot commit. For the other repositories it is the last commit that touched the file.
