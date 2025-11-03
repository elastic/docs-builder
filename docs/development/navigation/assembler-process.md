# Assembler Process

This document explains how the assembler builds a unified site navigation from multiple documentation repositories.

## Overview

The assembler combines multiple isolated documentation repositories into a single site with:
- Unified navigation structure
- Custom URL prefixes for each repository
- Cross-repository linking
- Phantom node tracking

## The Challenge

Given:
- `elastic-docs` repository (guides, API reference, tutorials)
- `elasticsearch` repository (ES-specific docs)
- `kibana` repository (Kibana-specific docs)
- `logstash` repository (Logstash-specific docs)

Build a site where:
- Each repository maintains its own `docset.yml`
- URLs are organized by product/section (not repository)
- Same TOC can appear in multiple places
- Navigation structure is defined centrally

**Example:**
```
elastic-docs has:
  - /api/
  - /guides/

We want site to have:
  - /elasticsearch/api/    (from elastic-docs://api)
  - /elasticsearch/guides/ (from elastic-docs://guides)
  - /kibana/api/           (from kibana://)
  - /logstash/              (from logstash://)
```

## The Solution: Site Navigation

`config/navigation.yml` defines the site structure:

```yaml
toc:
  - toc: elasticsearch
    children:
      - toc: elastic-docs://api
        path_prefix: elasticsearch/api
      - toc: elastic-docs://guides
        path_prefix: elasticsearch/guides

  - toc: kibana://
    path_prefix: kibana

  - toc: logstash://
    path_prefix: logstash

phantoms:
  - source: plugins://
    # Declared but not included in navigation
```

## Assembler Build Process

### Phase 1: Build Isolated Navigations

```csharp
// For each repository in assembler.yml
foreach (var repo in repositories)
{
    // Load docset configuration
    var docsetYaml = LoadFile($"{repo}/docset.yml");
    var docsetConfig = DocumentationSetFile.LoadAndResolve(
        collector,
        docsetYaml,
        fileSystem.DirectoryInfo.New(repo)
    );

    // Build isolated navigation
    var navigation = new DocumentationSetNavigation<IDocumentationFile>(
        docsetConfig,
        CreateContext(repo),
        GenericDocumentationFileFactory.Instance
    );

    // Store for later assembly
    isolatedNavigations[repo] = navigation;
}
```

**Result:** Each repository has its own navigation tree with URLs relative to `/`.

### Phase 2: Load Site Navigation Configuration

```csharp
// Load navigation.yml
var navigationYaml = LoadFile("config/navigation.yml");
var siteConfig = SiteNavigationFile.LoadAndResolve(
    collector,
    navigationYaml,
    fileSystem
);
```

**`SiteNavigationFile` contains:**
```csharp
public class SiteNavigationFile
{
    public List<SiteTableOfContentsRef> TableOfContents { get; set; }
    public List<PhantomRegistration> Phantoms { get; set; }
}

public class SiteTableOfContentsRef
{
    public Uri Source { get; set; }              // e.g., elastic-docs://api
    public string PathPrefix { get; set; }       // e.g., "elasticsearch/api"
    public List<SiteTableOfContentsRef> Children { get; set; }
}
```

### Phase 3: Assemble Site Navigation

```csharp
// Create site navigation - this does the re-homing!
var siteNavigation = new SiteNavigation(
    siteConfig,
    context,
    isolatedNavigations.Values,  // All isolated navigations
    sitePrefix: null  // Or custom prefix like "/docs"
);
```

**What `SiteNavigation` constructor does:**

```csharp
public SiteNavigation(
    SiteNavigationFile siteNavigationFile,
    IDocumentationContext context,
    IReadOnlyCollection<IDocumentationSetNavigation> documentationSetNavigations,
    string? sitePrefix)
{
    _sitePrefix = NormalizeSitePrefix(sitePrefix);

    // 1. Initialize root properties
    NavigationRoot = this;
    Parent = null;
    Hidden = false;
    Id = ShortId.Create("site");
    Identifier = new Uri("site://");

    // 2. Collect all root nodes (docsets and TOCs) into _nodes
    _nodes = [];
    foreach (var setNavigation in documentationSetNavigations)
    {
        foreach (var (identifier, node) in setNavigation.TableOfContentNodes)
        {
            if (!_nodes.TryAdd(identifier, node))
            {
                context.EmitError(
                    context.ConfigurationPath,
                    $"Duplicate navigation identifier: {identifier}"
                );
            }
        }
    }

    // 3. Build site navigation by re-homing nodes
    var items = new List<INavigationItem>();
    var index = 0;
    foreach (var tocRef in siteNavigationFile.TableOfContents)
    {
        var navItem = CreateSiteTableOfContentsNavigation(
            tocRef,
            index++,
            context,
            parent: this,
            root: null
        );

        if (navItem != null)
            items.Add(navItem);
    }

    // 4. Set index and navigation items
    var indexNavigation = items.QueryIndex<IDocumentationFile>(
        this,
        "/index.md",
        out var navigationItems
    );
    Index = indexNavigation;
    NavigationItems = navigationItems;
}
```

### Phase 4: Re-home Individual Nodes

The magic happens in `CreateSiteTableOfContentsNavigation`:

```csharp
private INavigationItem? CreateSiteTableOfContentsNavigation(
    SiteTableOfContentsRef tocRef,
    int index,
    IDocumentationContext context,
    INodeNavigationItem<INavigationModel, INavigationItem> parent,
    IRootNavigationItem<INavigationModel, INavigationItem>? root)
{
    // 1. Calculate path prefix
    var pathPrefix = tocRef.PathPrefix;
    if (string.IsNullOrWhiteSpace(pathPrefix))
    {
        // Handle narrative repository special case
        if (tocRef.Source.Scheme != NarrativeRepository.RepositoryName)
        {
            context.EmitError(
                context.ConfigurationPath,
                $"path_prefix is required for TOC reference: {tocRef.Source}"
            );
            return null;
        }
    }

    // Normalize path prefix
    pathPrefix = pathPrefix.Trim('/');
    pathPrefix = !string.IsNullOrWhiteSpace(_sitePrefix)
        ? $"{_sitePrefix}/{pathPrefix}"
        : "/" + pathPrefix;

    // 2. Look up the node
    if (!_nodes.TryGetValue(tocRef.Source, out var node))
    {
        context.EmitError(
            context.ConfigurationPath,
            $"Could not find navigation node for identifier: {tocRef.Source}"
        );
        return null;
    }

    if (node is not INavigationHomeAccessor homeAccessor)
    {
        context.EmitError(
            context.ConfigurationPath,
            $"Navigation node does not implement INavigationHomeAccessor: {tocRef.Source}"
        );
        return null;
    }

    root ??= node;

    // 3. RE-HOME THE NODE! ⚡
    node.Parent = parent;
    node.NavigationIndex = index;
    homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, root);

    // 4. Process children (may include re-homing nested nodes)
    var children = new List<INavigationItem>();

    // First, add node's existing children
    INavigationItem[] nodeChildren = [node.Index, .. node.NavigationItems];
    foreach (var nodeChild in nodeChildren)
    {
        nodeChild.Parent = node;
        if (nodeChild is INavigationHomeAccessor childAccessor)
            childAccessor.HomeProvider = homeAccessor.HomeProvider;

        // Don't add root nodes unless explicitly declared
        if (nodeChild is IRootNavigationItem<INavigationModel, INavigationItem>)
            continue;

        children.Add(nodeChild);
    }

    // Then, add any additional children from navigation.yml
    if (tocRef.Children.Count > 0)
    {
        var childIndex = 0;
        foreach (var child in tocRef.Children)
        {
            var childItem = CreateSiteTableOfContentsNavigation(
                child,
                childIndex++,
                context,
                node,
                root
            );
            if (childItem != null)
                children.Add(childItem);
        }
    }

    // 5. Set children on the node
    switch (node)
    {
        case IAssignableChildrenNavigation documentationSetNavigation:
            documentationSetNavigation.SetNavigationItems(children);
            break;
    }

    return node;
}
```

## Detailed Example

### Input: Isolated Navigation

```
elastic-docs repository:
DocumentationSetNavigation (elastic-docs://)
  PathPrefix: ""
  NavigationRoot: self
  Index: /
  NavigationItems:
    - TableOfContentsNavigation (elastic-docs://api)
        PathPrefix: ""
        NavigationRoot: DocumentationSetNavigation
        Index: /api/
        NavigationItems:
          - FileNavigationLeaf (api/rest.md)
              Url: /api/rest/
              NavigationRoot: DocumentationSetNavigation

    - TableOfContentsNavigation (elastic-docs://guides)
        PathPrefix: ""
        NavigationRoot: DocumentationSetNavigation
        Index: /guides/
        NavigationItems:
          - FileNavigationLeaf (guides/getting-started.md)
              Url: /guides/getting-started/
              NavigationRoot: DocumentationSetNavigation
```

### Configuration: navigation.yml

```yaml
toc:
  - toc: elasticsearch
    children:
      - toc: elastic-docs://api
        path_prefix: elasticsearch/api

      - toc: elastic-docs://guides
        path_prefix: elasticsearch/guides
```

### Output: Assembled Site Navigation

```
SiteNavigation (site://)
  PathPrefix: ""
  NavigationRoot: self
  Identifier: site://

  Nodes:
    [elastic-docs://] = DocumentationSetNavigation
    [elastic-docs://api] = TableOfContentsNavigation
    [elastic-docs://guides] = TableOfContentsNavigation

  NavigationItems:
    - VirtualFolderNode ("elasticsearch")
        NavigationItems:

          - TableOfContentsNavigation (elastic-docs://api) RE-HOMED! ⚡
              PathPrefix: "elasticsearch/api"           ← Changed!
              NavigationRoot: SiteNavigation            ← Changed!
              Parent: VirtualFolderNode                 ← Changed!
              HomeProvider: new NavigationHomeProvider(
                  "/elasticsearch/api",
                  SiteNavigation
              )
              Index: /elasticsearch/api/                ← Changed!
              NavigationItems:
                - FileNavigationLeaf (api/rest.md)
                    Url: /elasticsearch/api/rest/       ← Changed!
                    NavigationRoot: SiteNavigation      ← Changed!

          - TableOfContentsNavigation (elastic-docs://guides) RE-HOMED! ⚡
              PathPrefix: "elasticsearch/guides"        ← Changed!
              NavigationRoot: SiteNavigation            ← Changed!
              Parent: VirtualFolderNode                 ← Changed!
              HomeProvider: new NavigationHomeProvider(
                  "/elasticsearch/guides",
                  SiteNavigation
              )
              Index: /elasticsearch/guides/             ← Changed!
              NavigationItems:
                - FileNavigationLeaf (guides/getting-started.md)
                    Url: /elasticsearch/guides/getting-started/ ← Changed!
                    NavigationRoot: SiteNavigation      ← Changed!
```

**Key Changes:**
1. `HomeProvider` replaced → new path prefix and navigation root
2. All URLs automatically updated (lazy calculation)
3. Parent relationships updated
4. NavigationRoot points to SiteNavigation

## Re-homing Performance

**Cost of re-homing a subtree with 10,000 nodes:**
```csharp
// This single line re-homes all 10,000 nodes!
homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, root);
```

**Time complexity:** O(1)
- No tree traversal
- No URL updates
- Just reference assignment

**When URLs are accessed later:**
- First access: O(path depth) calculation
- Subsequent: O(1) from cache
- Cache invalidated automatically (HomeProvider ID changed)

## Phantom Nodes

Phantoms are nodes declared in navigation.yml but not included in the tree:

```yaml
phantoms:
  - source: plugins://
  - source: cloud://monitoring
```

**Purpose:**
- Document intentionally excluded content
- Prevent "undeclared navigation" warnings
- Enable validation of cross-links to phantom content

**Tracking:**
```csharp
// SiteNavigation tracks phantoms
public IReadOnlyCollection<PhantomRegistration> Phantoms { get; }
public HashSet<Uri> DeclaredPhantoms { get; }

// After assembly, check for unseen nodes
foreach (var node in UnseenNodes)
{
    if (!DeclaredPhantoms.Contains(node))
        context.EmitHint(
            context.ConfigurationPath,
            $"Navigation does not explicitly declare: {node} as a phantom"
        );
}
```

## Path Prefix Requirements

In assembler builds, `path_prefix` is **mandatory** (with one exception):

```yaml
toc:
  - toc: elastic-docs://api
    path_prefix: elasticsearch/api  # Required!

  - toc: docs-content://guides
    # path_prefix not required for narrative repository
    # Will default to "guides"
```

**Why?**
- Prevents URL collisions
- Makes routing explicit
- Enables flexible reorganization

**Validation:**
```csharp
if (string.IsNullOrWhiteSpace(pathPrefix))
{
    if (tocRef.Source.Scheme != NarrativeRepository.RepositoryName)
    {
        context.EmitError(
            context.ConfigurationPath,
            $"path_prefix is required for TOC reference: {tocRef.Source}"
        );
    }
}
```

## Nested Re-homing

Assembler builds can have nested structures where nodes are re-homed multiple times:

```yaml
toc:
  - toc: products
    children:
      - toc: elasticsearch
        path_prefix: products/elasticsearch
        children:
          - toc: elastic-docs://api
            path_prefix: products/elasticsearch/api
```

**How it works:**

1. Create virtual `products` node
2. Re-home `elasticsearch` to `/products/elasticsearch`
3. Re-home `elastic-docs://api` to `/products/elasticsearch/api`

Each level creates a new scope with its own HomeProvider.

## Error Handling

The assembler emits errors for common issues:

### Duplicate Identifiers
```csharp
// Two docsets with same identifier
if (!_nodes.TryAdd(identifier, node))
{
    context.EmitError(
        context.ConfigurationPath,
        $"Duplicate navigation identifier: {identifier}"
    );
}
```

### Missing Nodes
```csharp
// Referenced node doesn't exist
if (!_nodes.TryGetValue(tocRef.Source, out var node))
{
    context.EmitError(
        context.ConfigurationPath,
        $"Could not find navigation node for identifier: {tocRef.Source}"
    );
}
```

### Undeclared Nested TOCs
```csharp
// Found a nested TOC that wasn't declared in navigation.yml
if (!DeclaredTableOfContents.Contains(rootChild.Identifier) &&
    !DeclaredPhantoms.Contains(rootChild.Identifier))
{
    context.EmitWarning(
        context.ConfigurationPath,
        $"Navigation does not explicitly declare: {rootChild.Identifier}"
    );
}
```

## Site Prefix

The entire site can have a global prefix:

```csharp
var siteNavigation = new SiteNavigation(
    siteConfig,
    context,
    documentationSetNavigations,
    sitePrefix: "/docs"  // All URLs start with /docs
);
```

**Example:**
```
Without site prefix:
  /elasticsearch/api/rest/

With sitePrefix="/docs":
  /docs/elasticsearch/api/rest/
```

**Normalization:**
```csharp
private static string? NormalizeSitePrefix(string? sitePrefix)
{
    if (string.IsNullOrWhiteSpace(sitePrefix))
        return null;

    var normalized = sitePrefix.Trim();

    // Ensure leading slash
    if (!normalized.StartsWith('/'))
        normalized = "/" + normalized;

    // Remove trailing slash
    normalized = normalized.TrimEnd('/');

    return normalized;
}
```

## Testing Assembler Builds

```csharp
[Fact]
public void AssemblerRehomesNavigationUrls()
{
    // Arrange: Build isolated navigation
    var docset = DocumentationSetFile.LoadAndResolve(
        collector,
        yaml,
        fileSystem.NewDirInfo("/elastic-docs")
    );
    var isolatedNav = new DocumentationSetNavigation<IDocumentationFile>(
        docset,
        isolatedContext,
        factory
    );

    // Assert: URLs in isolated build
    var apiLeaf = FindLeaf(isolatedNav, "api/rest.md");
    Assert.Equal("/api/rest/", apiLeaf.Url);

    // Act: Assemble site
    var siteConfig = new SiteNavigationFile
    {
        TableOfContents = [
            new SiteTableOfContentsRef
            {
                Source = new Uri("elastic-docs://api"),
                PathPrefix = "elasticsearch/api"
            }
        ]
    };

    var siteNav = new SiteNavigation(
        siteConfig,
        assemblerContext,
        [isolatedNav],
        sitePrefix: null
    );

    // Assert: URLs in assembled site
    var apiLeafInSite = FindLeaf(siteNav, "api/rest.md");
    Assert.Equal("/elasticsearch/api/rest/", apiLeafInSite.Url);

    // The same leaf object! Just re-homed!
    Assert.Same(apiLeaf, apiLeafInSite);
}
```

## Summary

The assembler process:

1. **Builds isolated navigations** - Each repository with URLs relative to `/`
2. **Loads site configuration** - `navigation.yml` with path prefixes
3. **Re-homes nodes** - O(1) operation to change URL prefix
4. **Assembles site tree** - Unified navigation with custom structure
5. **Tracks phantoms** - Documents excluded content
6. **Validates** - Catches duplicates, missing nodes, undeclared TOCs

**Key Innovation:** Re-homing via HomeProvider pattern enables:
- O(1) URL prefix changes
- No tree reconstruction
- Lazy URL calculation
- Same node in multiple contexts

This makes it possible to:
- Build repositories independently
- Test in isolation
- Assemble flexibly into unified site
- Reorganize without rebuilding
