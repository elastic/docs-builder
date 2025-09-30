---
navigation_title: "bloom-filter create"
---

# assembler bloom-filter create

Generates a bloom filter that gets embedded into the `docs-builder` binary.

This bloom filter is used to determine whether a document's `mapped_page` in the frontmatter exists in 

the project of [legacy-url-mappings](../../configure/site/legacy-url-mappings.md) 

The existence determines how the document history selector should be populated.

## Usage

```
docs-builder assembler bloom-filter create [options...] [-h|--help] [--version]
```

## Options

`--built-docs-dir` `<string>`
:   The local dir of local elastic/built-docs repository (Required)