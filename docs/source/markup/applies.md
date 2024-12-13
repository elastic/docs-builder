---
title: Product Availability
applies:
  stack: ga 8.1
  serverless: tech-preview
  hosted: beta 8.1.1
  eck: beta 3.0.2
  ece: unavailable
---


Using yaml frontmatter pages can explicitly indicate to each deployment targets availability and lifecycle status


```yaml
applies:
  stack: ga 8.1
  serverless: tech-preview
  hosted: beta 8.1.1
  eck: beta 3.0.2
  ece: unavailable
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