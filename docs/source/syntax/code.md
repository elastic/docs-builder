---
title: Code blocks
---

Code blocks can be used to display multiple lines of code. They preserve formatting and provide syntax highlighting when possible.

### Syntax

:::none
\`\`\`yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
\`\`\`
:::

```yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
```

### Asciidoc syntax

:::none
[source,sh]
--------------------------------------------------
GET _tasks
GET _tasks?nodes=nodeId1,nodeId2
GET _tasks?nodes=nodeId1,nodeId2&actions=cluster:*
--------------------------------------------------
:::

### Code blocks with callouts

_coming soon!_