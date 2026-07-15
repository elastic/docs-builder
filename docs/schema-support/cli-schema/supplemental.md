---
navigation_title: Supplemental content
---

# Writing supplemental content

Supplemental files let you enrich any auto-generated CLI reference page with context, usage examples, and richer parameter descriptions without touching the schema. The generated parameter table, usage synopsis, and examples from the schema are always present; supplemental content adds to them.

**Files are discovered automatically.** Drop a file into the supplemental folder following the naming convention and it is picked up on the next build with no configuration needed.

**Validation is strict.** Any supplemental file whose name does not match a known namespace or command produces a build error, so renamed or removed commands can never leave orphaned docs behind silently.

**Frontmatter is preserved as metadata.** Add YAML frontmatter to set page metadata such as `description`, `applies_to`, or `navigation_title`. It is passed through to the generated page and is not rendered as supplemental description text.

## File naming

Two naming styles are supported and can coexist in the same folder.

**Hierarchy style** mirrors the CLI namespace structure:

```text
cli/
  assembler/
    index.md              ← assembler namespace
    cmd-build.md          ← assembler build command
    deploy/
      cmd-apply.md        ← assembler deploy apply command
  cmd-assemble.md         ← root-level assemble command
```

**Flat prefix style** places all files at the folder root with the full path encoded in the name:

```text
cli/
  ns-assembler.md                    ← assembler namespace
  ns-assembler-content-source.md     ← assembler content-source sub-namespace
  cmd-assembler-build.md             ← assembler build command
  cmd-assembler-deploy-apply.md      ← assembler deploy apply command (3-level flat)
```

| File pattern | Matches |
|---|---|
| `index.md` | Root CLI overview page (hierarchy style) |
| `ns-root.md` | Root CLI overview page (flat style) |
| `<ns>/index.md` | Namespace page for `<ns>` |
| `ns-<ns>.md` | Namespace page for `<ns>` (flat) |
| `<ns>/cmd-<cmd>.md` | Command `<cmd>` inside namespace `<ns>` |
| `cmd-<ns>-<cmd>.md` | Same, flat style |
| `cmd-<cmd>.md` | Root-level command `<cmd>` |

**Ignored files:** only `index.md`, `ns-*.md`, and `cmd-*.md` files are validated against the
schema. Any other `.md` file in the supplemental folder (such as shared include fragments) is
silently ignored by the discovery logic.

**`index`-named commands:** if a CLI command is literally named `index`, its supplemental file
must use the `cmd-` prefix to avoid colliding with the namespace `index.md`. For example, a
root-level command named `index` matches `cmd-index.md`, not `index.md`.

## Heading rules

The heading structure of a supplemental file controls what it contributes to the generated page.

### Frontmatter

Use frontmatter for page metadata:

```markdown
---
description: Use the Elastic CLI to call Elasticsearch REST APIs from the command line.
applies_to:
  stack: preview
---

## Description

The `elastic stack es` command group exposes Elasticsearch REST APIs as CLI commands.
```

The metadata remains metadata. The generated page uses the `## Description` section, or the schema description if the file only contains frontmatter.

### No headings

A file with no `##` headings replaces the auto-generated description entirely:

```markdown
Clones all repositories listed in the assembler configuration.
Defaults to `$(pwd)/.artifacts/checkouts/{content_source}`.
```

### Description

A `## Description` section replaces the auto-generated description while allowing other sections in the same file:

```markdown
## Description

:::{note}
This command requires that you've previously run `docs-builder assembler clone`.
:::

Builds all documentation sets into a complete site ready to be deployed.

## Usage examples

...
```

### Additional sections

Any `##` heading other than `Description`, `Options`, or `Arguments` is appended verbatim **after** the generated parameter table. Use this for usage walkthroughs, worked examples, or background context that belongs after the reference material.

## Overriding a flag description

Add a `## Options` section to replace the generated description for specific flags. Each entry starts with `: --flag-name` (or with backtick-wrapped `` : `--flag-name` ``) followed by the replacement text:

```markdown
## Options

: `--environment`
  The environment to target. Must match the environment used when cloning.
  See [environments](../../configure/site/environments.md) for available values.

: `--exporters`
  Comma-separated list of exporters to enable.
  Defaults to `html,llm,config,links,state,redirect`.
```

Only the flags you list are overridden; all other parameters keep their schema descriptions.

Flag names are validated at build time. If a flag does not exist in the schema for that command, the build emits an error.

## Overriding an argument description

Add a `## Arguments` section to replace the generated description for positional arguments. Each entry starts with `: <arg-name>` followed by the replacement text:

```markdown
## Arguments

: `<repository>`
  The name of the `elastic/<repository>` repository to match against configured content sources.

: `<branch>`
  The branch or tag to check. Follows semantic versioning for speculative build detection.
```

Argument names are validated the same way as flag names. An unknown name is a build error.
