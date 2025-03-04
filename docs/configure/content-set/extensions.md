---
navigation_title: Extensions
---

# Content set extensions. 

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

This now allows you to use the special `detection_rules` instruction in the [Table of Contents](navigation.md)

```yaml
toc:
  - file: index.md
  - detection_rules: '../rules'
```

