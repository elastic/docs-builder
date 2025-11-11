# Technical Principles

These principles define how the navigation system is implemented.

> **Prerequisites:** Read [Functional Principles](functional-principles.md) first to understand what the system does and why.

## 1. Generic Type System for Covariance

Navigation classes are generic over `TModel`:
```csharp
public class DocumentationSetNavigation<TModel>
    where TModel : class, IDocumentationFile
```

**Why Generic?**

**Covariance Enables Static Typing:**
```csharp
// Without covariance: always get base interface, requires runtime casts
INodeNavigationItem<INavigationModel, INavigationItem> node = GetNode();
if (node.Model is MarkdownFile markdown)  // Runtime check required
{
    var content = markdown.Content;
}

// With covariance: query for specific type statically
INodeNavigationItem<MarkdownFile, INavigationItem> node = QueryForMarkdownNodes();
var content = node.Model.Content;  // ✓ No cast needed! Static type safety
```

**Benefits:**
- **Type Safety**: Query methods can return specific types like `INodeNavigationItem<MarkdownFile, INavigationItem>`
- **No Runtime Casts**: Access `.Model.Content` directly without casting
- **Compile-Time Errors**: Type mismatches caught during compilation, not runtime
- **Better IntelliSense**: IDEs show correct members for specific model types
- **Flexibility**: Same navigation code works with different file models (MarkdownFile, ApiDocFile, etc.)

**Example:**
```csharp
// Query for nodes with specific model type
var markdownNodes = navigation.NavigationItems
    .OfType<INodeNavigationItem<MarkdownFile, INavigationItem>>();

foreach (var node in markdownNodes)
{
    // No cast needed! Static typing
    Console.WriteLine(node.Model.FrontMatter);
    Console.WriteLine(node.Model.Content);
}
```

## 2. Provider Pattern for URL Context

`INavigationHomeProvider` / `INavigationHomeAccessor`:
- **Providers** define context (PathPrefix, NavigationRoot)
- **Accessors** reference providers
- Decouples URL calculation from tree structure
- Enables context switching (re-homing)

**Why This Enables Re-homing:**
```csharp
// Isolated build
node.HomeProvider = new NavigationHomeProvider("", docsetRoot);
// URLs: /api/rest/

// Assembler build - O(1) operation!
node.HomeProvider = new NavigationHomeProvider("/guide", siteRoot);
// URLs: /guide/api/rest/
```

Single reference change updates all descendant URLs.

> See [Home Provider Architecture](home-provider-architecture.md) for complete explanation.

## 3. Lazy URL Calculation with Caching

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
- O(1) for repeated access, O(depth) for calculation

**Why HomeProvider.Id?**
- Each HomeProvider has a unique ID
- Comparing IDs is cheaper than deep equality checks
- ID changes when provider is replaced during re-homing
- Automatic cache invalidation without explicit cache clearing

---

## Performance Characteristics

- **Tree Construction**: O(n) where n = number of files
- **URL Calculation**: O(depth) for first access, O(1) with caching
- **Re-homing**: O(1) - just replace HomeProvider reference
- **Tree Traversal**: O(n) for full tree, but rarely needed
- **Memory**: O(n) for nodes, URLs computed on-demand

**Why Re-homing is O(1):**
1. Replace single HomeProvider reference
2. No tree traversal required
3. URLs lazy-calculated on next access
4. Cache invalidation via ID comparison
5. All descendants automatically use new provider
