# Elastic.Documentation.Navigation

This library provides a way to build navigation trees for documentation sets.

## Documentation Sets

When building single documentation sets you use docset.yml to declare the toc.

When we mention urls these are rooted at `/` unless `--canonical-base-url` is specified in which case the root is `/<canonical-base-url>`

```yaml
toc:
  - file: index.md
```

It supports the following children

### A single file

```yaml
toc:
  - file: index.md
  - file: getting-started.md
```
These would result in the following Url's `/` and `/getting-started`

From here on out the expected url appears as comment in the example

### Folders

```yaml
toc:
  - folder: getting-started # /getting-started
  - folder: syntax # /syntax
    children: 
      - file: index.md #/syntax
      - file: blocks.md #/syntax/blocks
```

* if `folder` does not specify children the folder will be scanned for Markdown files.
* The url for the folder is the same as its index.
* The index is determined by having an `index.md` otherwise it's the first file that is listed/found.
* `children` paths are scope to the folder.
  * Here we are including the files `blocks.md` and `index.md` in the `syntax` folder.

### Folders with a file

If you don't want to follow the `folder/index.md` pattern but instead want to have the index file one level up e.g

```
getting-started.md
getting-started/
  install.md
```

You can do this by specifying the `file` property directly on the folder.


```yaml
toc:
  - folder: getting-started # /getting-started
    file: getting-started.md
    children: 
      - file: install.md # /getting-started/install
```

* `file` is the index file for the folder.
* `children` paths are scope to the folder.
  * Here we are including the files `install.md` in the `getting-started` folder.
* deep linking on the folder `file` is NOT supported
* It's best practice to name the file like the folder. We emit a hint if this is not the case.

### Virtual Files

```yaml
toc:
  - file: index.md # /
    children:
      - file: getting-started.md # /getting-started
      - file: setup.md # /setup
      - folder: syntax # /syntax
        children:
          - file: blocks.md # /syntax/blocks
          - file: index.md # /syntax
```

A file can specify `children` without having baring on it's children path on disk or url structure

```yaml
toc:
  - file: getting-started.md # /getting-started
    children:
      - file: setup.md # /setup
```

#### Deeplinking virtual files

```yaml
toc:
  - file: a/b/c/getting-started.md # /a/b/c/getting-started
    children:
      - file: a/b/c/setup.md # /a/b/c/setup
      - file: c/b/c/setup.md # /c/b/c/setup
```

While supported, this is not recommended.
* Favor `folder` over `file` when possible.
* Navigation should follow the file structure as much as possible.
* Virtual files are primarily intended to group sibling files together.

`docs-builder` will hint when these guidelines are not followed.


### Nested Table Of Contents

A `docset.yml` may include a `toc.yml` that itself contains a `toc` and may include more `toc.yml` files.

Given this `docset.yml`
```yaml
toc:
  - toc: getting-started # /getting-started
  - file: index.md # /
```

`docs-builder` will include the `getting-started/toc.yml` file.

```yaml
toc:
  - file: index.md # / getting-started
  - file: install.md # / getting-started/install
```

Note that the `toc` creates a scope both for paths and urls.

A `toc` reference may **NOT** have children of its own and must appear at the top level of a `toc:` section inside a `docset.yml` or `toc.yml` file.

`docset.yml` defines how many levels deep we may include a `toc` reference. The default is `1`

```yaml
max_toc_depth: 2
```


## Global Navigation.

The global navigation is defined in `config/navigation.yml` and is used to build a single global navigation for all documentation sets defined in `config/assembler.yml`.

The global navigation is built by
* `docs-builder assemble` 
* or calling `docs-builder assmembler clone` and `docs-builder assmembler build`

`config/navigation.yml` links ALL `docset.yml` and `toc.yml` files. 

Repositories in `config/assembler.yml` MAY be included in the global navigation. Once they do ALL their `docset.yml` and `toc.yml` files MUST be configured.

```yaml
toc:
  - toc: get-started
  - toc: extend
    children:
      - toc: kibana://extend
        path_prefix: extend/kibana
      - toc: logstash://extend
        path_prefix: extend/logstash
```

The toc follows a `<repository>://<path>` scheme. 

* If `<repository>` is not defined it's the narrative repository (`docs-builder`).
`path_prefix` is mandatory.
  * unless `<repository>` is not defined in which it defaults to `<path>`.
* `path_prefix`'s MUST be unique

