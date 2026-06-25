# Vector sizing directive (implemented in-tree)

The `{vector-sizing-calculator}` directive is implemented under:

- `src/Elastic.Markdown/Myst/Directives/VectorSizing/`
- `DirectiveBlockParser.cs` — registers the block
- `DirectiveHtmlRenderer.cs` — renders via `VectorSizingView`

The assembled docs site loads the widget from `src/Elastic.Documentation.Site/Assets/main.ts` (dynamic import of `VectorSizingCalculatorComponent`).
