# Vector sizing calculator

The `{vector-sizing-calculator}` directive embeds an interactive calculator for estimating disk and off-heap memory for `dense_vector` fields. The docs site loads the React/EUI web component that registers the `<vector-sizing-calculator>` custom element.

## Usage

```markdown
:::{vector-sizing-calculator}
:::
```

No body content or options are required. Configuration happens in the widget UI.

## Live preview

:::{vector-sizing-calculator}
:::

## Requirements

This directive only produces useful output where the assembled documentation site includes the vector sizing bundle (see `src/Elastic.Documentation.Site/Assets/web-components/VectorSizingCalculator/`).
