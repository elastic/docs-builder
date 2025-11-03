# Navigation

## File Navigation types

### Isolated builds

When building a signle repository's documentation, we are building the table of contents defined in `docset.yml` the root of which is represented by ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `DocumentationSetNavigation`

The table of contents is composed of a number of `NavigationItem`s.

- `folder:` ![FolderNavigation](images/bullet-folder-navigation.svg) `FolderNavigation`
- `file:` ![FileNavigationLeaf](images/bullet-file-navigation-leaf.svg) `FileNavigationLeaf`

`docset.yml` may break up it's table of contents into multiple sub navigation's using nested `toc.yml` files.

- `toc:` ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) TableOfContentsNavigation

### Assembler builds

The assembler build takes multiple `Isolated Build` navigations and recomposes it into a single ![SiteNavigation](images//bullet-site-navigation.svg) `SiteNavigation` navigation.

This navigation is defined in [`navigation.yml`](https://github.com/elastic/docs-builder/blob/main/config/navigation.yml)

An assembler build can only reference:

- ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `DocumentationSetNavigation` (using `<repository>://` crosslink)
- ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) `TableOfContentsNavigation` (using `<repository>://<path/toc/folder>` crosslink)

A new special root is created for asssembler builds:

- ![SiteNavigation](images//bullet-site-navigation.svg) `SiteNavigation`

## A Visual Example

### Isolated builds

Imagine we have the following `docset.yml` that defines two nested `toc.yml` files.

![Isolated Build](images/isolated-build-tree.svg)

### Assembler builds

Now we can break that navigation up into multiple sections of the wider site navigation.

- ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg)`docs-content://api`
  - ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg)**`elastic-project://api`**
- ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg)`docs-content://guides`
  - ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg)**`elastic-project://guides`**
- ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `elastic-client://`
  - ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `elastic-client-node://`
  - ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `elastic-client-dotnet://`
  - ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `elastic-client-java://`


![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `DocumentationSetNavigation` may arbitrary nest other
![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) `TableOfContentsNavigation` and visa versa.

The only requirement is that each node in the navigation **MUST** define a unique `path_prefix`.

#### A fully resolved navigation.

When resolving the navigation, all the child navigation items will be included and the url will dynamically be `re-homed` to the root ![DocumentationSetNavigation](images/bullet-documentation-set-navigation.svg) `DocumentationSetNavigation` or ![TableOfContentsNavigation](images/bullet-table-of-contents-navigation.svg) `TableOfContentsNavigation` that defines it. 

![Assembler Build](images/assembler-build-tree.svg)

