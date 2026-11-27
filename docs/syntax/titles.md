---
navigation_title: Title
---

# Page title

### Syntax

Each page is required to at least define a level one heading.

```markdown
# This is my title
```

This title is used both by the documentation navigation

* Left hand site
* Navigational elements such as breadcrumbs and previous/next links.

As well as when using the [auto text links](./links.md#same-page-links-anchors), e.g:

```markdown
[](titles.md)
```

Generated link text: [](titles.md)



```markdown
---
navigation_title: Title as it appears on the left hand site navigation
meta_title: Title as it appears in search results and browser tabs
---

# This is my title
```

Use `meta_title` when the browser and search title needs different wording than the page heading.
The builder appends the site name, so don't include it in `meta_title`.
When the title equals the site name, ignoring case, the builder leaves the site name off. The root page of a site shows `Elastic Docs`, not `Elastic Docs | Elastic Docs`.
