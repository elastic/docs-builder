---
navigation_title: Extensions
---

# Content set extensions.

For the full list of `docset.yml` keys, see [`docset.yml` reference](docset-reference.md).

The documentation engineering team will on occasion built extensions for specific use-cases.

These extension needs to be explicitly opted into since they typically only apply to a few content sets.


## Detection Rules Extensions

For the TRADE team the team built in support to picking up detection rule files from the source and emitting 
documentation and navigation for them.

To enable:

```yaml
extensions:
  - detection-rules
```

This now allows you to use the special `detection_rules` instruction in the [TOC reference](toc-reference.md#detection_rules)
As a means to pick up `toml` files as `children`

```yaml
toc:
  - file: index.md
    detection_rules: '../rules'
```

