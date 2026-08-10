---
layout: hub
description: docs-builder documentation. Build, validate, and publish Elastic documentation from Markdown across many repositories.
---

:::{hero}
:icon: docs-builder
:title: docs-builder documentation hub
:description: The toolchain that builds Elastic's documentation. Author in Markdown, validate cross-repository links, preview locally, and publish one unified site.
:primary-action: [Install docs-builder](/getting-started/installation.md)
:secondary-action: [Elastic documentation](docs-content://get-started/index.md)
:tertiary-action: [Explore docs-builder](#explore)
:::

::::{card-group}
:title: Get hands-on
:id: hands-on
:intro: New to the toolchain? Follow a guided path from install to published page.

:::{link-card}
title: Write your first page
link: /getting-started/writing-content.md
description: Author a page, add links, and preview it locally.
links:
  - label: Writing content
    url: /getting-started/writing-content.md
  - label: Pages and links
    url: /getting-started/pages-and-links.md
:::

:::{link-card}
title: Serve and publish
link: /getting-started/serve.md
description: Run the local preview server, then publish the built site.
links:
  - label: Serve locally
    url: /getting-started/serve.md
  - label: Publish
    url: /getting-started/publish.md
:::

:::{link-card}
title: Syntax reference
link: /syntax/index.md
description: Every directive and role the toolchain understands.
links:
  - label: Browse the syntax guide
    url: /syntax/index.md
  - label: Hub pages
    url: /syntax/hub-pages.md
:::
::::

::::{card-group}
:title: Documentation this toolchain builds
:id: solutions
:intro: The published Elastic documentation, linked with cross-repository links.
:variant: solutions

:::{link-card}
icon: elasticsearch
variant: es
title: Elasticsearch
description: Search and analytics documentation.
links:
  - label: Search solution
    url: docs-content://solutions/search.md
  - label: Manage data
    url: docs-content://manage-data/index.md
:::

:::{link-card}
icon: observability
variant: obs
title: Observability
description: Logs, metrics, traces, and alerting documentation.
links:
  - label: Observability solution
    url: docs-content://solutions/observability.md
  - label: Explore and analyze
    url: docs-content://explore-analyze/index.md
:::

:::{link-card}
icon: security
variant: sec
title: Security
description: SIEM, endpoint, and detection documentation.
links:
  - label: Security solution
    url: docs-content://solutions/security.md
  - label: Deploy and manage
    url: docs-content://deploy-manage/index.md
:::
::::

:::::{explore}
:id: explore
:title: Explore docs-builder
:intro: Find what you need, organized by task, from authoring and building to publishing and operating.

::::{card-group}
:title: Authoring
:id: authoring

:::{link-card}
title: Syntax
links:
  - label: Directives
    url: /syntax/directives.md
  - label: Hub pages
    url: /syntax/hub-pages.md
  - label: Hero
    url: /syntax/hero.md
aside:
  label: Card directives
  links:
    - label: Card group
      url: /syntax/card-group.md
    - label: Link card
      url: /syntax/link-card.md
    - label: Explore
      url: /syntax/explore.md
    - label: Page card
      url: /syntax/page-card.md
:::

:::{link-card}
title: Getting started
links:
  - label: Installation
    url: /getting-started/installation.md
  - label: Writing content
    url: /getting-started/writing-content.md
  - label: Pages and links
    url: /getting-started/pages-and-links.md
:::

:::{link-card}
title: Formatting
links:
  - label: Code blocks
    url: /syntax/code.md
  - label: Tables
    url: /syntax/tables.md
  - label: Lists
    url: /syntax/lists.md
  - label: Admonitions
    url: /syntax/admonitions.md
:::

:::{link-card}
title: Page metadata
links:
  - label: Frontmatter
    url: /syntax/frontmatter.md
  - label: Links
    url: /syntax/links.md
  - label: Substitutions
    url: /syntax/substitutions.md
:::
::::

::::{card-group}
:title: Builds and configuration
:id: builds

:::{link-card}
title: Build types
links:
  - label: Isolated builds
    url: /documentation/isolated/configure/index.md
  - label: Assembler
    url: /documentation/assembler/configure/index.md
  - label: Codex
    url: /documentation/codex/index.md
:::

:::{link-card}
title: Catalog
links:
  - label: Products
    url: /documentation/catalog/products.md
  - label: Versions
    url: /documentation/catalog/versions.md
aside:
  label: Also see
  links:
    - label: Synonyms
      url: /documentation/catalog/synonyms.md
    - label: Legacy URLs
      url: /documentation/catalog/legacy-url-mappings.md
:::
::::

::::{card-group}
:title: Structured data and operations
:id: data

:::{link-card}
title: Exporters
links:
  - label: Overview
    url: /data/exporters/index.md
  - label: LLM markdown
    url: /data/exporters/llm.md
  - label: Plain text
    url: /data/exporters/plain-text.md
:::

:::{link-card}
title: Operations
links:
  - label: Distributed builds
    url: /documentation/distributed-builds.md
  - label: Infrastructure
    url: /documentation/assembler/infrastructure.md
:::
::::
:::::
