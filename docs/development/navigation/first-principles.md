# First Principles

This document outlines the fundamental principles that guide the design of `Elastic.Documentation.Navigation`.

## Core Principles

### 1. Two-Phase Loading

Navigation construction follows a strict two-phase approach:

**Phase 1: Configuration Resolution** (`Elastic.Documentation.Configuration`)
- Parse YAML files (`docset.yml`, `toc.yml`, `navigation.yml`)
- Resolve all file references to **full paths** relative to documentation set root
- Validate configuration structure and relationships
- Output: Fully resolved configuration objects with complete file paths

**Phase 2: Navigation Construction** (`Elastic.Documentation.Navigation`)
- Consume resolved configuration from Phase 1
- Build navigation tree with **full URLs**
- Create node relationships (parent/child/root)
- Set up home providers for URL calculation
- Output: Complete navigation tree with calculated URLs

**Why Two Phases?**
- **Separation of Concerns**: Configuration parsing is independent of navigation structure
- **Validation**: Catch file/structure errors before building expensive navigation trees
- **Reusability**: Same configuration can build different navigation structures (isolated vs assembler)
- **Performance**: Resolve file system operations once, reuse for navigation

### 2. Single Documentation Source

URLs are always built relative to the documentation set's source directory:
- Files referenced in `docset.yml` are relative to the docset root
- Files referenced in nested `toc.yml` are relative to the toc directory
- During Phase 1, all paths are resolved to be relative to the docset root
- During Phase 2, URLs are calculated from these resolved paths

**Example:**
```
docs/
├── docset.yml          # Root
├── index.md
└── api/
    ├── toc.yml         # Nested TOC
    └── rest.md
```

Phase 1 resolves `api/toc.yml` reference to `rest.md` as: `api/rest.md` (relative to docset root)
Phase 2 builds URL as: `/api/rest/`

### 3. URL Building is Dynamic and Cheap

URLs are **calculated on-demand**, not stored:
- Nodes don't store their final URL
- URLs are computed from `HomeProvider.PathPrefix` + relative path
- Changing a `HomeProvider` instantly updates all descendant URLs
- No tree traversal needed to update URLs

**Why Dynamic?**
- **Re-homing**: Same subtree can have different URLs in different contexts
- **Memory Efficient**: Don't store redundant URL strings
- **Consistency**: URLs always reflect current home provider state

### 4. Navigation Roots Can Be Re-homed

A key design feature that enables assembler builds:
- **Isolated Build**: Each `DocumentationSetNavigation` is its own root
- **Assembler Build**: `SiteNavigation` becomes the root, docsets are "re-homed"
- **Re-homing**: Replace a subtree's `HomeProvider` to change its URL prefix
- **Cheap Operation**: O(1) - just replace the provider reference

**Example:**
```csharp
// Isolated: URLs start at /
homeProvider.PathPrefix = "";
// → /api/rest/

// Assembled: Re-home to /guide
homeProvider = new NavigationHomeProvider("/guide", siteNav);
// → /guide/api/rest/
```

### 5. Navigation Scope via HomeProvider

`INavigationHomeProvider` creates navigation scopes:
- **Provider**: Defines `PathPrefix` and `NavigationRoot` for a scope
- **Accessor**: Children use `INavigationHomeAccessor` to access their scope
- **Inheritance**: Child nodes inherit their parent's accessor
- **Isolation**: Changes to a provider only affect its scope

**Scope Creators:**
- `DocumentationSetNavigation` - Creates scope for entire docset
- `TableOfContentsNavigation` - Creates scope for TOC subtree (enables re-homing)

**Scope Consumers:**
- `FileNavigationLeaf` - Uses accessor to calculate URL
- `FolderNavigation` - Passes accessor to children
- `VirtualFileNavigation` - Passes accessor to children

### 6. Index Files Determine Folder URLs

Every folder/node navigation has an **Index**:
- Index is either `index.md` or the first file
- The node's URL is the same as its Index's URL
- Children appear "under" the index in navigation
- Index files map to folder paths: `/api/index.md` → `/api/`

**Why?**
- **Consistent URL Structure**: Folders and their indexes share the same URL
- **Natural Navigation**: Index represents the folder's landing page
- **Hierarchical**: Clear parent-child URL relationships

### 7. File Structure Should Mirror Navigation

Best practices for maintainability:
- Navigation structure should follow file system structure
- Avoid deep-linking files from different directories
- Use `folder:` references when possible
- Virtual files should group sibling files, not restructure the tree

**Rationale:**
- **Discoverability**: Developers can find files by following navigation
- **Predictability**: URL structure matches file structure
- **Maintainability**: Moving files in navigation matches moving them on disk

### 8. Generic Type System for Covariance

Navigation classes are generic over `TModel`:
```csharp
public class DocumentationSetNavigation<TModel>
    where TModel : class, IDocumentationFile
```

**Why Generic?**
- **Covariance**: Can treat `DocumentationSetNavigation<MarkdownFile>` as `IRootNavigationItem<IDocumentationFile, INavigationItem>`
- **Type Safety**: Factory pattern ensures correct model types
- **Flexibility**: Same navigation code works with different file models

### 9. Lazy URL Calculation with Caching

`FileNavigationLeaf` implements smart URL caching:
```csharp
private string? _homeProviderCache;
private string? _urlCache;

public string Url
{
    get
    {
        if (_homeProviderCache == HomeProvider.Id && _urlCache != null)
            return _urlCache;

        _urlCache = CalculateUrl();
        _homeProviderCache = HomeProvider.Id;
        return _urlCache;
    }
}
```

**Strategy:**
- Cache URL along with HomeProvider ID
- Invalidate cache when HomeProvider changes
- Recalculate only when needed
- O(1) for repeated access, O(path) for calculation

### 10. Phantom Nodes for Incomplete Navigation

`navigation.yml` can declare phantoms:
```yaml
phantoms:
  - source: plugins://
```

**Purpose:**
- Reference nodes that exist but aren't included in site navigation
- Prevent "undeclared navigation" warnings
- Document intentionally excluded content
- Enable validation of cross-links

## Design Patterns

### Factory Pattern for Node Creation

`DocumentationNavigationFactory` creates navigation items:
- Encapsulates construction logic
- Ensures consistent initialization
- Centralizes node creation
- Type-safe generic methods

### Provider Pattern for URL Context

`INavigationHomeProvider` / `INavigationHomeAccessor`:
- Providers define context (PathPrefix, NavigationRoot)
- Accessors reference providers
- Decouples URL calculation from tree structure
- Enables context switching (re-homing)

### Visitor Pattern for Tree Operations

Navigation items implement interfaces for traversal:
- `IRootNavigationItem<TModel, TChild>` - Root nodes
- `INodeNavigationItem<TModel, TChild>` - Nodes with children
- `ILeafNavigationItem<TModel>` - Leaf nodes
- Common `INavigationItem` base for polymorphic traversal

## Key Invariants

1. **Phase Order**: Configuration must be fully resolved before navigation construction
2. **Path Resolution**: All paths in configuration are relative to docset root after Phase 1
3. **URL Uniqueness**: Every navigation item must have a unique URL within its site
4. **Root Consistency**: All nodes in a subtree point to the same `NavigationRoot`
5. **Provider Validity**: A node's `HomeProvider` must be an ancestor in the tree
6. **Index Requirement**: All node navigations (folder/toc/docset) must have an Index
7. **Path Prefix Uniqueness**: In assembler builds, all `path_prefix` values must be unique

## Performance Characteristics

- **Tree Construction**: O(n) where n = number of files
- **URL Calculation**: O(depth) for first access, O(1) with caching
- **Re-homing**: O(1) - just replace HomeProvider reference
- **Tree Traversal**: O(n) for full tree, but rarely needed
- **Memory**: O(n) for nodes, URLs computed on-demand
