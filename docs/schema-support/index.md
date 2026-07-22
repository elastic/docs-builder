---
navigation_title: Schema support
---

# Reference documentation from schemas and specs

`docs-builder` can generate complete reference sections from machine-readable sources and let
you layer local Markdown on top. Two generators are available:

- **CLI reference** — generates one page per command and namespace from a JSON schema file.
- **API Explorer** — generates one page per API operation, tag, and shared schema type from an
  OpenAPI JSON specification.

Both follow the same pattern: generate the reference from the source file, then let authors add
context, examples, and richer descriptions using local Markdown — without editing the generated
content by hand. The mechanisms differ in granularity.

## Comparison

| | CLI reference | API Explorer |
|---|---|---|
| **Source** | JSON schema (`argh-cli-schema.json` format) | OpenAPI JSON spec |
| **Config key** | `cli:` inside `toc:` (any `docset.yml` or `toc.yml`) | `api:` in `docset.yml` only |
| **Generated output** | One page per command and namespace | Operation, tag, and schema type pages |
| **Markdown extension granularity** | Fine-grained: per-command/namespace supplemental files | Page-level only: standalone intro/outro pages |
| **Discovery** | Automatic — files are matched by naming convention | Explicit — files are listed in the `api:` sequence |
| **Validation** | Strict schema validation — unknown file names and unknown flag/argument names are build errors | Slug/collision validation — reserved slugs (`types`, `tags`) and operation-moniker conflicts are build errors |
| **One source per entry** | One schema per `cli:` node | Exactly one spec per product key |

## Markdown extension granularity

The key difference between the two generators is how much you can override using local Markdown.

**CLI reference** supports fine-grained extension. For any namespace or command, you can:

- Replace the auto-generated **description** entirely, or add a `## Description` section.
- Append **"after text"** (any `## Heading` other than `Description`, `Options`, or `Arguments`)
  after the generated parameter table.
- Override individual **flag descriptions** via a `## Options` definition list.
- Override individual **argument descriptions** via a `## Arguments` definition list.
- All overrides are validated against the schema at build time — referencing an unknown flag or
  argument name is a build error.

**API Explorer** supports page-level extension only. You can add standalone Markdown pages
**before** (intro) or **after** (outro) the generated API content for a product. You cannot
currently override or augment an individual operation description, tag description, schema
description, or parameter description using a local Markdown file — those come verbatim from the
OpenAPI spec. Per-operation augmentation is planned as a future enhancement.

## Learn more

- [CLI reference — schema setup and docset.yml configuration](./cli-schema/index.md)
- [CLI reference — writing supplemental content, naming conventions, and validation](./cli-schema/supplemental.md)
- [API Explorer — configuration, intro/outro pages, naming, validation, and OpenAPI extensions](./api-explorer/index.md)
