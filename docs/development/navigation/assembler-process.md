# Assembler Process

The assembler combines multiple documentation repositories into a unified site with custom URL prefixes.

> **Prerequisites:** Read [Functional Principles #4](functional-principles.md#4.-navigation-roots-can-be-re-homed) and [Home Provider Architecture](home-provider-architecture.md) first to understand re-homing.

## The Challenge

Multiple repositories need to appear as one site:
- `elastic-docs` with `/api/` and `/guides/`
- Assembled site needs `/elasticsearch/api/` and `/elasticsearch/guides/`
- Same content, different URLs, no rebuilding

## The Solution

Four-phase process:

### Phase 1: Build with AssemblerBuild Flag

```csharp
public AssemblerDocumentationSet(
    ILoggerFactory logFactory,
    AssembleContext context,
    Checkout checkout,
    ICrossLinkResolver crossLinkResolver,
    IConfigurationContext configurationContext,
    IReadOnlySet<Exporter> availableExporters)
{
    // For each repository:
    // 1. Load and resolve docset.yml
    var buildContext = new BuildContext(...)
    {
        AssemblerBuild = true  // ← CRITICAL!
    };

    // 2. Build DocumentationSetNavigation with assembler context
    DocumentationSet = new DocumentationSet(buildContext, logFactory, crossLinkResolver);
}
```

**Why `AssemblerBuild = true` matters:**
```csharp
// DocumentationSetNavigation constructor when creating TOCs:
var assemblerBuild = context.AssemblerBuild;

var tocHomeProvider = assemblerBuild
    ? new NavigationHomeProvider(...)  // Create NEW scope
    : parentHomeProvider;              // Inherit parent's scope

// Result: Each TOC gets its own HomeProvider instance
```

**Without this flag:**
```
DocumentationSetNavigation
  └─ TableOfContentsNavigation (api)
      HomeProvider: INHERITED ← shares parent's provider
```
Can't re-home independently.

**With this flag:**
```
DocumentationSetNavigation
  └─ TableOfContentsNavigation (api)
      HomeProvider: NEW INSTANCE ← own provider!
```
Can re-home independently!

### Phase 2: Load navigation.yml

```yaml
toc:
  - toc: elastic-docs://api
    path_prefix: elasticsearch/api
  - toc: elastic-docs://guides
    path_prefix: elasticsearch/guides
```

Defines where each TOC appears in the site.

### Phase 3: Create SiteNavigation

```csharp
public SiteNavigation(
    SiteNavigationFile siteNavigationFile,
    IDocumentationContext context,
    IReadOnlyCollection<IDocumentationSetNavigation> documentationSetNavigations,
    string? sitePrefix)
{
    // 1. Initialize SiteNavigation as root
    NavigationRoot = this;

    // 2. Collect all docset/TOC nodes into dictionary
    foreach (var setNavigation in documentationSetNavigations)
    {
        foreach (var (identifier, node) in setNavigation.TableOfContentNodes)
            _nodes.TryAdd(identifier, node);
    }

    // 3. Process each navigation.yml reference
    foreach (var tocRef in siteNavigationFile.TableOfContents)
    {
        var navItem = CreateSiteTableOfContentsNavigation(tocRef, index++, context, this, null);
        if (navItem != null)
            items.Add(navItem);
    }
}
```

### Phase 4: Re-home Each Reference

For each entry in `navigation.yml`:

```csharp
private INavigationItem? CreateSiteTableOfContentsNavigation(
    SiteTableOfContentsRef tocRef,
    int index,
    IDocumentationContext context,
    INodeNavigationItem<INavigationModel, INavigationItem> parent,
    IRootNavigationItem<INavigationModel, INavigationItem>? root)
{
    // 1. Calculate final path_prefix
    // 2. Look up node by identifier (elastic-docs://api)
    // 3. Replace node's HomeProvider ← THE MAGIC! ⚡
    // 4. Update parent/index
    // 5. Process children (skip nested root nodes)
}
```

**The critical line:**
```csharp
private INavigationItem? CreateSiteTableOfContentsNavigation(...)
{
    // ...
    homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, siteRoot);
}
```

This single assignment updates all descendant URLs instantly (O(1)).

## How It Works: Example

**Input: Built with AssemblerBuild = true**
```
elastic-docs://
  HomeProvider: self
  └─ elastic-docs://api
      HomeProvider: Instance A (PathPrefix = "")
      └─ api/rest.md → URL: /api/rest/
  └─ elastic-docs://guides
      HomeProvider: Instance B (PathPrefix = "")
      └─ guides/start.md → URL: /guides/start/
```

**navigation.yml:**
```yaml
- toc: elastic-docs://api
  path_prefix: elasticsearch/api
- toc: elastic-docs://guides
  path_prefix: elasticsearch/guides
```

**After Re-homing:**
```
SiteNavigation
  └─ elastic-docs://api
      HomeProvider: NEW (PathPrefix = "elasticsearch/api") ← Replaced!
      └─ api/rest.md → URL: /elasticsearch/api/rest/ ✓
  └─ elastic-docs://guides
      HomeProvider: NEW (PathPrefix = "elasticsearch/guides") ← Replaced!
      └─ guides/start.md → URL: /elasticsearch/guides/start/ ✓
```

## Why Separate Scopes Matter

**Scenario:** Split a docset across the site.

```yaml
# elastic-docs has both api/ and guides/ TOCs
toc:
  - toc: elastic-docs://api
    path_prefix: reference/api       # Goes here
  - toc: elastic-docs://guides
    path_prefix: learn/guides        # Goes there
```

If TOCs shared their parent's provider, both would get the same prefix. Separate providers enable different prefixes from the same repository.

## Key Architecture Points

**1. AssemblerBuild Flag Controls Scope Creation**
- True: TOCs create own HomeProvider
- False: TOCs inherit parent's HomeProvider

**2. HomeProvider is the Re-homing Mechanism**
- URLs calculated from `HomeProvider.PathPrefix`
- Changing provider changes all descendant URLs
- No tree traversal needed

**3. Root Nodes Can Be Re-homed**
- `DocumentationSetNavigation` - Entire docset
- `TableOfContentsNavigation` - Individual TOC
- Must have own provider (not inherited)

**4. Non-Root Nodes Inherit**
- `FileNavigationLeaf`, `FolderNavigation`, etc.
- Use parent's HomeProvider
- Re-home automatically when parent re-homed

## Path Prefix Requirements

```yaml
# Required
- toc: elastic-docs://api
  path_prefix: elasticsearch/api  # Must be unique!

# Exception: narrative repository
- toc: docs-content://guides
  # path_prefix defaults to "guides"
```

All `path_prefix` values must be unique across the site.

## Phantom Nodes

Declared but not included:

```yaml
phantoms:
  - source: plugins://
```

Prevents "undeclared navigation" warnings for excluded content.

## The Re-homing Flow

```
1. Build with AssemblerBuild = true
   → TOCs get own HomeProvider

2. Collect all nodes into dictionary
   → Indexed by identifier (elastic-docs://api)

3. For each navigation.yml entry:
   → Look up node
   → Replace HomeProvider ← O(1) operation
   → All URLs update automatically

4. Result: Unified site with custom structure
```

## What Makes This Fast

**O(1) Re-homing:**
```csharp
// This updates 10,000 URLs instantly:
node.HomeProvider = new NavigationHomeProvider(newPrefix, newRoot);
```

**Why?**
- URLs calculated on-demand from HomeProvider
- Not stored in nodes
- Changing provider = all URLs recalculate next access
- Smart caching invalidates on provider change

## Common Patterns

**Pattern 1: Keep docset together**
```yaml
- toc: elastic-docs://
  path_prefix: elasticsearch
```

**Pattern 2: Split docset apart**
```yaml
- toc: elastic-docs://api
  path_prefix: reference/api
- toc: elastic-docs://guides
  path_prefix: learn/guides
```

**Pattern 3: Nest docsets**
```yaml
- toc: products
  children:
    - toc: elasticsearch://
      path_prefix: products/elasticsearch
    - toc: kibana://
      path_prefix: products/kibana
```

## Summary

**The assembler enables:**
- Build repositories independently (isolated)
- Combine into unified site (assembled)
- Custom URL structure per site
- Split single docset across multiple sections
- O(1) re-homing (no tree reconstruction)

**The critical piece:**
`AssemblerBuild = true` causes `TableOfContentsNavigation` to create own `HomeProvider`, enabling independent re-homing of TOCs within a docset.

Without this, you can only re-home entire docsets. With it, you can split a docset anywhere.
