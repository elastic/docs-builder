---
navigation_title: "Redirects"
---

# Manage redirects across doc sets

When you [move](move.md) or delete pages, other [documentation sets](../configure/content-set/index.md) might still link to them. This can lead to a chicken-and-egg problem: you can't publish your changes without breaking links elsewhere.

Redirects let you map old links to new targets across documentation sets, so you can publish changes while updating other doc sets.

## Limitations

Redirects only work within Elastic Docs V3 content sets. You cannot use this method to redirect to external destinations like [API docs](https://www.elastic.co/docs/api/).

For API redirects, consult with the documentation engineering team on Slack (#elastic-docs-v3).

For elastic.co/guide redirects, open a [web team request](http://ela.st/web-request).

## Validation

Running `docs-builder diff validate` will give you feedback on whether all necessary redirect rules are in place after your changes. It will also run on pull requests.

## File location

Redirects are configured at the content set-level.
The configuration file should be located next to your `docset.yml` file:

* `redirects.yml` if you use `docset.yml`
* `_redirects.yml` if you use `_docset.yml`

## Syntax

Example syntax:

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
  'testing/redirects/cross-repo-page.md': 'other-repo://reference/section/new-cross-repo-page.md'
  'testing/redirects/8th-page.md':
    to: 'other-repo://reference/section/new-cross-repo-page.md'
    anchors: '!'
    many:
      - to: 'testing/redirects/second-page.md'
        anchors:
          'item-a': 'yy'
      - to: 'testing/redirects/third-page.md'
        anchors:
          'item-b': 
            
  
```

### Redirect preserving all anchors

This example redirects `4th-page.md#anchor` to `5th-page.md#anchor`:

```yaml
redirects:
  'testing/redirects/4th-page.md': 'testing/redirects/5th-page.md'
```
### Redirect stripping all anchors

This example strips all anchors from the source page.
Any remaining links resolving to anchors on `7th-page.md` will fail link validation.

```yaml
redirects
  'testing/redirects/7th-page.md':
    to: 'testing/redirects/5th-page.md'
    anchors: '!'
```

Alternate syntax:

```yaml
redirects:
  'testing/redirects/7th-page.md': '!testing/redirects/5th-page.md'
```

To handle removed anchors on a page that still exists, omit the `to:` field:

```yaml
  'testing/redirects/third-page.md':
    anchors:
      'removed-anchor':
```

### Redirect with renamed anchors

This example redirects:

- `first-page-old.md#old-anchor` → `second-page.md#active-anchor`
- `first-page-old.md#removed-anchor` → `second-page.md`
- Any other anchor is passed through and validated normally.

```yaml
redirects:
  'testing/redirects/first-page-old.md':
    to: 'testing/redirects/second-page.md'
    anchors:
      'old-anchor': 'active-anchor'
      'removed-anchor':
```

### Redirecting to other repositories

Use the `repo://path/to/page.md` syntax to redirect across repositories.

```yaml
redirects:
  'testing/redirects/cross-repo-page.md': 'other-repo://reference/section/new-cross-repo-page.md'
```

### Managing complex scenarios with anchors

* `to`, `anchor` and `many` can be used together to support more complex scenarios.
* Setting `to` at the top level determines the default case, which can be used for partial redirects.
* Cross-repository links are supported, with the same syntax as in the previous example.
* The existing rules for `anchors` also apply here. To define a catch-all redirect, use `{}`.

```yaml
redirects:
  # In this first scenario, the default redirection target remains the same page, with anchors being preserved. 
  # Omitting the ``anchors`` tag or explicitly setting it as empty are both supported.
  'testing/redirects/8th-page.md':
    to: 'testing/redirects/8th-page.md'
    many:
      - to: 'testing/redirects/second-page.md'
        anchors:
          'item-a': 'yy'
      - to: 'testing/redirects/third-page.md'
        anchors:
          'item-b':

  # In this scenario, the default redirection target is a different page, and anchors are dropped.
  'testing/redirects/deleted-page.md':
    to: 'testing/redirects/5th-page.md'
    anchors: '!'
    many:
      - to: "testing/redirects/second-page.md"
        anchors:
          "aa": "zz"
          "removed-anchor":
      - to: "other-repo://reference/section/partial-content.md"
        anchors:
          "bb": "yy"
```
