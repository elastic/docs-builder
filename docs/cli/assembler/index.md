---
navigation_title: "assembler"
---

# Assembler commands

Assembler builds bring together all isolated builds and turn them into the overall documentation that gets published.

If you want to build the latest documentation, you can do so using the following commands

:::{note}
When assembling using the `config init --local` option, it's advised to create an empty directory to run these commands in.
This creates a dedicated workspace for the assembler build and any local changes that you might want to test.
:::

```bash
docs-builder assembler config init --local
docs-builder assemble --serve
```

The full assembled documentation should now be running at http://localhost:4000.

The [assemble](assemble.md) command is syntactic sugar over the following commands:

```bash
docs-builder assembler config init --local
docs-builder assembler clone
docs-builder assembler build
docs-builder assembler serve
```

Which may be more appropriate to call in isolation depending on the workflow you are going for.

All `assembler` commans take an `--environment <environment>` argument that defaults to 'dev' but can be set e.g to 'prod' to
build the production documentation. See [assembler.yml](../../configure/site/index.md) configuration for which environments are
available

## Build commands

- [assemble](assemble.md)
- [assembler build](assembler-build.md)
- [assembler clone](assembler-clone.md)
- [assembler config init](assembler-config-init.md)
- [assembler index](assembler-index.md)
- [assembler serve](assembler-serve.md)

## Specialized build commands

- [assembler bloom-filter create](assembler-bloom-filter-create.md)
- [assembler bloom-filter lookup](assembler-bloom-filter-lookup.md)

## Validation commands

- [assembler content-source match](assembler-content-source-match.md)
- [assembler content-source validate](assembler-content-source-validate.md)
- [assembler navigation validate](assembler-navigation-validate.md)
- [assembler navigation validate-link-reference](assembler-navigation-validate-link-reference.md)

## Deploy commands

- [assembler deploy apply](assembler-deploy-apply.md)
- [assembler deploy plan](assembler-deploy-plan.md)
- [assembler deploy update-redirects](assembler-deploy-update-redirects.md)

