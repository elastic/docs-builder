# First Principles

The navigation system is built on two types of principles:

## [Functional Principles](functional-principles.md)

These define **what** the navigation system does and **why**:

1. **Two-Phase Loading** - Separate configuration resolution from navigation construction
2. **Single Documentation Source** - All paths relative to docset root
3. **URL Building is Dynamic** - URLs calculated on-demand, not stored
4. **Navigation Roots Can Be Re-homed** - O(1) URL prefix changes
5. **Navigation Scope via HomeProvider** - Scoped URL calculation contexts
6. **Index Files Determine URLs** - Folders and indexes share URLs
7. **File Structure Mirrors Navigation** - Predictable, maintainable structure
8. **Acyclic Graph Structure** - Tree with no cycles, unique URLs
9. **Phantom Nodes** - Acknowledge content without including it

[Read Functional Principles →](functional-principles.md)

## [Technical Principles](technical-principles.md)

These define **how** the navigation system is implemented:

1. **Generic Type System** - Covariance enables static typing without runtime casts
2. **Provider Pattern** - Decouples URL calculation from tree structure
3. **Lazy URL Calculation** - Smart caching with automatic invalidation

[Read Technical Principles →](technical-principles.md)

---

## Quick Reference

**For understanding architecture:** Start with [Functional Principles](functional-principles.md)

**For implementation details:** See [Technical Principles](technical-principles.md)

**For visual examples:** See [Visual Walkthrough](visual-walkthrough.md)

**For specific topics:**
- How assembly works: [Assembler Process](assembler-process.md)
- How re-homing works: [Home Provider Architecture](home-provider-architecture.md)
- Two-phase approach: [Two-Phase Loading](two-phase-loading.md)
- Node reference: [Node Types](node-types.md)
