# Mermaid diagrams

You can create diagrams using [Mermaid](https://mermaid.js.org/) with standard fenced code blocks. Diagrams are rendered client-side in the browser.

```mermaid
flowchart LR
    A[Write Mermaid] --> B[Render diagram]
```

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

### Complex flowchart

::::::{tab-set}

:::::{tab-item} Source
````markdown
```mermaid
graph TB
    A["Painless Operators"]

    B["General"]
    C["Numeric"]
    D["Boolean"]
    E["Reference"]
    F["Array"]

    B1["Control expression flow and<br/>value assignment"]
    C1["Mathematical operations and<br/>bit manipulation"]
    D1["Boolean logic and<br/>conditional evaluation"]
    E1["Object interaction and<br/>safe data access"]
    F1["Array manipulation and<br/>element access"]

    B2["Precedence ( )<br/>Function Call ( )<br/>Cast ( )<br/>Conditional ? :<br/>Elvis ?:<br/>Assignment =<br/>Compound Assignment $="]
    C2["Post/Pre Increment ++<br/>Post/Pre Decrement --<br/>Unary +/-<br/>Bitwise Not ~<br/>Multiplication *<br/>Division /<br/>Remainder %<br/>Addition +<br/>Subtraction -<br/>Shift <<, >>, >>><br/>Bitwise And &<br/>Bitwise Xor ^<br/>Bitwise Or |"]
    D2["Boolean Not !<br/>Comparison >, >=, <, <=<br/>Instanceof instanceof<br/>Equality ==, !=<br/>Identity ===, !==<br/>Boolean Xor ^<br/>Boolean And &&<br/>Boolean Or ||"]
    E2["Method Call . ( )<br/>Field Access .<br/>Null Safe ?.<br/>New Instance new ( )<br/>String Concatenation +<br/>List/Map Init [ ], [ : ]<br/>List/Map Access [ ]"]
    F2["Array Init [ ] { }<br/>Array Access [ ]<br/>Array Length .length<br/>New Array new [ ]"]

    A --> B & C & D & E & F
    B --> B1
    C --> C1
    D --> D1
    E --> E1
    F --> F1
    B1 --> B2
    C1 --> C2
    D1 --> D2
    E1 --> E2
    F1 --> F2

    classDef rootNode fill:#0B64DD,stroke:#101C3F,stroke-width:2px,color:#fff
    classDef categoryBox fill:#e1f5fe,stroke:#01579b,stroke-width:2px,color:#343741
    classDef descBox fill:#48EFCF,stroke:#343741,stroke-width:2px,color:#343741
    classDef exampleBox fill:#f5f7fa,stroke:#343741,stroke-width:2px,color:#343741

    class A rootNode
    class B,C,D,E,F categoryBox
    class B1,C1,D1,E1,F1 descBox
    class B2,C2,D2,E2,F2 exampleBox
```
````
:::::

:::::{tab-item} Rendered
```mermaid
graph TB
    A["Painless Operators"]

    B["General"]
    C["Numeric"]
    D["Boolean"]
    E["Reference"]
    F["Array"]

    B1["Control expression flow and<br/>value assignment"]
    C1["Mathematical operations and<br/>bit manipulation"]
    D1["Boolean logic and<br/>conditional evaluation"]
    E1["Object interaction and<br/>safe data access"]
    F1["Array manipulation and<br/>element access"]

    B2["Precedence ( )<br/>Function Call ( )<br/>Cast ( )<br/>Conditional ? :<br/>Elvis ?:<br/>Assignment =<br/>Compound Assignment $="]
    C2["Post/Pre Increment ++<br/>Post/Pre Decrement --<br/>Unary +/-<br/>Bitwise Not ~<br/>Multiplication *<br/>Division /<br/>Remainder %<br/>Addition +<br/>Subtraction -<br/>Shift <<, >>, >>><br/>Bitwise And &<br/>Bitwise Xor ^<br/>Bitwise Or |"]
    D2["Boolean Not !<br/>Comparison >, >=, <, <=<br/>Instanceof instanceof<br/>Equality ==, !=<br/>Identity ===, !==<br/>Boolean Xor ^<br/>Boolean And &&<br/>Boolean Or ||"]
    E2["Method Call . ( )<br/>Field Access .<br/>Null Safe ?.<br/>New Instance new ( )<br/>String Concatenation +<br/>List/Map Init [ ], [ : ]<br/>List/Map Access [ ]"]
    F2["Array Init [ ] { }<br/>Array Access [ ]<br/>Array Length .length<br/>New Array new [ ]"]

    A --> B & C & D & E & F
    B --> B1
    C --> C1
    D --> D1
    E --> E1
    F --> F1
    B1 --> B2
    C1 --> C2
    D1 --> D2
    E1 --> E2
    F1 --> F2

    classDef rootNode fill:#0B64DD,stroke:#101C3F,stroke-width:2px,color:#fff
    classDef categoryBox fill:#e1f5fe,stroke:#01579b,stroke-width:2px,color:#343741
    classDef descBox fill:#48EFCF,stroke:#343741,stroke-width:2px,color:#343741
    classDef exampleBox fill:#f5f7fa,stroke:#343741,stroke-width:2px,color:#343741

    class A rootNode
    class B,C,D,E,F categoryBox
    class B1,C1,D1,E1,F1 descBox
    class B2,C2,D2,E2,F2 exampleBox
```
:::::

::::::

## Interactive controls

Mermaid diagrams include interactive controls that appear when you hover over the diagram:

- **Zoom in/out**: Click the `+` and `-` buttons to zoom in or out. You can also hold `Ctrl` (or `Cmd` on macOS) and use the mouse wheel to zoom.
- **Reset**: Click the reset button to return to the default view.
- **Fullscreen**: Click the expand button to view the diagram in a fullscreen modal.
- **Pan**: Click and drag the diagram to pan around when zoomed in.

These controls are particularly useful for large or complex diagrams.

## Notes

- Diagrams require JavaScript to render. Users with JavaScript disabled will see the raw Mermaid code.
- For the full list of diagram types and syntax, see the [Mermaid documentation](https://mermaid.js.org/intro/).
