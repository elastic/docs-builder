# Home Provider Architecture

The Home Provider pattern enables O(1) re-homing of navigation subtrees through indirection.

> **Overview:** For high-level concepts, see [Functional Principles #3-5](functional-principles.md#3.-url-building-is-dynamic-and-cheap). This document explains the implementation.

## The Problem

When building assembled documentation sites, we need to:
1. Build navigation for individual repositories in isolation
2. Combine them into a single site with custom URL prefixes
3. Update all URLs in a subtree efficiently

**Naive approach:**
```csharp
// Traverse entire subtree to update URLs
void UpdateUrlPrefix(INavigationItem root, string newPrefix)
{
    // O(n) - visit every node
    foreach (var item in TraverseTree(root))
    {
        item.UrlPrefix = newPrefix;
    }
}
```

**Issues:**
- O(n) traversal for every prefix change
- URL prefix stored at every node
- URLs calculated at construction time
- Changes require tree reconstruction

## The Solution: Provider Pattern

Instead of storing URL information at each node, use indirection through a provider:

```csharp
// The provider defines the URL context for a scope
public interface INavigationHomeProvider
{
    string PathPrefix { get; }
    IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }
    string Id { get; } // For cache invalidation
}

// Nodes access their provider through an accessor
public interface INavigationHomeAccessor
{
    INavigationHomeProvider HomeProvider { get; set; }
}
```

Nodes reference a provider instead of storing URL information:

```csharp
public class FileNavigationLeaf<TModel>
{
    private readonly INavigationHomeAccessor _homeAccessor;

    public string Url
    {
        get
        {
            // Calculate from current provider
            var prefix = _homeAccessor.HomeProvider.PathPrefix;
            return $"{prefix}/{_relativePath}/";
        }
    }
}
```

Re-homing becomes a single assignment:

```csharp
// Change the provider → all descendants use new prefix
docsetNavigation.HomeProvider = new NavigationHomeProvider("/guide", siteNav);
```

## How It Works

### 1. Scope Creation

Navigation types that can be re-homed implement `INavigationHomeProvider`:

```csharp
public class DocumentationSetNavigation<TModel>
    : INavigationHomeProvider, INavigationHomeAccessor
{
    private string _pathPrefix;

    // Provider properties
    public string PathPrefix => HomeProvider == this
        ? _pathPrefix
        : HomeProvider.PathPrefix;

    public IRootNavigationItem<...> NavigationRoot =>
        HomeProvider == this
            ? this
            : HomeProvider.NavigationRoot;

    // Accessor property
    public INavigationHomeProvider HomeProvider { get; set; }

    // Initially self-referential
    public DocumentationSetNavigation(...)
    {
        _pathPrefix = pathPrefix ?? "";
        HomeProvider = this;
    }
}
```

### 2. Scope Inheritance

Child nodes receive their parent's accessor:

```csharp
// Creating a child node
var fileNav = new FileNavigationLeaf<TModel>(
    model,
    fileInfo,
    new FileNavigationArgs(
        path,
        relativePath,
        hidden,
        index,
        parent,
        homeAccessor: this // Pass down the accessor
    )
);
```

### 3. URL Calculation

Leaf nodes use the accessor to calculate URLs:

```csharp
public class FileNavigationLeaf<TModel>
{
    private readonly FileNavigationArgs _args;

    public string Url
    {
        get
        {
            // Get prefix from current provider
            var rootUrl = _args.HomeAccessor.HomeProvider.PathPrefix.TrimEnd('/');

            // Determine path based on context
            var relativeToContainer =
                _args.HomeAccessor.HomeProvider.NavigationRoot.Parent is SiteNavigation;

            var relativePath = relativeToContainer
                ? _args.RelativePathToTableOfContents
                : _args.RelativePathToDocumentationSet;

            return BuildUrl(rootUrl, relativePath);
        }
    }
}
```

### 4. Re-homing

In assembler builds, `SiteNavigation` replaces the provider:

```csharp
// CreateSiteTableOfContentsNavigation(...):
// Calculate new path prefix for this subtree
var pathPrefix = $"{_sitePrefix}/{tocRef.PathPrefix}".Trim('/');

// Create new provider with custom prefix
var newProvider = new NavigationHomeProvider(pathPrefix, root);

// Replace provider - this is the magic! ⚡
homeAccessor.HomeProvider = newProvider;

// All descendants now use the new prefix
```

**What happens:**
1. `homeAccessor.HomeProvider` is assigned a new provider
2. Provider has `PathPrefix = "/guide"` and `NavigationRoot = SiteNavigation`
3. Every URL calculation in that subtree now uses the "/guide" prefix
4. No tree traversal needed

## Example: Isolated to Assembled

### Isolated Build

```
DocumentationSetNavigation (elastic-docs)
├─ HomeProvider: self
├─ PathPrefix: ""
├─ NavigationRoot: self
│
└─ TableOfContentsNavigation (api/)
   ├─ HomeProvider: inherited from parent = DocumentationSetNavigation
   ├─ PathPrefix: "" (from provider)
   ├─ NavigationRoot: DocumentationSetNavigation (from provider)
   │
   └─ FileNavigationLeaf (api/rest.md)
      ├─ HomeAccessor.HomeProvider: DocumentationSetNavigation
      └─ URL calculation:
         prefix = HomeProvider.PathPrefix = ""
         path = "api/rest.md"
         url = "/api/rest/"
```

### Assembler Build - After Re-homing

```
SiteNavigation
├─ HomeProvider: self
├─ PathPrefix: ""
├─ NavigationRoot: self
│
└─ DocumentationSetNavigation (elastic-docs)
   ├─ HomeProvider: NEW NavigationHomeProvider("/guide", SiteNavigation) ⚡
   ├─ PathPrefix: "/guide" (from new provider)
   ├─ NavigationRoot: SiteNavigation (from new provider)
   │
   └─ TableOfContentsNavigation (api/)
      ├─ HomeProvider: inherited = new provider ⚡
      ├─ PathPrefix: "/guide" (from new provider)
      ├─ NavigationRoot: SiteNavigation (from new provider)
      │
      └─ FileNavigationLeaf (api/rest.md)
         ├─ HomeAccessor.HomeProvider: new provider ⚡
         └─ URL calculation:
            prefix = HomeProvider.PathPrefix = "/guide"
            path = "api/rest.md"
            url = "/guide/api/rest/" ✨
```

**The re-homing happened at lines marked with ⚡ - a single assignment!**

## Key Characteristics

### O(1) Re-homing

```csharp
// This updates ALL URLs in the subtree - regardless of size!
node.HomeProvider = new NavigationHomeProvider("/new-prefix", newRoot);
```

**Time complexity: O(1)**

This isn't marketing - it's a fact. Whether the subtree has 10 nodes or 10,000 nodes, re-homing takes the same amount of time because it's a single reference assignment.

**Compare to naive approach:**
- Naive: O(n) - must visit every node
- Provider: O(1) - single assignment

### Lazy Evaluation

URLs calculated on-demand:
- Not calculated until accessed
- Always reflects current provider state
- Memory efficient - no stored URL strings

### Smart Caching

```csharp
private string? _homeProviderCache;
private string? _urlCache;

public string Url
{
    get
    {
        // Check if provider changed
        if (_homeProviderCache != null &&
            _homeProviderCache == _args.HomeAccessor.HomeProvider.Id &&
            _urlCache != null)
        {
            return _urlCache;
        }

        // Recalculate and cache
        _homeProviderCache = _args.HomeAccessor.HomeProvider.Id;
        _urlCache = DetermineUrl();
        return _urlCache;
    }
}
```

Caching strategy:
- First access: O(depth) calculation
- Subsequent accesses: O(1) cache lookup
- Cache invalidates automatically when provider changes (via Id comparison)

### Scope Isolation

Each provider creates an isolated scope:
- Changes to one scope don't affect others
- Clear ownership of URL context
- Enables independent re-homing of subtrees

## Implementation Details

### Provider Identity

Each provider has a unique ID for cache invalidation:

```csharp
public class NavigationHomeProvider : INavigationHomeProvider
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
}
```

When a provider changes, the ID changes, invalidating cached URLs.

### Accessor vs Provider

**Provider:** Nodes that create scopes (`DocumentationSetNavigation`, `TableOfContentsNavigation`)

**Accessor:** All nodes that need to calculate URLs

Some nodes implement both:
```csharp
public class DocumentationSetNavigation<TModel>
    : INavigationHomeProvider, INavigationHomeAccessor
{
    // Can be a provider AND access a different provider
}
```

This dual implementation is what enables re-homing.

### Passing Accessors Down

During construction, accessors flow down the tree:

```csharp
// Parent creates child, passes its accessor
var childNav = ConvertToNavigationItem(
    tocItem,
    index,
    context,
    parent: this,
    homeAccessor: this // Pass down accessor
);
```

Children inherit their parent's accessor, creating a reference chain back to the scope provider.

### Assembler-Specific Provider Behavior

In assembler builds, TOCs create isolated providers:

```csharp
var assemblerBuild = context.AssemblerBuild;

var isolatedHomeProvider = assemblerBuild
    ? new NavigationHomeProvider(
        homeAccessor.HomeProvider.PathPrefix,
        homeAccessor.HomeProvider.NavigationRoot
      )
    : homeAccessor.HomeProvider;
```

This ensures TOCs can be re-homed independently during site assembly.

> See [Assembler Process](assembler-process.md) for details on how this flag controls scope creation.

## Performance Analysis

### Memory Usage

**Per Node:**
- Provider: ~48 bytes (string, reference, guid)
- Accessor: 8 bytes (reference)
- Cache: ~32 bytes (2 strings) - leaf nodes only

**For 10,000 nodes:**
- Without caching: ~560 KB
- With cached URLs: ~880 KB
- Naive approach (stored URLs): ~1.5 MB+

### CPU Usage

**URL Calculation:**
- Cache hit: O(1) - pointer dereference + string return
- Cache miss: O(depth) - string concatenation + path processing
- Re-homing: O(1) - reference assignment

**Access Pattern:**
- First access: Calculate and cache
- Subsequent: Return cached value
- After re-homing: Recalculate on next access

### Scalability

Re-homing time is constant regardless of subtree size:

| Subtree Size | Re-homing Time |
|--------------|----------------|
| 100 nodes | O(1) |
| 10,000 nodes | O(1) |
| 1,000,000 nodes | O(1) |

This is O(1) because re-homing is a single reference assignment, regardless of how many nodes reference that provider.

## Common Patterns

### Pattern 1: Creating a Scope

```csharp
public class MyNavigation : INavigationHomeProvider, INavigationHomeAccessor
{
    private string _pathPrefix;

    public MyNavigation(string pathPrefix)
    {
        _pathPrefix = pathPrefix;
        HomeProvider = this; // Self-referential initially
    }

    public string PathPrefix => HomeProvider == this ? _pathPrefix : HomeProvider.PathPrefix;
    public IRootNavigationItem<...> NavigationRoot => /* ... */;
    public INavigationHomeProvider HomeProvider { get; set; }
}
```

### Pattern 2: Consuming a Scope

```csharp
public class MyLeaf
{
    private readonly INavigationHomeAccessor _homeAccessor;

    public MyLeaf(INavigationHomeAccessor homeAccessor)
    {
        _homeAccessor = homeAccessor;
    }

    public string Url =>
        $"{_homeAccessor.HomeProvider.PathPrefix}/{_path}/";
}
```

### Pattern 3: Re-homing

```csharp
void RehomeSubtree(
    INavigationHomeAccessor subtree,
    string newPrefix,
    IRootNavigationItem<...> newRoot)
{
    subtree.HomeProvider = new NavigationHomeProvider(newPrefix, newRoot);
    // ✅ All URLs updated
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public void RehomingUpdatesUrlsDynamically()
{
    // Create isolated navigation
    var docset = new DocumentationSetNavigation<IDocumentationFile>(...);
    var leaf = docset.NavigationItems.First() as FileNavigationLeaf<IDocumentationFile>;

    // Initial URL
    Assert.Equal("/api/rest/", leaf.Url);

    // Re-home the docset
    docset.HomeProvider = new NavigationHomeProvider("/guide", siteNav);

    // URL updated ✨
    Assert.Equal("/guide/api/rest/", leaf.Url);
}
```

## Summary

The Home Provider pattern provides:

✅ **O(1) re-homing** - Single reference assignment updates entire subtree
✅ **Lazy URL evaluation** - URLs calculated on-demand
✅ **Automatic cache invalidation** - Via provider ID comparison
✅ **Memory efficiency** - No stored URL strings
✅ **Scope isolation** - Changes don't leak between scopes

This enables building isolated documentation repositories and efficiently assembling them into a unified site with custom URL prefixes. The O(1) re-homing is what makes the assembler build practical - without it, combining large documentation sites would require expensive tree traversal for every URL prefix change.
