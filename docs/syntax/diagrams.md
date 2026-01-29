# Mermaid diagrams

The `mermaid` directive allows you to render Mermaid diagrams using [Beautiful Mermaid](https://github.com/lukilabs/beautiful-mermaid). Diagrams are rendered to SVG at build time and embedded directly in the HTML output.

## Basic usage

The basic syntax for the mermaid directive is:

```markdown
::::{mermaid}
<mermaid content>
::::
```

## Supported diagram types

The mermaid directive supports [Mermaid](https://mermaid.js.org/) diagrams, including:

- Flowcharts
- Sequence diagrams
- State diagrams
- Class diagrams
- Entity Relationship (ER) diagrams

## Examples

### Flowchart

::::::{tab-set}

:::::{tab-item} Source
```markdown
::::{mermaid}
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
::::
```
:::::

:::::{tab-item} Rendered
::::{mermaid}
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
::::
:::::

::::::

### Sequence diagram

::::::{tab-set}

:::::{tab-item} Source
```markdown
::::{mermaid}
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello Bob, how are you?
    B-->>A: Great!
::::
```
:::::

:::::{tab-item} Rendered
::::{mermaid}
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello Bob, how are you?
    B-->>A: Great!
::::
:::::

::::::

### State diagram

::::::{tab-set}

:::::{tab-item} Source
```markdown
::::{mermaid}
stateDiagram-v2
    [*] --> Idle
    Idle --> Processing: start
    Processing --> Complete: done
    Complete --> [*]
::::
```
:::::

:::::{tab-item} Rendered
::::{mermaid}
stateDiagram-v2
    [*] --> Idle
    Idle --> Processing: start
    Processing --> Complete: done
    Complete --> [*]
::::
:::::

::::::

### Class diagram

::::::{tab-set}

:::::{tab-item} Source
```markdown
::::{mermaid}
classDiagram
    Animal <|-- Duck
    Animal <|-- Fish
    Animal: +int age
    Animal: +isMammal() bool
    Duck: +String beakColor
    Duck: +quack()
    Fish: +int sizeInFeet
    Fish: +canEat()
::::
```
:::::

:::::{tab-item} Rendered
::::{mermaid}
classDiagram
    Animal <|-- Duck
    Animal <|-- Fish
    Animal: +int age
    Animal: +isMammal() bool
    Duck: +String beakColor
    Duck: +quack()
    Fish: +int sizeInFeet
    Fish: +canEat()
::::
:::::

::::::

### ER diagram

::::::{tab-set}

:::::{tab-item} Source
```markdown
::::{mermaid}
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE_ITEM : contains
    PRODUCT ||--o{ LINE_ITEM : "is in"
::::
```
:::::

:::::{tab-item} Rendered
::::{mermaid}
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE_ITEM : contains
    PRODUCT ||--o{ LINE_ITEM : "is in"
::::
:::::

::::::

## Error handling

If the Mermaid content is empty or the syntax is invalid, an error will be reported during the build process. This helps catch diagram issues early rather than at render time.
