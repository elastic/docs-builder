# Navigation Documentation

Welcome to the documentation for `Elastic.Documentation.Navigation`, the library that powers documentation navigation for Elastic's documentation sites.

## What This Is

This library builds hierarchical navigation trees for documentation sites with a unique capability: navigation built for isolated repositories can be **efficiently re-homed** during site assembly without rebuilding the entire tree.

**Why does this matter?**

Individual documentation teams can build and test their docs in isolation with URLs like `/api/overview/`, then those same docs can be assembled into a unified site with URLs like `/elasticsearch/api/overview/` - with **zero tree reconstruction**. It's an O(1) operation.

## Documentation Map

Start with any document based on what you want to learn:

### 🎯 [navigation.md](navigation.md) - Start Here
**Overview of the navigation system**

Read this first to understand:
- The two build modes (isolated vs assembler)
- Core concepts at a high level
- Quick introduction to re-homing
- Links to detailed documentation

### 🎨 [visual-walkthrough.md](visual-walkthrough.md) - See It In Action
**Visual tour with diagrams showing navigation structures**

Read this to understand:
- What different node types look like in the tree
- How isolated builds differ from assembler builds visually
- How the same content appears with different URLs
- How to split and reorganize documentation across sites
- Common patterns for multi-repository organization
- Includes actual tree diagrams from this repository

### 🧭 [first-principles.md](first-principles.md) - Design Philosophy
**Core principles that guide the architecture**

Read this to understand:
- Why two-phase loading (configuration → navigation)
- Why URLs are calculated dynamically, not stored
- Why navigation roots can be re-homed
- Design patterns used (factory, provider, visitor)
- Performance characteristics and invariants

### 🔄 [two-phase-loading.md](two-phase-loading.md) - The Loading Process
**Deep dive into Phase 1 (configuration) and Phase 2 (navigation)**

Read this to understand:
- What happens in Phase 1: Configuration resolution
- What happens in Phase 2: Navigation construction
- Why these phases are separate
- Data flow diagrams
- How to test each phase independently

### 🏠 [home-provider-architecture.md](home-provider-architecture.md) - The Re-homing Magic
**How O(1) re-homing works**

Read this to understand:
- The problem: naive re-homing requires O(n) tree traversal
- The solution: HomeProvider pattern with indirection
- How `INavigationHomeProvider` and `INavigationHomeAccessor` work
- Why URLs are lazily calculated and cached
- Detailed examples of re-homing in action
- Performance analysis

**This is the most important technical concept in the system.**

### 📦 [node-types.md](node-types.md) - Node Type Reference
**Complete reference for every navigation node type**

Read this to understand:
- All 7 node types in detail:
  - **Leaves**: FileNavigationLeaf, CrossLinkNavigationLeaf
  - **Nodes**: FolderNavigation, VirtualFileNavigation
  - **Roots**: DocumentationSetNavigation, TableOfContentsNavigation, SiteNavigation
- Constructor signatures
- URL calculation for each type
- Factory methods
- Model types (IDocumentationFile)

### 🔨 [assembler-process.md](assembler-process.md) - Building Unified Sites
**How multiple repositories become one site**

Read this to understand:
- The assembler build process step-by-step
- How `SiteNavigation` works
- Re-homing in practice during assembly
- Path prefix requirements
- Phantom nodes
- Nested re-homing
- Error handling

## Suggested Reading Order

**If you're new to the codebase:**
1. [navigation.md](navigation.md) - Get the overview
2. [visual-walkthrough.md](visual-walkthrough.md) - See it visually
3. [first-principles.md](first-principles.md) - Understand the why
4. [home-provider-architecture.md](home-provider-architecture.md) - Understand the how
5. [node-types.md](node-types.md) - Reference as needed

**If you're debugging an issue:**
1. [node-types.md](node-types.md) - Find the node type
2. [home-provider-architecture.md](home-provider-architecture.md) - Understand URL calculation
3. [two-phase-loading.md](two-phase-loading.md) - Check which phase

**If you're adding a feature:**
1. [first-principles.md](first-principles.md) - Ensure design consistency
2. [node-types.md](node-types.md) - See existing patterns
3. [two-phase-loading.md](two-phase-loading.md) - Determine which phase
4. [assembler-process.md](assembler-process.md) - Consider assembler impact

**If you're optimizing performance:**
1. [home-provider-architecture.md](home-provider-architecture.md) - Understand caching
2. [first-principles.md](first-principles.md) - See performance characteristics
3. [two-phase-loading.md](two-phase-loading.md) - Find expensive operations

## Key Concepts Summary

### Two Build Modes

1. **Isolated Build**
   - Single repository
   - URLs relative to `/`
   - `DocumentationSetNavigation` is the root
   - Fast iteration for doc teams

2. **Assembler Build**
   - Multiple repositories
   - Custom URL prefixes
   - `SiteNavigation` is the root
   - Docsets/TOCs are re-homed

### Two-Phase Loading

1. **Phase 1: Configuration** (`Elastic.Documentation.Configuration`)
   - Parse YAML files
   - Resolve all relative paths to absolute paths from docset root
   - Validate structure and file references
   - Load nested `toc.yml` files
   - Output: Fully resolved configuration

2. **Phase 2: Navigation** (`Elastic.Documentation.Navigation`)
   - Build tree from resolved configuration
   - Establish parent-child relationships
   - Set up home providers
   - Calculate navigation indexes
   - Output: Complete navigation tree

### Home Provider Pattern

The secret to O(1) re-homing:

```csharp
// Provider defines URL context
public interface INavigationHomeProvider
{
    string PathPrefix { get; }
    IRootNavigationItem<...> NavigationRoot { get; }
}

// Accessor references provider
public interface INavigationHomeAccessor
{
    INavigationHomeProvider HomeProvider { get; set; }
}

// Nodes calculate URLs from current provider
public string Url =>
    $"{_homeAccessor.HomeProvider.PathPrefix}/{_relativePath}/";
```

**Re-homing:**
```csharp
// Change provider → all URLs update instantly!
node.HomeProvider = new NavigationHomeProvider("/new-prefix", newRoot);
```

### Node Types

7 types organized by capabilities:

**Leaves** (no children):
- `FileNavigationLeaf<TModel>` - Markdown file
- `CrossLinkNavigationLeaf` - External link

**Nodes** (have children):
- `FolderNavigation<TModel>` - Directory
- `VirtualFileNavigation<TModel>` - File with YAML-defined children

**Roots** (can be re-homed):
- `DocumentationSetNavigation<TModel>` - Docset root
- `TableOfContentsNavigation<TModel>` - Nested TOC
- `SiteNavigation` - Assembled site root

## Code Organization

The library is organized into:

### `Elastic.Documentation.Navigation/`
Root namespace - shared types:
- `IDocumentationFile.cs` - Base interface for documentation files
- `NavigationModels.cs` - Common model types (CrossLinkModel, SiteNavigationNoIndexFile)

### `Elastic.Documentation.Navigation/Isolated/`
Isolated build navigation:
- `DocumentationSetNavigation.cs` - Docset root
- `TableOfContentsNavigation.cs` - Nested TOC
- `FolderNavigation.cs` - Folder nodes
- `FileNavigationLeaf.cs` - File leaves
- `VirtualFileNavigation.cs` - Virtual file nodes
- `CrossLinkNavigationLeaf.cs` - Crosslink leaves
- `DocumentationNavigationFactory.cs` - Factory for creating nodes
- `NavigationArguments.cs` - Constructor argument records
- `NavigationHomeProvider.cs` - Home provider implementation

### `Elastic.Documentation.Navigation/Assembler/`
Assembler build navigation:
- `SiteNavigation.cs` - Unified site root

### Supporting Files
- `README.md` - High-level overview (in src/)
- `url-building.md` - URL building rules (in src/)

## Testing

Tests are in `tests/Navigation.Tests/`:

**Isolated build tests:**
- `Isolation/ConstructorTests.cs` - Basic navigation construction
- `Isolation/FileNavigationTests.cs` - File leaf behavior
- `Isolation/FolderIndexFileRefTests.cs` - Folder navigation
- `Isolation/PhysicalDocsetTests.cs` - Real docset loading

**Assembler build tests:**
- `Assembler/SiteNavigationTests.cs` - Site assembly
- `Assembler/SiteDocumentationSetsTests.cs` - Multiple docsets
- `Assembler/ComplexSiteNavigationTests.cs` - Complex scenarios

**Test pattern:**
```csharp
[Fact]
public void FeatureUnderTest_Scenario_ExpectedBehavior()
{
    // Arrange: Create mock file system and configuration
    var fileSystem = new MockFileSystem();
    var config = CreateConfig(...);

    // Act: Build navigation
    var nav = new DocumentationSetNavigation<IDocumentationFile>(...);

    // Assert: Verify behavior
    Assert.Equal("/expected/url/", nav.Index.Url);
}
```

## Common Tasks

### Adding a New Node Type

1. Create class in `Isolated/` namespace
2. Implement appropriate interface (`ILeafNavigationItem` or `INodeNavigationItem`)
3. Add factory method if needed
4. Update `ConvertToNavigationItem` in `DocumentationSetNavigation`
5. Add tests in `Isolation/`
6. Update [node-types.md](node-types.md)

### Changing URL Calculation

1. Review [first-principles.md](first-principles.md) - ensure consistency
2. Update `FileNavigationLeaf.Url` property
3. Consider cache invalidation
4. Update tests
5. Update [home-provider-architecture.md](home-provider-architecture.md)

### Modifying Configuration

1. Update classes in `Elastic.Documentation.Configuration`
2. Update `LoadAndResolve` methods
3. Update Phase 2 consumption in navigation classes
4. Update tests for both phases
5. Update [two-phase-loading.md](two-phase-loading.md)

### Debugging Re-homing Issues

1. Check `HomeProvider` assignments in [assembler-process.md](assembler-process.md)
2. Verify `PathPrefix` values
3. Check `NavigationRoot` points to correct root
4. Look for cache issues (HomeProvider ID changed?)
5. Review [home-provider-architecture.md](home-provider-architecture.md)

## Related Documentation

- `Elastic.Documentation.Configuration` - Phase 1 (configuration resolution)
- `Elastic.Documentation.Links` - Cross-link resolution
- `Elastic.Markdown` - Markdown processing

## Source Reference

For the actual implementation, see:
- Library: `src/Elastic.Documentation.Navigation/`
- Tests: `tests/Navigation.Tests/`
- Configuration: `src/Elastic.Documentation.Configuration/`

## Contributing

When making changes:

1. **Maintain invariants** from [first-principles.md](first-principles.md)
2. **Keep phases separate** - don't mix configuration and navigation
3. **Preserve O(1) re-homing** - don't add tree traversals
4. **Add tests** for both isolated and assembler scenarios
5. **Update documentation** in this directory
6. **Run all 111+ tests** - they should all pass

## Questions?

- **"How do URLs get calculated?"** → [home-provider-architecture.md](home-provider-architecture.md)
- **"Why two phases?"** → [two-phase-loading.md](two-phase-loading.md)
- **"What is re-homing?"** → [navigation.md](navigation.md) then [home-provider-architecture.md](home-provider-architecture.md)
- **"Which node type do I need?"** → [node-types.md](node-types.md)
- **"How does the assembler work?"** → [assembler-process.md](assembler-process.md)
- **"What are the design principles?"** → [first-principles.md](first-principles.md)

---

**Welcome to Elastic.Documentation.Navigation!**

The library that makes it possible to build documentation in isolation and efficiently assemble it into unified sites with custom URL structures - no rebuilding required. 🚀
