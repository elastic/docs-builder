# File structure

The file structure of each content set directly impacts the URL path of each page. The directory structure you pull into docs-builder is the directory structure it produces.

Paths are relative to your `docset.yml` file, not the repository root. For example, if `docset.yml` is in `./docs/` and a page is at `./docs/a/b/c/my.md`, its URL path is `/a/b/c/my`.

This is true for isolated builds like `docs-builder serve` and codex.elastic.dev. On elastic.co/docs, the assembled site can change this path with [`path_prefix`](../site/navigation.md#path_prefix-optional) in navigation.yml.

## Navigation

Navigation is set independent of directory and file structure in docset and toc files.

See [Navigation](./navigation.md) to learn more.
