# Two-Phase Loading

Navigation construction splits into two distinct phases: configuration resolution and navigation building.

> **Overview:** For a high-level understanding, see [Functional Principles #1](functional-principles.md#1.-two-phase-loading). This document provides detailed implementation information.

## Why Two Phases?

Building navigation requires two fundamentally different operations:
1. **Loading configuration** - Parse YAML, check files exist, resolve paths
2. **Building structure** - Create tree, set relationships, calculate URLs

These operations have different concerns:

| Aspect | Configuration | Navigation |
|--------|--------------|------------|
| **Input** | File system + YAML | Resolved paths |
| **Validation** | Files exist, YAML valid | Tree structure valid |
| **Errors** | Missing files, bad YAML | Empty TOCs, broken links |
| **Changes when** | YAML format changes | Tree logic changes |
| **Testing needs** | Mock file system | Mock config objects |

Mixing them creates coupling. Configuration parsing shouldn't know about tree structure. Tree building shouldn't touch the file system.

**Concrete benefits:**

**Error messages are clearer:**
```
Phase 1: File 'api/missing.md' not found at /docs/api/missing.md
Phase 2: Folder 'setup' has children defined but none could be created
```
You immediately know which layer failed.

**Testing is simpler:**
```csharp
// Phase 1 test: Does path resolution work?
[Fact] void ResolvesNestedPaths() { /* mock file system */ }

// Phase 2 test: Does tree structure work?
[Fact] void CreatesNavigationTree() { /* mock config, no files */ }
```
Each phase tests one thing.

**Configuration reuses:**
```csharp
// Parse once
var config = DocumentationSetFile.LoadAndResolve(yaml, fileSystem);

// Build multiple ways
var isolated = new DocumentationSetNavigation(config, isolatedContext, factory);
var assembled = new SiteNavigation(siteConfig, context, [isolated], prefix);
```
Same configuration, different navigation structures.

**Separation of concerns:**
- Change YAML format → only Phase 1 changes
- Change URL calculation → only Phase 2 changes
- Swap YAML for JSON → only Phase 1 changes
- Add new node type → only Phase 2 changes

## Phase 1: Configuration Resolution

**Package:** `Elastic.Documentation.Configuration`

**Goal:** Parse YAML → Resolve paths → Validate existence

```
Raw YAML + File System → Fully resolved configuration
```

**What it does:**
1. Parse YAML files (`docset.yml`, `toc.yml`, `navigation.yml`)
2. Resolve relative paths to absolute paths from docset root
3. Validate files exist on disk
4. Load nested `toc.yml` files recursively
5. Emit configuration errors

**Example:**
```csharp
// In: Raw YAML
toc:
  - toc: api
    # api/toc.yml contains:
    toc:
      - file: rest.md  # Relative to api/toc.yml

// Out: Fully resolved
FileRef {
    PathRelativeToDocumentationSet = "api/rest.md"  // ✓ From docset root
}
```

**Key point:** All paths become relative to docset root. No more file I/O needed.

## Phase 2: Navigation Construction

**Package:** `Elastic.Documentation.Navigation`

**Goal:** Build tree → Calculate URLs → Set relationships

```
Resolved Configuration → Navigation tree with URLs
```

**What it does:**
1. Create node objects from configuration
2. Set parent-child relationships
3. Set up home providers (for URL calculation)
4. Calculate navigation indexes
5. Emit navigation errors

**Example:**
```csharp
// In: Resolved configuration
FileRef { PathRelativeToDocumentationSet = "api/rest.md" }

// Out: Navigation with URL
FileNavigationLeaf {
    Url = "/api/rest/",
    Parent = TableOfContentsNavigation,
    NavigationRoot = DocumentationSetNavigation
}
```

**Key point:** URLs calculated dynamically from HomeProvider. No stored paths.

## The Flow

```
┌─────────────────────────────────────┐
│ Phase 1: Configuration              │
├─────────────────────────────────────┤
│ YAML files + File system            │
│           ↓                         │
│ Parse & validate                    │
│           ↓                         │
│ Resolve all paths                   │
│           ↓                         │
│ DocumentationSetFile                │
│ (all paths relative to docset root) │
└─────────────────────────────────────┘
                ↓
┌─────────────────────────────────────┐
│ Phase 2: Navigation                 │
├─────────────────────────────────────┤
│ Resolved configuration              │
│           ↓                         │
│ Build tree                          │
│           ↓                         │
│ Set relationships                   │
│           ↓                         │
│ Set up URL providers                │
│           ↓                         │
│ DocumentationSetNavigation          │
│ (complete tree with URLs)           │
└─────────────────────────────────────┘
```

## Path Resolution Example

**Before Phase 1:**
```yaml
# docset.yml
toc:
  - toc: api

# api/toc.yml
toc:
  - file: rest.md      # ← Relative to api/
  - file: graphql.md   # ← Relative to api/
```

**After Phase 1:**
```csharp
IsolatedTableOfContentsRef {
    PathRelativeToDocumentationSet = "api",
    Children = [
        FileRef { PathRelativeToDocumentationSet = "api/rest.md" },     // ✓
        FileRef { PathRelativeToDocumentationSet = "api/graphql.md" }   // ✓
    ]
}
```

All paths now relative to docset root. Phase 2 can build without touching filesystem.

## Error Attribution

Clear errors because phases are separate:

**Phase 1 errors (configuration):**
```
Error: File 'api/missing.md' not found at /docs/api/missing.md
Error: TableOfContents 'api' cannot have children in docset.yml
```
→ Fix your YAML or add the file.

**Phase 2 errors (navigation):**
```
Error: Documentation set has no table of contents defined
Error: Folder 'setup' has children defined but none could be created
```
→ Fix your navigation structure.

## Testing Benefits

**Phase 1 tests:**
```csharp
[Fact]
public void LoadAndResolve_ResolvesNestedPaths()
{
    var yaml = "toc:\n  - toc: api";
    var fs = new MockFileSystem();
    fs.AddFile("/docs/api/toc.yml", "toc:\n  - file: rest.md");

    var docset = DocumentationSetFile.LoadAndResolve(
        collector, yaml, fs.NewDirInfo("/docs")
    );

    var fileRef = docset.TableOfContents[0].Children[0] as FileRef;
    Assert.Equal("api/rest.md", fileRef.PathRelativeToDocumentationSet);
}
```
Tests YAML parsing and path resolution.

**Phase 2 tests:**
```csharp
[Fact]
public void Constructor_CreatesNavigationTree()
{
    // Pre-resolved configuration (no file I/O!)
    var docset = new DocumentationSetFile {
        TableOfContents = [
            new FileRef { PathRelativeToDocumentationSet = "index.md" }
        ]
    };

    var nav = new DocumentationSetNavigation<IDocumentationFile>(
        docset, context, factory
    );

    Assert.Equal("/", nav.Index.Url);
}
```
Tests tree construction without file system.

## Reusability

Same configuration works for both build modes:

```csharp
// Phase 1: Build configuration once
var docset = DocumentationSetFile.LoadAndResolve(
    collector, yaml, fileSystem.NewDirInfo("/docs")
);

// Phase 2a: Isolated build
var isolatedNav = new DocumentationSetNavigation<IDocumentationFile>(
    docset,  // ← Same config
    isolatedContext,
    factory
);
// URLs: /api/rest/

// Phase 2b: Assembler build
var siteNav = new SiteNavigation(
    siteConfig,
    assemblerContext,
    [isolatedNav],  // ← Reuse isolated navigation
    sitePrefix: null
);
// Re-home: /api/rest/ → /elasticsearch/api/rest/
```

## Assembler Extension

Assembler adds two more phases:

```
Phase 1a: Load individual docset configs
    ↓
Phase 2a: Build isolated navigations
    ↓
Phase 1b: Load site navigation config
    ↓
Phase 2b: Assemble + re-home
```

Each docset goes through Phases 1 & 2 independently, then site navigation assembles them.

## Key Invariants

**After Phase 1:**
- ✅ All paths relative to docset root
- ✅ All files validated to exist
- ✅ All nested TOCs loaded
- ✅ Configuration structure validated

**After Phase 2:**
- ✅ Complete navigation tree
- ✅ All relationships set (parent/child/root)
- ✅ All home providers configured
- ✅ All URLs calculable

## Summary

| Aspect | Phase 1 | Phase 2 |
|--------|---------|---------|
| **Package** | `Configuration` | `Navigation` |
| **Input** | YAML + File system | Resolved config |
| **Output** | Resolved config | Navigation tree |
| **Errors** | Config/file issues | Structure issues |
| **File I/O** | Yes | No |
| **Testing** | Mock file system | Mock config |
| **Reusable** | Yes (both builds) | Build-specific |

**The key insight:** Configuration is about files and YAML. Navigation is about tree structure and URLs. Keep them separate.
