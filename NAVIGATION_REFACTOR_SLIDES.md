# Navigation Refactor - Walkthrough
**Branch:** `refactor/navigation` | **Date:** 2025-11-04

---

## Slide 1: Overview

**Impact: 169 files changed**
- 13,872 additions
- 2,756 deletions
- 4,699 new test lines

**Key Achievements**
- Type-safe covariant interfaces
- O(1) URL re-homing via Home Provider pattern
- Two-phase building (isolated + assembly)
- Complete separation of concerns
- Extensive test coverage

---

## Slide 2: Architecture - Before (main branch)

**Problems with Monolithic Navigation**

- Tight coupling - navigation built during markdown parsing
- No reusability - can't build docsets in isolation
- URL rigidity - URLs baked in at construction
- Mixed concerns - config + navigation + content entangled
- Weak typing - no covariance support

---

## Slide 3: Architecture - After (current branch)

**Two-Phase Isolated Navigation**

**Phase 1: Isolated Building**
- Each docset builds independently
- Self-contained navigation trees
- Cacheable, testable, parallelizable

**Phase 2: Assembly**
- Combine multiple docsets
- O(1) URL prefix updates
- Cross-docset reference resolution

---

## Slide 4: Type System - Covariance

**Problems in main branch:**
- `TIndex Index` - invariant, breaks polymorphism
- `int Depth` - stored at every node, wasteful
- `bool IsCrossLink` - runtime checks vs compile-time safety

**Solution:**
- Added `out` keyword for covariance
- Wrapped Index in `ILeafNavigationItem<TIndex>`
- Removed Depth (calculate on-demand)
- Removed IsCrossLink (use pattern matching)

---

## Slide 5: Interface Hierarchy

```
INavigationModel (marker)
    ↓
INavigationItem
    ├─→ ILeafNavigationItem<out TModel>
    │       └─ TModel Model
    │
    └─→ INodeNavigationItem<out TIndex, out TChildNavigation>
            ├─ ILeafNavigationItem<TIndex> Index
            ├─ IReadOnlyCollection<TChildNavigation> NavigationItems
            │
            └─→ IRootNavigationItem<out TIndex, out TChildNavigation>
                    ├─ bool IsUsingNavigationDropdown
                    ├─ Uri Identifier
                    └─ IAssignableChildrenNavigation
```

**Note:** `out` keywords enable upcasting to base types

---

## Slide 6: Home Provider Pattern - The Problem

**Scenario:** Combine repos with different URL prefixes
- `elasticsearch` repo → `/elasticsearch/docs/intro/`
- `kibana` repo → `/kibana/docs/intro/`

**Old approach (main branch):**
- URLs baked in at construction
- Changing prefixes = rebuild entire tree (O(n))
- High memory cost

---

## Slide 7: Home Provider Pattern - The Solution

**Key Interfaces:**
- `INavigationHomeProvider` - provides URL context
- `INavigationHomeAccessor` - nodes access/update provider

**How it works:**
1. Nodes reference provider (indirection)
2. URLs computed on-demand from PathPrefix
3. URLs cached per provider ID
4. **O(1) re-homing** - single assignment updates entire subtree

**Example:**
```csharp
elasticsearchNav.HomeProvider =
    new NavigationHomeProvider("/elasticsearch", siteNav);
// All URLs now use /elasticsearch/ prefix - O(1)!
```

---

## Slide 8: Two-Phase Building - Phase 1

**Phase 1: Isolated Building**

**Process:**
1. Parse `_docset.yml` via `LoadAndResolve()`
2. Build `DocumentationSetNavigation<TModel>`
3. Result: self-contained navigation tree

**Properties:**
- Independent (no knowledge of other docsets)
- Cacheable (serialize/load separately)
- Testable (isolated testing)
- Parallel (build concurrently)

---

## Slide 9: Two-Phase Building - Phase 2

**Phase 2: Assembly**

**Process:**
1. Parse `navigation.yml` listing docsets
2. Load each docset's navigation
3. Re-home: `docSetNav.HomeProvider = new NavigationHomeProvider(...)`
4. Add to `SiteNavigation`

**Result:**
- Each docset has custom URL prefix
- Cross-docset references resolved
- Phantom navigation for external refs
- Single unified hierarchy

---

## Slide 10: Configuration Layer Refactor

**Key Changes:**

**DocumentationSetFile** (726 lines)
- Pure data class for YAML config
- `LoadAndResolve()` parses and resolves paths
- Immutable, validated configuration

**DocumentationSetNavigation** (429 lines)
- Converts config → navigation tree
- Pure transformation, no YAML parsing

**ConfigurationFile** (140 lines, was ~350)
- Only product/version substitutions
- No TOC parsing or navigation building

---

## Slide 11: Node Type Hierarchy

**Root Nodes (can be re-homed):**
- `SiteNavigation`
- `DocumentationSetNavigation<TModel>`
- `TableOfContentsNavigation<TModel>`

**Intermediate Nodes:**
- `FolderNavigation<TModel>`
- `VirtualFileNavigation<TModel>`

**Leaf Nodes:**
- `FileNavigationLeaf<TModel>`
- `CrossLinkNavigationLeaf`

All implement `INavigationHomeAccessor` for re-homing

---

## Slide 12: Path Resolution

**The Problem (main branch):**
- Runtime path manipulation for URL building
- Different logic for isolated vs assembler builds

**The Solution:**
- `LoadAndResolve()` creates **two path representations**:
  1. `PathRelativeToDocumentationSet` (isolated builds)
  2. `PathRelativeToContainer` (assembler builds)

**URL Building:**
```csharp
var relativePath = isAssemblerBuild
    ? PathRelativeToContainer
    : PathRelativeToDocumentationSet;
```

**Benefits:** No runtime manipulation, simple conditional

---

## Slide 13: Diagnostic System - Hints

**New Suppressible Hints:**

**1. DeepLinkingVirtualFile**
- File with deep path that has children
- Suggests using `folder` syntax instead
- Virtual files for sibling grouping, not nesting

**2. FolderFileNameMismatch**
- Folder+file with mismatched names
- Best practice: file = folder name or index.md

**Suppressing:**
```yaml
suppress_hints:
  - deep_linking_virtual_file
  - folder_file_name_mismatch
```

---

## Slide 14: Test Infrastructure

**Total: 4,699 lines across 3 projects**

**Navigation.Tests/Isolation** (2,383 lines)
- Node construction, URL generation
- File validation, tree structure
- Hint emission, physical docset testing

**Navigation.Tests/Assembler** (1,528 lines)
- HomeProvider re-homing (O(1) changes)
- Multi-docset scenarios with cross-links
- Site-wide integration, phantom support

**Configuration.Tests** (1,153 lines)
- YAML deserialization, path resolution
- Dual path representation validation
- Real docset configuration testing

---

## Slide 15: Key Benefits

**Type Safety**
- Covariant interfaces catch errors at compile time
- Polymorphic navigation trees

**Performance**
- O(1) re-homing vs O(n) tree rebuilding
- Lazy URL evaluation with caching

**Modularity**
- Two-phase building enables caching
- Parallel docset processing

**Testability**
- 4,699 lines of tests
- >80% coverage

**Maintainability**
- Clear separation of concerns
- Extensive documentation in `/docs/development/navigation/`

---

## Slide 16: Summary

**Fundamental Architectural Improvements:**

1. **Covariant interfaces** - type-safe polymorphism
2. **Home Provider pattern** - O(1) URL re-homing
3. **Two-phase building** - isolated + assembly
4. **Path resolution** - eager resolution, context-aware
5. **Suppressible hints** - guidance without blocking
6. **Comprehensive tests** - 4,699 new test lines

**Documentation:** `/docs/development/navigation/`

---

## Slide 17: Questions?

**Documentation Resources:**
- `/docs/development/navigation/navigation.md` - Overview
- `/docs/development/navigation/home-provider-architecture.md` - Deep dive
- `/docs/development/navigation/functional-principles.md` - Design principles

**Test Examples:**
- `/tests/Navigation.Tests/` - Comprehensive test suite

**Next Steps:**
1. Review covariance changes in `INavigationItem.cs`
2. Understand Home Provider pattern
3. Trace navigation build in `DocumentationSetNavigation.cs`
4. Run tests to see system in action
