# Navigation Node Types

This document provides a detailed reference for each navigation node type in `Elastic.Documentation.Navigation`.

> **Context:** For the acyclic graph structure that these nodes form, see [Functional Principles #8](functional-principles.md#8-acyclic-graph-structure).

## Type Hierarchy

```
INavigationItem
├── ILeafNavigationItem<TModel>
│   ├── FileNavigationLeaf<TModel>
│   └── CrossLinkNavigationLeaf
│
└── INodeNavigationItem<TModel, TChild>
    ├── IRootNavigationItem<TModel, TChild>
    │   ├── DocumentationSetNavigation<TModel>
    │   ├── TableOfContentsNavigation<TModel>
    │   └── SiteNavigation
    │
    ├── FolderNavigation<TModel>
    └── VirtualFileNavigation<TModel>
```

## Common Properties

All navigation items implement `INavigationItem`:

```csharp
public interface INavigationItem
{
    /// <summary>The URL for this navigation item</summary>
    string Url { get; }

    /// <summary>Title displayed in navigation</summary>
    string NavigationTitle { get; }

    /// <summary>Root of the navigation tree</summary>
    IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

    /// <summary>Parent in the tree, null for roots</summary>
    INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

    /// <summary>Whether this item is hidden from navigation</summary>
    bool Hidden { get; }

    /// <summary>Breadth-first index in the tree</summary>
    int NavigationIndex { get; set; }
}
```

---

## Leaf Nodes

Leaf nodes have no children. They represent individual documentation files or external links.

### FileNavigationLeaf<TModel>

Represents an individual markdown file in the documentation.

**Location:** `src/Elastic.Documentation.Navigation/Isolated/FileNavigationLeaf.cs`

**YAML Declaration:**
```yaml
toc:
  - file: getting-started.md
  - file: api/overview.md  # Can deep-link
  - hidden: 404.md         # Hidden from navigation
```

**Key Features:**
- URL calculated dynamically from home provider + relative path
- Smart caching (see [Home Provider Architecture](home-provider-architecture.md))
- Handles index files specially: `folder/index.md` → `/folder/`
- Can be hidden from navigation while remaining accessible

**URL Calculation:**
```csharp
public string Url
{
    get
    {
        var rootUrl = _homeAccessor.HomeProvider.PathPrefix.TrimEnd('/');
        var relativePath = DetermineRelativePath();

        // Remove .md extension
        var path = relativePath.EndsWith(".md") ? relativePath[..^3] : relativePath;

        // Handle index files
        if (path.EndsWith("/index"))
            path = path[..^6];
        else if (path.Equals("index"))
            return string.IsNullOrEmpty(rootUrl) ? "/" : $"{rootUrl}/";

        return $"{rootUrl}/{path.TrimEnd('/')}/";
    }
}
```

**Example:**
```
File: docs/api/rest.md
PathPrefix: "/guide"
URL: /guide/api/rest/

File: docs/index.md
PathPrefix: "/guide"
URL: /guide/
```

**Constructor:**
```csharp
public FileNavigationLeaf(
    TModel model,              // The documentation file model
    IFileInfo fileInfo,        // File system info
    FileNavigationArgs args)   // Construction arguments
```

**Arguments:**
- `RelativePathToDocumentationSet` - Path from docset root (for URL calculation)
- `RelativePathToTableOfContents` - Path from TOC root (for assembler builds)
- `Hidden` - Whether hidden from navigation
- `NavigationIndex` - Initial index (will be recalculated)
- `Parent` - Parent node
- `HomeAccessor` - For accessing path prefix and navigation root

---

### CrossLinkNavigationLeaf

Represents a link to external documentation or different documentation set.

**Location:** `src/Elastic.Documentation.Navigation/Isolated/CrossLinkNavigationLeaf.cs`

**YAML Declaration:**
```yaml
toc:
  - title: "External Guide"
    crosslink: https://example.com/guide
  - title: "Other Docset"
    crosslink: docs-content://guide.md
```

**Key Features:**
- URL is the crosslink itself (not calculated)
- Can link to external sites or use crosslink scheme
- Title is required (no auto-title from file)
- Always marked as `IsCrossLink = true`

**Constructor:**
```csharp
public CrossLinkNavigationLeaf(
    CrossLinkModel model,                              // Contains Uri and title
    string url,                                        // The crosslink URL
    bool hidden,                                       // Hidden from navigation?
    INodeNavigationItem<...>? parent,                  // Parent node
    INavigationHomeAccessor homeAccessor)              // For navigation root
```

**Example:**
```csharp
new CrossLinkNavigationLeaf(
    new CrossLinkModel(new Uri("https://elastic.co"), "Elastic Docs"),
    "https://elastic.co",
    hidden: false,
    parent: this,
    homeAccessor: this
)
// URL: https://elastic.co
// NavigationTitle: "Elastic Docs"
```

---

## Node Types (With Children)

Node types can have child navigation items. They represent structural elements of the documentation.

### FolderNavigation<TModel>

Represents a directory in the file system with markdown files.

**Location:** `src/Elastic.Documentation.Navigation/Isolated/FolderNavigation.cs`

**YAML Declaration:**
```yaml
toc:
  - folder: getting-started
    # Auto-discovers markdown files in the folder

  - folder: api
    children:
      - file: index.md
      - file: rest.md
      # Explicit children, no auto-discovery
```

**Key Features:**
- URL is the same as its `Index` property
- Index is either `index.md` or first file
- Can auto-discover markdown files if no children specified
- Children paths are scoped to the folder

**Properties:**
```csharp
public class FolderNavigation<TModel>
{
    public string FolderPath { get; }  // Relative path to folder
    public ILeafNavigationItem<TModel> Index { get; }  // Folder's index file
    public IReadOnlyCollection<INavigationItem> NavigationItems { get; }  // Children
}
```

**URL:**
```csharp
public string Url => Index.Url;  // Same as index file
```

**Example:**
```
Folder: docs/getting-started/
Files:
  - index.md
  - install.md
  - configure.md

Navigation:
FolderNavigation
  Index: getting-started/index.md → /getting-started/
  NavigationItems:
    - install.md → /getting-started/install/
    - configure.md → /getting-started/configure/
```

---

### VirtualFileNavigation<TModel>

Represents a file with children defined in YAML (not file system structure).

**Location:** `src/Elastic.Documentation.Navigation/Isolated/VirtualFileNavigation.cs`

**YAML Declaration:**
```yaml
toc:
  - file: getting-started.md
    children:
      - file: install.md
      - file: configure.md
      # Children can be anywhere in the file system
```

**Key Features:**
- Allows grouping files without matching file system structure
- Index is the file itself
- Children don't have to be in the same directory
- URL is the same as its `Index` property

**Properties:**
```csharp
public class VirtualFileNavigation<TModel>
{
    public ILeafNavigationItem<TModel> Index { get; }  // The file itself
    public IReadOnlyCollection<INavigationItem> NavigationItems { get; }  // Virtual children
}
```

**Example:**
```
File: docs/getting-started.md
Children (defined in YAML):
  - docs/install.md
  - docs/setup.md

Navigation:
VirtualFileNavigation
  Index: getting-started.md → /getting-started/
  NavigationItems:
    - install.md → /install/
    - setup.md → /setup/
```

**Use Cases:**
- Grouping related files that aren't in the same directory
- Creating navigation structure independent of file structure
- Collecting files under a parent concept

**Best Practice:** Use sparingly. Prefer `FolderNavigation` when file structure can match navigation structure.

---

## Root Node Types

Root nodes can be re-homed in assembler builds. They implement `IRootNavigationItem<TModel, TChild>`.

### DocumentationSetNavigation<TModel>

Represents the root navigation for a documentation set (`docset.yml`).

**Location:** `src/Elastic.Documentation.Navigation/Isolated/DocumentationSetNavigation.cs`

**Source:** `docset.yml` file

**Key Features:**
- Root of navigation tree in isolated builds
- Can be re-homed in assembler builds
- Creates home provider scope
- Implements both `INavigationHomeProvider` and `INavigationHomeAccessor`
- Has unique identifier: `{repository}://`

**Properties:**
```csharp
public class DocumentationSetNavigation<TModel>
    : IRootNavigationItem<TModel, INavigationItem>
    , INavigationHomeProvider
    , INavigationHomeAccessor
{
    public Uri Identifier { get; }  // e.g., elastic-docs://
    public string PathPrefix { get; }  // URL prefix for this docset
    public GitCheckoutInformation Git { get; }  // Repository info
    public INavigationHomeProvider HomeProvider { get; set; }  // For re-homing!

    public ILeafNavigationItem<TModel> Index { get; }  // Docset index
    public IReadOnlyCollection<INavigationItem> NavigationItems { get; }  // Top-level items
    public bool IsUsingNavigationDropdown { get; }  // From features.primary-nav
}
```

**Isolated Build:**
```csharp
// In isolated builds, it's its own home provider
DocumentationSetNavigation
{
    NavigationRoot = this,
    HomeProvider = this,
    PathPrefix = "",
    Identifier = new Uri("elastic-docs://")
}
// Child URL: /api/rest/
```

**Assembler Build (Re-homed):**
```csharp
// In assembler builds, re-homed to site navigation
DocumentationSetNavigation
{
    NavigationRoot = SiteNavigation,              // Changed!
    HomeProvider = new NavigationHomeProvider(    // Changed!
        pathPrefix: "/guide",
        navigationRoot: SiteNavigation
    ),
    PathPrefix = "/guide",                        // From new provider
    Identifier = new Uri("elastic-docs://")       // Unchanged
}
// Child URL: /guide/api/rest/
```

**Re-homing:**
```csharp
// This is all it takes!
docsetNav.HomeProvider = new NavigationHomeProvider("/guide", siteNav);
```

---

### TableOfContentsNavigation<TModel>

Represents a nested `toc.yml` file within a documentation set.

**Location:** `src/Elastic.Documentation.Navigation/Isolated/TableOfContentsNavigation.cs`

**Source:** `toc.yml` file

**YAML Declaration (in docset.yml or parent toc.yml):**
```yaml
toc:
  - toc: api
  - toc: guides
```

**Key Features:**
- **Creates a scope only in assembler builds** (for independent re-homing)
- In isolated builds, inherits HomeProvider from DocumentationSetNavigation
- Can be re-homed independently in assembler builds
- Implements both `INavigationHomeProvider` and `INavigationHomeAccessor`
- Has unique identifier: `{repository}://{path}`
- Cannot have children defined in YAML (children come from toc.yml file)

**Properties:**
```csharp
public class TableOfContentsNavigation<TModel>
    : IRootNavigationItem<TModel, INavigationItem>
    , INavigationHomeProvider
    , INavigationHomeAccessor
{
    public Uri Identifier { get; }  // e.g., elastic-docs://api
    public string ParentPath { get; }  // Path to toc folder
    public string PathPrefix { get; }  // URL prefix
    public IDirectoryInfo TableOfContentsDirectory { get; }  // Physical directory
    public INavigationHomeProvider HomeProvider { get; set; }  // For re-homing!

    public ILeafNavigationItem<TModel> Index { get; }  // TOC index
    public IReadOnlyCollection<INavigationItem> NavigationItems { get; }  // TOC items
}
```

**Example:**
```
Docset: elastic-docs
TOC: api/toc.yml

In isolated build:
TableOfContentsNavigation
{
    Identifier = new Uri("elastic-docs://api"),
    ParentPath = "api",
    PathPrefix = "",
    NavigationRoot = DocumentationSetNavigation,
    HomeProvider = DocumentationSetNavigation.HomeProvider  // ← Inherited, no new scope
}
// Child URL: /api/rest/

In assembler build (creates its own scope for re-homing):
TableOfContentsNavigation
{
    Identifier = new Uri("elastic-docs://api"),
    ParentPath = "api",
    PathPrefix = "/reference",                    // Different from docset!
    NavigationRoot = SiteNavigation,
    HomeProvider = new NavigationHomeProvider(    // ← New scope created!
        pathPrefix: "/reference",
        navigationRoot: SiteNavigation
    )
}
// Child URL: /reference/rest/
```

**Re-homing:**
```csharp
// TOCs can be re-homed independently from their parent docset!
tocNav.HomeProvider = new NavigationHomeProvider("/reference", siteNav);
```

**Use Case:** Allows assembler builds to split a docset across multiple site sections.

---

### SiteNavigation

Represents the root navigation for an assembled documentation site.

**Location:** `src/Elastic.Documentation.Navigation/Assembler/SiteNavigation.cs`

**Source:** `config/navigation.yml`

**Key Features:**
- Only exists in assembler builds
- Ultimate root of the navigation tree
- Re-homes child DocumentationSetNavigation and TableOfContentsNavigation nodes
- Manages `path_prefix` mappings from navigation.yml
- Tracks phantom nodes (declared but not included)
- Has unique identifier: `site://`

**Properties:**
```csharp
public class SiteNavigation
    : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
    public Uri Identifier { get; } = new Uri("site://");
    public string Url { get; }  // Site prefix or "/"

    // All docset/TOC nodes indexed by identifier
    public IReadOnlyDictionary<Uri, IRootNavigationItem<...>> Nodes { get; }

    // Top-level navigation items
    public ILeafNavigationItem<IDocumentationFile> Index { get; }
    public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

    // Phantom tracking
    public IReadOnlyCollection<PhantomRegistration> Phantoms { get; }
    public HashSet<Uri> DeclaredPhantoms { get; }
    public ImmutableHashSet<Uri> DeclaredTableOfContents { get; }
}
```

**Example:**
```yaml
# config/navigation.yml
toc:
  - toc: elastic-docs://
    path_prefix: guide

  - toc: elastic-docs://api
    path_prefix: reference

phantoms:
  - source: plugins://  # Not included in navigation
```

```csharp
SiteNavigation
{
    Identifier = new Uri("site://"),
    NavigationRoot = this,
    Nodes = {
        [new Uri("elastic-docs://")] = DocumentationSetNavigation { ... },
        [new Uri("elastic-docs://api")] = TableOfContentsNavigation { ... },
    },
    NavigationItems = [
        DocumentationSetNavigation (re-homed to /guide),
        TableOfContentsNavigation (re-homed to /reference)
    ]
}
```

**Re-homing Logic:**
```csharp
// From SiteNavigation.cs:211
private INavigationItem? CreateSiteTableOfContentsNavigation(...)
{
    var pathPrefix = $"{_sitePrefix}/{tocRef.PathPrefix}".Trim('/');

    // Look up the node
    if (!_nodes.TryGetValue(tocRef.Source, out var node))
        return null;

    // Re-home it!
    homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, root);

    // All URLs in subtree now use pathPrefix!
    return node;
}
```

---

## Type Comparison Table

| Type | Has Children | Is Root | Can Be Re-homed | Creates Scope | URL Source |
|------|-------------|---------|-----------------|---------------|------------|
| **FileNavigationLeaf** | ❌ | ❌ | ❌ | ❌ | Calculated from path + prefix |
| **CrossLinkNavigationLeaf** | ❌ | ❌ | ❌ | ❌ | Crosslink URI itself |
| **FolderNavigation** | ✅ | ❌ | ❌ | ❌ | Same as Index |
| **VirtualFileNavigation** | ✅ | ❌ | ❌ | ❌ | Same as Index |
| **DocumentationSetNavigation** | ✅ | ✅ | ✅ | ✅ (always) | Same as Index |
| **TableOfContentsNavigation** | ✅ | ✅ | ✅ | ✅ (assembler only) | Same as Index |
| **SiteNavigation** | ✅ | ✅ | ❌ | ✅ (always) | Site prefix or "/" |

**Note:** TableOfContentsNavigation only creates its own scope in assembler builds to enable independent re-homing. In isolated builds, it inherits the HomeProvider from its parent DocumentationSetNavigation.

## Factory Methods

Navigation items are created through factory methods in `DocumentationNavigationFactory`:

```csharp
public static class DocumentationNavigationFactory
{
    // Create a file leaf
    public static ILeafNavigationItem<TModel> CreateFileNavigationLeaf<TModel>(
        TModel model,
        IFileInfo fileInfo,
        FileNavigationArgs args)
        where TModel : IDocumentationFile
        => new FileNavigationLeaf<TModel>(model, fileInfo, args)
           { NavigationIndex = args.NavigationIndex };

    // Create a virtual file node
    public static VirtualFileNavigation<TModel> CreateVirtualFileNavigation<TModel>(
        TModel model,
        IFileInfo fileInfo,
        VirtualFileNavigationArgs args)
        where TModel : IDocumentationFile
        => new(model, fileInfo, args)
           { NavigationIndex = args.NavigationIndex };
}
```

**Why Factory Methods?**
- Encapsulate creation logic
- Ensure consistent initialization (NavigationIndex)
- Type-safe generic construction
- Centralize instantiation

## Model Types

All navigation items work with models that implement `IDocumentationFile`:

```csharp
public interface IDocumentationFile : INavigationModel
{
    string NavigationTitle { get; }
}
```

**Built-in Models:**

### CrossLinkModel
```csharp
public record CrossLinkModel(Uri CrossLinkUri, string NavigationTitle)
    : IDocumentationFile;
```

### SiteNavigationNoIndexFile
```csharp
public record SiteNavigationNoIndexFile(string NavigationTitle)
    : IDocumentationFile;
```

**Custom Models:**

You can create custom models for specialized documentation types:

```csharp
public record ApiDocumentationFile(
    string NavigationTitle,
    string ApiVersion,
    ApiType Type
) : IDocumentationFile;

// Use with generic navigation
var navigation = new DocumentationSetNavigation<ApiDocumentationFile>(
    docset,
    context,
    new ApiDocumentationFileFactory()
);
```

## Summary

The navigation system provides:

- **7 node types** - 2 leaves, 3 nodes, 3 roots
- **Generic design** - Works with any `IDocumentationFile` model
- **Flexible structure** - Files, folders, TOCs, virtual files
- **Re-homing** - Roots can change URL prefix in O(1)
- **Scope isolation** - Each root creates its own URL scope
- **Type safety** - Factory methods ensure correct construction

For implementation details, see the source code in:
- `src/Elastic.Documentation.Navigation/Isolated/` - Individual node types
- `src/Elastic.Documentation.Navigation/Assembler/` - Site assembly
