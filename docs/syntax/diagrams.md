# Diagrams

You can create and embed diagrams using Mermaid.js, a docs-as-code diagramming tool.

Using Mermaid diagrams has several advantages:

- Diagrams as code: You can version and edit diagrams without having to use third-party tools.
- Easier contribution: External contributors can add or edit new diagrams easily.
- Consistent look and feel: All Mermaid diagrams are rendered using the same style.
- Improved accessibility: Text can be copied and read by a text-to-speech engine.

## Create Mermaid diagrams

To create a Mermaid, you can use the following editors:

- [Mermaid Live Editor](https://mermaid.live/): Instantly previews Mermaid diagrams.
- [Mermaid Chart](https://www.mermaidchart.com/app/dashboard): Visual editor for Mermaid.
- Create diagrams in Visual Studio Code and preview them using the [VS Code extension](https://docs.mermaidchart.com/plugins/visual-studio-code).
- Other tools, including AI tools, can generate Mermaid diagrams.

For reference documentation on the Mermaid language, refer to [mermaid.js](https://mermaid.js.org).

## Syntax guidelines

When creating Mermaid diagrams, keep these guidelines in mind:

- Use clear, descriptive node names.
- Use comments (`%% comment text`) to document complex diagrams.
- Break complex diagrams into smaller, more manageable ones.
- Use consistent naming conventions throughout your diagrams.

## Supported Diagram Types

Mermaid.js supports various diagram types to visualize different kinds of information:

- Flowcharts: Visualize processes and workflows.
- Sequence Diagrams: Show interactions between components over time.
- Gantt Charts: Illustrate project schedules and timelines.
- Class Diagrams: Represent object-oriented structures.
- Entity Relationship Diagrams: Model database structures.
- State Diagrams: Illustrate state machines and transitions.
- Pie Charts: Display proportional data.
- User Journey Maps: Visualize user experiences.

For a full list of supported diagrams, see the [Mermaid.js](https://mermaid.js.org/intro/) documentation.

### Flowcharts

This is an example flowchart made with Mermaid:

:::::{tab-set}

::::{tab-item} Output

```mermaid
flowchart LR
A[Jupyter Notebook] --> C
B[MyST Markdown] --> C
C(mystmd) --> D{AST}
D <--> E[LaTeX]
E --> F[PDF]
D --> G[Word]
D --> H[React]
D --> I[HTML]
D <--> J[JATS]
```

::::

::::{tab-item} Markdown
````markdown
```mermaid 
flowchart LR
A[Jupyter Notebook] --> C
B[MyST Markdown] --> C
C(mystmd) --> D{AST}
D <--> E[LaTeX]
E --> F[PDF]
D --> G[Word]
D --> H[React]
D --> I[HTML]
D <--> J[JATS]
```
````
::::

:::::


### Sequence diagrams

This is an example sequence diagram made with Mermaid:

:::::{tab-set}

::::{tab-item} Output

```mermaid
sequenceDiagram
    participant Alice
    participant Bob
    Alice->>John: Hello John, how are you?
    loop HealthCheck
        John->>John: Fight against hypochondria
    end
    Note right of John: Rational thoughts <br/>prevail!
    John-->>Alice: Great!
    John->>Bob: How about you?
    Bob-->>John: Jolly good!
```

::::

::::{tab-item} Markdown
````markdown
```mermaid 
sequenceDiagram
    participant Alice
    participant Bob
    Alice->>John: Hello John, how are you?
    loop HealthCheck
        John->>John: Fight against hypochondria
    end
    Note right of John: Rational thoughts <br/>prevail!
    John-->>Alice: Great!
    John->>Bob: How about you?
    Bob-->>John: Jolly good!
```
````
::::

:::::

### Gantt charts

This is an example Gantt chart made with Mermaid:

:::::{tab-set}

::::{tab-item} Output

```mermaid
gantt
dateFormat  YYYY-MM-DD
title Adding GANTT diagram to mermaid
excludes weekdays 2014-01-10

section A section
Completed task            :done,    des1, 2014-01-06,2014-01-08
Active task               :active,  des2, 2014-01-09, 3d
Future task               :         des3, after des2, 5d
Future task2               :         des4, after des3, 5d
```

::::

::::{tab-item} Markdown
````markdown
```mermaid 
gantt
dateFormat  YYYY-MM-DD
title Adding GANTT diagram to mermaid
excludes weekdays 2014-01-10

section A section
Completed task            :done,    des1, 2014-01-06,2014-01-08
Active task               :active,  des2, 2014-01-09, 3d
Future task               :         des3, after des2, 5d
Future task2               :         des4, after des3, 5d
```
````
::::

:::::

### Class diagrams

This is an example class diagram made with Mermaid:

:::::{tab-set}

::::{tab-item} Output

```mermaid
classDiagram
Class01 <|-- AveryLongClass : Cool
Class03 *-- Class04
Class05 o-- Class06
Class07 .. Class08
Class09 --> C2 : Where am i?
Class09 --* C3
Class09 --|> Class07
Class07 : equals()
Class07 : Object[] elementData
Class01 : size()
Class01 : int chimp
Class01 : int gorilla
Class08 <--> C2: Cool label
```

::::

::::{tab-item} Markdown
````markdown
```mermaid 
classDiagram
Class01 <|-- AveryLongClass : Cool
Class03 *-- Class04
Class05 o-- Class06
Class07 .. Class08
Class09 --> C2 : Where am i?
Class09 --* C3
Class09 --|> Class07
Class07 : equals()
Class07 : Object[] elementData
Class01 : size()
Class01 : int chimp
Class01 : int gorilla
Class08 <--> C2: Cool label
```
````
::::

:::::

## Troubleshooting

These are the most common issues when creating Mermaid diagrams and their solution:

- Syntax errors: Ensure proper indentation and syntax.
- Rendering issues: Check for unsupported characters or syntax.
- Performance: Simplify diagrams with many nodes for better performance.