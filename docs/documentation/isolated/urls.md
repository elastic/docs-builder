---
navigation_title: URLs
---

# URL structure

The file structure of each content set directly impacts the URL path of each page. The directory structure you give {{dbuild}} is the directory structure it produces.

Paths are relative to your `docset.yml` file, not the repository root. For example, if `docset.yml` is in `./docs/` and a page is at `./docs/a/b/c/my.md`, its URL path is `/a/b/c/my`.

This is true for isolated builds (`docs-builder serve`) and codex environments. In assembler builds, the assembled site can change this path with `path_prefix` in `navigation.yml`.

## Navigation is independent of file structure

Navigation order is set in `docset.yml` and `toc.yml` files, not by directory or file naming. See [Navigation](./navigation.md) to learn more.
