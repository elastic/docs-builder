## Replace Kroki with Beautiful Mermaid for diagram rendering

### Summary

- Replaced the external Kroki service with [Beautiful Mermaid](https://github.com/lukilabs/beautiful-mermaid) for build-time SVG rendering.
- Renamed the `{diagram}` directive to `{mermaid}` to reflect the focused scope.
- Diagrams are now rendered during build and embedded inline in HTML.

### Benefits over Kroki

| Aspect | Kroki | Beautiful Mermaid |
|--------|-------|-------------------|
| **Dependency** | External service (network required) | Local Node.js (offline capable) |
| **Rendering** | Runtime fetch via URL | Build-time SVG generation |
| **Page load** | Additional HTTP request per diagram | Zero runtime overhead |
| **Availability** | Subject to service uptime | Always available |
| **Styling** | Limited control | High-contrast theme with inlined colors |

### Changes

- `src/Elastic.Documentation.Site/MermaidRenderer.cs` - C# wrapper to invoke Node.js renderer
- `src/Elastic.Documentation.Site/scripts/mermaid-renderer.mjs` - Node.js script using beautiful-mermaid
- `src/Elastic.Markdown/Myst/Directives/Mermaid/` - Directive block, view, and view model
- `docs/syntax/mermaid.md` - Updated documentation with examples
- `tests/Elastic.Markdown.Tests/Directives/DiagramTests.cs` - Comprehensive test coverage

### Supported diagram types

- Flowcharts
- Sequence diagrams
- State diagrams
- Class diagrams
- Entity Relationship (ER) diagrams
