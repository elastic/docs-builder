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

### Redirecting to other repositories

It is possible to redirect to other repositories. The syntax is the same as when linking on documentation sets:

* 'other-repo://reference/section/new-cross-repo-page.md'

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
