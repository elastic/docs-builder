---
title: Images
---

Images include screenshots, inline images, icons, and more.

Images can be referenced from the top-level `_static` dir or a local image dir.

## Screenshots

```{caution}
This feature is not currently supported in Elastic Docs V3.
```

Screenshots get a box-shadow.

## Block-level images

:::none
\!\[APM](/_static/img/apm.png)
:::

![APM](/_static/img/apm.png)

Or, use the `image` directive.

:::none
\`\`\`\{image} /_static/img/observability.png
:alt: Elasticsearch
:width: 250px
\`\`\`
:::

```{image} /_static/img/observability.png
:alt: Elasticsearch
:width: 250px
```

## Inline image

:::none
Here is the same image used inline \!\[Elasticsearch](/_static/img/observability.png)\{width=30px}
:::

Here is the same image used inline ![Elasticsearch](/_static/img/observability.png){width=30px}.

## Asciidoc syntax

:::none
[role="screenshot"]
image::images/metrics-alert-filters-and-group.png[Metric threshold filter and group fields]
:::

:::none
image::images/synthetics-get-started-projects.png[]
:::