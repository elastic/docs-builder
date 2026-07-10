# File structure

The file structure of each content set directly impacts the URL path of each page. The directory structure you pull into docs-builder is the same as the directory structure it produces, but paths are resolved **relative to the location of your `docset.yml` file**, not the repository root.

Any directories above `docset.yml` are not part of the URL path.

For example, if `docset.yml` lives in `./docs/` and a page lives at `./docs/a/b/c/my.md`, the resulting URL path is `/a/b/c/my` (the `./docs/` prefix is dropped).

## Navigation

Navigation is set independent of directory and file structure in docset and toc files.

See [Navigation](./navigation.md) to learn more.
