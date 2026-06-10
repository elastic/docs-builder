---
navigation_title: Automated Reference
---

# Automated CLI reference docs

`docs-builder` can generate a complete CLI reference section from a JSON schema file that describes your tool's commands, namespaces, flags, and arguments. The generated pages render usage synopses, parameter definitions, and examples directly from that schema — no hand-maintained markdown required.

The schema format is documented at [argh-cli-schema.json](https://github.com/nullean/argh/blob/main/schema/argh-cli-schema.json).

:::{note}
`docs-builder` supports automatic schema generation through the `__schema` meta-command, a built-in feature of the [Nullean.Argh](https://github.com/nullean/argh) CLI framework.
For other frameworks, add a command or script to your build tooling that writes an equivalent JSON file.
:::

:::::{stepper}

::::{step} Create a schema file

Add a mechanism to your CLI that outputs a schema JSON matching the [argh-cli-schema.json](https://github.com/nullean/argh/blob/main/schema/argh-cli-schema.json) format. The schema describes your CLI's structure: top-level commands, namespaces, nested sub-namespaces, per-parameter types and descriptions, usage synopses, and examples.

Once you have a way to generate the schema, write it to a file in your docs repository and commit it:

```bash
# Example — replace with whatever generates your schema
my-tool export-schema > docs/cli-schema.json
```

Commit that file. It is the source of truth for the generated reference section.

:::{tip}
Add a CI step that regenerates the schema and fails if the checked-in copy has drifted:

```yaml
- name: Check CLI schema is up to date
  run: |
    my-tool export-schema > docs/cli-schema.json.tmp
    diff docs/cli-schema.json docs/cli-schema.json.tmp || \
      (echo "cli-schema.json is out of date — regenerate and commit it" && exit 1)
    rm docs/cli-schema.json.tmp
```
:::

::::

::::{step} Add a cli: entry to docset.yml

In your `docset.yml`, add a `cli:` entry to the `toc:` section pointing at the schema file:

```yaml
toc:
  - cli: cli-schema.json
```

That's the minimal setup. `docs-builder` generates a navigation subtree and a page for every namespace and command.

To give the section a stable URL prefix and a home for supplemental docs, also set `folder:` to the directory name you want to use:

```yaml
toc:
  - cli: cli-schema.json
    folder: cli-reference
```

Use `title:` to customize the generated CLI root page title, and `navigation_title:` to customize the sidebar and breadcrumb label without changing generated command examples:

```yaml
toc:
  - cli: cli-schema.json
    folder: cli-reference
    title: Elastic CLI reference
    navigation_title: CLI reference
```

Use `children:` to prepend hand-written pages — installation guides, conceptual overviews, or quick-start tutorials — before the auto-generated reference. All schema-generated pages follow the listed children:

```yaml
toc:
  - cli: cli-schema.json
    folder: cli-reference
    children:
      - file: installation.md
      - file: getting-started.md
```

::::

::::{step} Write supplemental content for namespaces and commands

Drop a supplemental file into the folder for any namespace or command page where you want to add context, usage examples, or richer parameter descriptions. The generated parameter table and usage synopsis are always present — supplemental files let you add to them.

See [Writing supplemental content](./cli-supplemental-docs.md) for the full rules on file naming and heading conventions.

::::

::::{step} Done

Your CLI reference section is live. As your CLI evolves, regenerate the schema and commit — the docs update automatically on the next build.

**Navigation indicators** — generated pages show a `ns` (purple) or `cmd` (amber) badge in the sidebar, making it easy to see at a glance which pages come from the schema and which are hand-written.

**Schema version** — the JSON includes a `schemaVersion` field. Check the [schema spec](https://github.com/nullean/argh/blob/main/schema/argh-cli-schema.json) for the current version when updating your schema generator.

::::

:::::

## Reference

| docset.yml key | Description |
|---|---|
| `cli: <path>` | Path to the schema JSON, relative to `docset.yml` |
| `folder: <path>` | Supplemental docs folder; also sets the URL prefix |
| `title: <title>` | Optional generated CLI root page title |
| `navigation_title: <title>` | Optional generated CLI root navigation label |
| `children:` | Regular toc items prepended before generated pages |
