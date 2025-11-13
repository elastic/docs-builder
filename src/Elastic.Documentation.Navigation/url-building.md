# Url Building

There are two modes of building a navigation

## Isolated 

### DocumentationSetNavigation

Is the root of the navigation for isolated builds:

Url is `path_prefix` from `BuildContext` + `/` from `DocumentationSet`

### TableOfContentsNavigation 

Models a rehomable portion of the navigation.

Url is `path_prefix` from `BuildContext` + `folder/path` to the `toc.yml` file defining the `TableOfContentsNavigation`

`TableOfContentsNavigation` needs to create a new scope for its children utilizing a new instance of `INavigationHomeAccessor` using `this` as the `INavigationHomeProvider`.

We are not actually changing the PathPrefix, we create the scope to be able to rehome during `Assembler` builds.

### Navigation Scopes

DocumentationSetNavigation and TableOfContentsNavigation create a new scope for the navigation using `INavigationHomeProvider` children use `INavigationHomeAccessor` to access their scope's `PathPrefix` and `NavigationRoot`. In isolated builds the `NavigationRoot` is always the `DocumentationSetNavigation`.

`INavigationHomeAccessor` needs to be passed down the navigation tree to ensure we can calculate `Url` and `NavigationRoot` dynamically.

#### FileNavigationLeaf

Url is `path_prefix` from `DocumentationSetNavigation` + `path` to the markdown file.
FileNavigationLeaf utilizes `INavigationHomeAccessor` to query for `path_prefix` and `NavigationRoot`.
Rules for `path`:
  * Relative to `DocumentationSet` (use `FileRef.Path`)
  * if the name is `index.md` then the `path` is only the folder portion.
  * Otherwise, include the name of the file without its extension.

### FolderNavigation

Url is the url of its `Index` property.

### VirtualFileNavigation

Url is the url of its `Index` property.

### CrossLinkNavigationLeaf

Url is the resolved url of `CrossLinkRef.CrossLinkUri` using `ICrossLinkResolver`.


## Assembler

The assembler is responsible for building the navigation for the entire site.
It utilizes `SiteNavigation` to build its own navigation. 

`SiteNavigation` receives a set of composed `DocumentationSets` and utilizes `navigation.yml` (`NavigationFile`) to restructure the navigation. `SiteNavigation` holds all `DocumentationSetNavigation` and `TableOfContentsNavigation` under `_nodes` and uses the references to build its own navigation.

Using `path_prefix` defined in `navigation.yml` it will update the `PathPrefix` and `NavigationRoot` of the `DocumentationSetNavigation` and `TableOfContentsNavigation`. 

The NavigationRoot anything under the top level navigation itemsis the top level navigation item. 

The root of top level navigation items is the `DocumentationSetNavigation`.


`SiteNavigation` also steals the top level leaf navigation nodes from the `NarrativeRepository.RepositoryName` DocumentationSetNavigation to act as its own top level navigation items.




