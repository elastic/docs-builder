# Diagram Directive

The `diagram` directive allows you to render various types of diagrams using the [Kroki](https://kroki.io/) service. Kroki supports many diagram types including Mermaid, D2, Graphviz, PlantUML, and more.

## Basic Usage

The basic syntax for the diagram directive is:

```markdown
::::{diagram} [diagram-type]
<diagram content>
::::
```

If no diagram type is specified, it defaults to `mermaid`.

## Supported Diagram Types

The diagram directive supports the following diagram types:

- `mermaid` - Mermaid diagrams (default)
- `d2` - D2 diagrams
- `graphviz` - Graphviz/DOT diagrams
- `plantuml` - PlantUML diagrams
- `ditaa` - Ditaa diagrams
- `erd` - Entity Relationship diagrams
- `excalidraw` - Excalidraw diagrams
- `nomnoml` - Nomnoml diagrams
- `pikchr` - Pikchr diagrams
- `structurizr` - Structurizr diagrams
- `svgbob` - Svgbob diagrams
- `vega` - Vega diagrams
- `vegalite` - Vega-Lite diagrams
- `wavedrom` - WaveDrom diagrams

## Examples

### Mermaid Flowchart (Default)

::::{diagram}
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
::::

### Mermaid Sequence Diagram

::::{diagram} mermaid
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello Bob, how are you?
    B-->>A: Great!
::::

### D2 Diagram

::::{diagram} d2
x -> y: hello world
y -> z: nice to meet you
::::

### Graphviz Diagram

::::{diagram} graphviz
digraph G {
    rankdir=LR;
    A -> B -> C;
    A -> C;
}
::::

## How It Works

The diagram directive:

1. **Parses** the diagram type from the directive argument
2. **Extracts** the diagram content from the directive body
3. **Encodes** the content using zlib compression and Base64URL encoding
4. **Generates** a Kroki URL in the format: `https://kroki.io/{type}/svg/{encoded-content}`
5. **Renders** an HTML `<img>` tag that loads the diagram from Kroki

## Error Handling

If the diagram content is empty or the encoding fails, an error message will be displayed instead of the diagram.

## Implementation Details

The diagram directive is implemented using:

- **DiagramBlock**: Parses the directive and extracts content
- **DiagramEncoder**: Handles compression and encoding using the same algorithm as the Kroki documentation
- **DiagramView**: Renders the final HTML with the Kroki URL
- **Kroki Service**: External service that generates SVG diagrams from encoded content

The encoding follows the Kroki specification exactly, ensuring compatibility with all supported diagram types.
