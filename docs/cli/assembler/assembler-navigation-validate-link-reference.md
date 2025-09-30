---
navigation_title: "navigation validate-link-reference"
---

# assembler navigation validate-link-reference

Validate all published links in links.json do not collide with navigation path_prefixes and all urls are unique.

Read more about [navigation](../../configure/site/navigation.md).

## Usage

```
docs-builder assembler navigation validate-link-reference [arguments...] [-h|--help] [--version]
```

## Arguments

`[0] <string>`
:   Path to `links.json` defaults to '.artifacts/docs/html/links.json'