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

When the page resolves to exactly one product and the heading does not already contain
that product's display name, the builder adds the product name automatically. Product
resolution merges page frontmatter with docset, repository, `applies_to`, and `mapped_pages`
metadata:

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
meta_title: Title as it appears in search results and browser tabs
---

# This is my title
```

Use `meta_title` only as a last-resort override when the automatic title needs different wording.
The public site appends `| Elastic Docs` to the page title.
Other build types keep their existing site suffix.
Do not include the suffix in `meta_title`.
