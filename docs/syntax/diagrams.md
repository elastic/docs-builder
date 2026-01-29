# Mermaid diagrams

Create diagrams using [Mermaid](https://mermaid.js.org/) with standard fenced code blocks. Diagrams are rendered client-side in the browser.

## Basic usage

Use a fenced code block with `mermaid` as the language:

````markdown
```mermaid
flowchart LR
A --> B
```
````

## Supported diagram types

All [Mermaid diagram types](https://mermaid.js.org/intro/) are supported, including:

- Flowcharts
- Sequence diagrams
- State diagrams
- Class diagrams
- Entity relationship (ER) diagrams
- And more

## Examples

### Flowchart

:::::{tab-set}

::::{tab-item} Source
````markdown
```mermaid
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
```
````
::::

::::{tab-item} Rendered
```mermaid
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
```
::::

:::::

### Sequence diagram

:::::{tab-set}

::::{tab-item} Source
````markdown
```mermaid
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello Bob, how are you?
    B-->>A: Great!
```
````
::::

::::{tab-item} Rendered
```mermaid
sequenceDiagram
    participant A as Alice
    participant B as Bob
    A->>B: Hello Bob, how are you?
    B-->>A: Great!
```
::::

:::::

### State diagram

:::::{tab-set}

::::{tab-item} Source
````markdown
```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Processing: start
    Processing --> Complete: done
    Complete --> [*]
```
````
::::

::::{tab-item} Rendered
```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Processing: start
    Processing --> Complete: done
    Complete --> [*]
```
::::

:::::

### Class diagram

:::::{tab-set}

::::{tab-item} Source
````markdown
```mermaid
classDiagram
    Animal <|-- Duck
    Animal <|-- Fish
    Animal : +int age
    Animal : +isMammal() bool
    Duck : +String beakColor
    Duck : +quack()
    Fish : +int sizeInFeet
    Fish : +canEat()
```
````
::::

::::{tab-item} Rendered
```mermaid
classDiagram
    Animal <|-- Duck
    Animal <|-- Fish
    Animal : +int age
    Animal : +isMammal() bool
    Duck : +String beakColor
    Duck : +quack()
    Fish : +int sizeInFeet
    Fish : +canEat()
```
::::

:::::

### ER diagram

:::::{tab-set}

::::{tab-item} Source
````markdown
```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE_ITEM : contains
    PRODUCT ||--o{ LINE_ITEM : "is in"
```
````
::::

::::{tab-item} Rendered
```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ LINE_ITEM : contains
    PRODUCT ||--o{ LINE_ITEM : "is in"
```
::::

:::::

## Notes

- Diagrams require JavaScript to render. Users with JavaScript disabled will see the raw Mermaid code.
- For the full list of diagram types and syntax, see the [Mermaid documentation](https://mermaid.js.org/intro/).
