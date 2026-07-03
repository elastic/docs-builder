---
navigation_title: Navigation layout
---

# Navigation layout

Navigation structure is defined in the `toc` key of `docset.yml` (and optionally in nested `toc.yml` files). Two nav file types are supported:

* **`docset.yml`** — root configuration for the content set
* **`toc.yml`** — optional per-folder navigation for large sections

## Reference

* [`docset.yml` reference](docset-reference.md) — all top-level configuration keys (`cross_links`, `api`, `release_notes`, and more)
* [TOC reference](toc-reference.md) — syntax for `file`, `folder`, `hidden`, `toc`, `crosslink`, and other `toc` entry types

## Layout patterns

For patterns, trade-offs, and worked examples — when to use a `folder` without `children`, virtual file groupings, nested `toc.yml` files, and mixed structures — see [Documentation set navigation](../../building-blocks/documentation-set-navigation.md).

That guide covers:

* TOC node types and file path rules
* Dedicated `toc.yml` files for modular sections
* Common patterns (single file, folder with explicit children, folder + file combination, nested toc references)
* Suppressing diagnostic hints when you have a valid reason to deviate from defaults

## Minimum navigation

At minimum, every content set needs an `index.md` referenced in `toc`:

```yaml
toc:
  - file: index.md
```

See [File structure](file-structure.md) for how directory layout maps to URLs.
