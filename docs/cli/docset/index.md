---
navigation_title: "documentation set"
---

# Documentation Set Commands

An isolated build means building a single documentation set.

A `Documentation Set` is defined as a folder containing a [docset.yml](../../configure/content-set/index.md) file.

These commands are typically what you interface with when you are working on the documentation of a single repository locally.

## Isolated build commands

`build` is the default command so you can just run `docs-builder` to build a single documentation set. `docs-builder` will
locate the `docset.yml` anywhere in the directory tree automatically and build the documentation.

- [build](build.md) - build a single documentation set (incrementally)
- [serve](serve.md) - partial build and serve documentation as needed at http://localhost:3000
- [index](index-command.md) - ingest a single documentation set to an Elasticsearch index.

## Refactor commands

- [mv](mv.md) - move a file or folder to a new location. This will rewrite all links in all files too.
- [diff validate](diff-validate.md) - validate that local changes are reflected in [redirects.yml](../../contribute/redirects.md)

