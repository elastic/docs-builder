---
navigation_title: Title
---

# Page title

## Syntax

Each page must define a level-one heading.

```markdown
# This is my title
```

The heading supplies the visible page title, link text, and the default HTML page title.
For public Elastic Docs builds, the builder appends `| Elastic Docs` to the HTML title.

When frontmatter identifies exactly one product and the heading does not already contain
that product's display name, the builder adds the product name automatically:

```markdown
---
products:
  - id: elasticsearch
---

# Query DSL
```

The resulting HTML title is `Query DSL - Elasticsearch | Elastic Docs`, while the
visible heading remains `Query DSL`. Pages associated with multiple products keep the
heading as their default HTML title because the builder cannot choose one product keyword.

API operation pages use the related form `{H1} - {Product} API | Elastic Docs`.

The heading is also used by:

* The left navigation.
* Navigational elements, such as breadcrumbs and previous and next links.
* [Automatic link text](./links.md#same-page-links-anchors).

```markdown
[](titles.md)
```

Generated link text: [](titles.md).

```markdown
---
navigation_title: Title as it appears on the left hand site navigation
---

# This is my title
```