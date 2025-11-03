# Navigation

This document provides an overview of how `Elastic.Documentation.Navigation` works.

## Documentation Structure

**Start here:**
- **[Visual Walkthrough](visual-walkthrough.md)** - Visual tour with diagrams (best for first-time readers)
- **[First Principles](first-principles.md)** - Core design principles (functional and technical)

**Learn the architecture:**
- **[Functional Principles](functional-principles.md)** - What the system does and why
- **[Two-Phase Loading](two-phase-loading.md)** - Configuration resolution vs navigation construction
- **[Node Types](node-types.md)** - Reference for each navigation node type

**Understand key mechanisms:**
- **[Home Provider Architecture](home-provider-architecture.md)** - How O(1) re-homing works
- **[Assembler Process](assembler-process.md)** - How multiple repositories combine into one site

**Implementation details:**
- **[Technical Principles](technical-principles.md)** - How the system is implemented

## Quick Start

### Core Concepts

The navigation system builds hierarchical trees for documentation sites with these key features:

1. **Two Build Modes:**
   - **Isolated** - Single repository (e.g., `docs-builder isolated build`)
   - **Assembler** - Multi-repository site (e.g., `docs-builder assemble`)

2. **Two-Phase Loading:**
   - **Phase 1**: Parse YAML, resolve paths → Configuration
   - **Phase 2**: Build tree, calculate URLs → Navigation

3. **Re-homing:**
   - Build navigation in isolation with URLs like `/api/rest/`
   - Re-home during assembly to URLs like `/elasticsearch/api/rest/`
   - **O(1) operation** - no tree traversal needed!

### How Re-homing Works

The key innovation is the [Home Provider pattern](home-provider-architecture.md):

```csharp
// Isolated build
DocumentationSetNavigation
{
    HomeProvider: self,
    PathPrefix: ""
}
// Child URL: /api/rest/

// Re-home for assembler build (ONE LINE!)
docset.HomeProvider = new NavigationHomeProvider("/guide", siteNav);

// Child URL: /guide/api/rest/  ✨ All URLs updated!
```

This is possible because URLs are **calculated dynamically** from the HomeProvider, not stored. Changing the provider instantly updates all descendant URLs without any tree traversal.

See [Home Provider Architecture](home-provider-architecture.md) for the complete explanation.

## Visual Examples

For a visual tour of navigation structures with diagrams showing both isolated and assembler builds, see the **[Visual Walkthrough](visual-walkthrough.md)**.

The walkthrough covers:
- What nodes look like in isolated vs assembler builds
- How the same content appears with different URLs
- How to split and reorganize documentation across the site
- Common patterns for organizing multi-repository sites
- Examples with the actual tree diagrams from this repository

