---
navigation_title: New syntax
---

# New syntax

With the migration to Elastic Docs v3, the primary format for all Elastic Docs is transitioning from AsciiDoc to Markdown. Why Markdown? Markdown is already an industry standard and 90% of Elastic developers are comfortable working with Markdown syntax [[source](https://docs.google.com/presentation/d/1morhFX4tyVB0A2f1_fnySzeJvPYf0kXGjVVYU_lVRys/edit#slide=id.g13b75c8f1f3_0_463)].

See our [syntax guide](../syntax/index.md) to learn more about the flavor of Markdown that we support.

## How does this impact teams with automatically generated Docs?

For teams that generate documentation programmatically, the transition means automatically generated files must now be output in Markdown format instead of AsciiDoc. This adjustment will require updating documentation generation pipelines, but it aligns with the broader benefits of a simpler and more extensible documentation framework.

In addition, we're refining support for including YAML files directly in the docs. See [automated settings](../syntax/automated_settings.md) to learn more.