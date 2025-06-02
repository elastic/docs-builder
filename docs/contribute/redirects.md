---
navigation_title: "Redirects"
---
# Adding redirects to link resolving

If you [move files around](move.md) or simply need to delete a few pages you might end up in a chicken-and-egg situation. The files you move or delete might still be linked to by other [documentation sets](../configure/content-set/index.md).

Redirects allow you to force other documentation sets to resolve old links to their new location.
This allows you to publish your changes and work to update the other documentation sets.

:::{note}
The source and target URLs must both be within Elastic Docs V3.
You cannot use this process to redirect to [API docs](https://www.elastic.co/docs/api/), for example.
:::

## File location.

The file should be located next to your `docset.yml` file

* `redirects.yml` if you use `docset.yml`
* `_redirects.yml` if you use `_docset.yml`

## Syntax

A full overview of the syntax, don't worry we'll zoom after!

```yaml
redirects:
  'testing/redirects/4th-page.md': 'testing/redirects/5th-page.md'
  'testing/redirects/9th-page.md': '!testing/redirects/5th-page.md'
  'testing/redirects/6th-page.md':
  'testing/redirects/7th-page.md':
    to: 'testing/redirects/5th-page.md'
    anchors: '!'
  'testing/redirects/first-page-old.md':
    to: 'testing/redirects/second-page.md'
    anchors:
      'old-anchor': 'active-anchor'
      'removed-anchor':
  'testing/redirects/second-page-old.md':
    many:
      - to: "testing/redirects/second-page.md"
        anchors:
          "aa": "zz"
          "removed-anchor":
      - to: "testing/redirects/third-page.md"
        anchors:
          "bb": "yy"
  'testing/redirects/third-page.md':
    anchors:
      'removed-anchor':
```

### Redirect preserving all anchors

This will rewrite `4th-page.md#anchor` to `5th-page#anchor` while still validating the 
anchor is valid like normal.

```yaml
redirects:
  'testing/redirects/4th-page.md': 'testing/redirects/5th-page.md'
```
### Redirect stripping all anchors

Here both `9th-page.md` and `7th-page.md` redirect to `5th-page.md` but the crosslink resolver
will strip any anchors on `9th-page.md` and `7th-page.md`.

:::{note}
The following two are equivalent. The `!` prefix provides a convenient shortcut
:::

```yaml
redirects:
  'testing/redirects/9th-page.md': '!testing/redirects/5th-page.md'
  'testing/redirects/7th-page.md':
    to: 'testing/redirects/5th-page.md'
    anchors: '!'
```

A special case is redirecting to the page itself when a section gets removed/renamed.
In which case `to:` can simply be omitted

```yaml
  'testing/redirects/third-page.md':
    anchors:
      'removed-anchor':
```

### Redirect renaming anchors

* `first-page-old.md#old-anchor` will redirect to `second-page.md#active-anchor`
* `first-page-old.md#removed-anchor` will redirect to `second-page.md`

Any anchor not listed will be forwarded and validated e.g;

* `first-page-old.md#another-anchor` will redirect to `second-page.md#another-anchor` and validate 
  `another-anchor` exists in `second-page.md`

```yaml
redirects:
  'testing/redirects/first-page-old.md':
    to: 'testing/redirects/second-page.md'
    anchors:
      'old-anchor': 'active-anchor'
      'removed-anchor':
```

## Glob pattern redirects

The redirect system also supports glob patterns for redirecting multiple pages at once. This is useful when moving entire sections or directories.

### Trailing wildcard redirects

Use the `**` pattern at the end of a path to match all content under a specific path prefix and redirect it to a new location while preserving the path structure.

```yaml
redirects:
  'reference/apm/agents/android/**': 'reference/opentelemetry/edot-sdks/android/**'
  'old-section/**': 'new-section/**'
```

This redirects:
- `reference/apm/agents/android/setup` → `reference/opentelemetry/edot-sdks/android/setup`
- `reference/apm/agents/android/api/classes` → `reference/opentelemetry/edot-sdks/android/api/classes`

### Simple glob pattern redirects

You can use basic glob patterns with `*` (single segment wildcard) and `?` (single character wildcard).

```yaml
redirects:
  'guides/*/intro': 'tutorials/*/getting-started'
  'api/v1/user?.json': 'api/v2/users'
```

For simple cases where the target doesn't contain wildcards, the target is used directly:

```yaml
redirects:
  'docs/v*/setup': 'documentation/setup-guide'
```

### Best practices for glob patterns

1. Use exact path redirects when possible.
2. Use trailing `**` wildcards for redirecting entire directory trees.
3. Keep glob patterns simple, with wildcards in predictable positions.
4. Test your redirects thoroughly, especially when using complex patterns.
