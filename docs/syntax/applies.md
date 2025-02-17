---
applies_to:
  stack: ga 9.1
  deployment:
    eck: ga 9.0
    ess: beta 9.1
    ece: discontinued 9.2.0
    self: unavailable 9.3.0
  serverless:
    security: ga 9.0.0
    elasticsearch: beta 9.1.0
    observability: discontinued 9.2.0
  product: coming 9.5, discontinued 9.7
---

# Applies to


Using yaml frontmatter pages can explicitly indicate to each deployment targets availability and lifecycle status


```yaml
applies_to:
  stack: ga 9.1
  deployment:
    eck: ga 9.0
    ess: beta 9.1
    ece: discontinued 9.2.0
    self: unavailable 9.3.0
  serverless:
    security: ga 9.0.0
    elasticsearch: beta 9.1.0
    observability: discontinued 9.2.0
  product: coming 9.5, discontinued 9.7
```

Its syntax is

```
 <product>: <lifecycle> [version]
```

Where version is optional.

`all` and empty string mean generally available for all active versions

```yaml
applies:
  stack:
  serverless: all
```

`all` and empty string can also be specified at a version level

```yaml
applies:
  stack: beta all
  serverless: beta
```

Are equivalent, note `all` just means we won't be rendering the version portion in the html.


## This section has its own applies annotations [#sections]

:::{applies}
:serverless: unavailable
:::

:::{note}
the `{applies}` directive **MUST** be preceded by a heading.
:::


This section describes a feature that's unavailable in `stack` and `ga` in all cloud products
however its tech preview on `serverless` since it overrides what `cloud` specified.
