# Tables

A table is an arrangement of data with rows and columns. Each row consists of cells containing arbitrary text in which inlines are parsed, separated by pipes `|`. The rows of a table consist of:

* a single header row
* a delimiter row separating the header from the data
* zero or more data rows

## Basic Table

::::{tab-set}

:::{tab-item} Output
| Country | Capital         |
| ------- | --------------- |
| USA     | Washington D.C. |
| Canada  | Ottawa          |
| Mexico  | Mexico City     |
| Brazil  | Brasília        |
| UK      | London          |
:::

:::{tab-item} Markdown
```markdown
| Country | Capital         |
| ------- | --------------- |
| USA     | Washington D.C. |
| Canada  | Ottawa          |
| Mexico  | Mexico City     |
| Brazil  | Brasília        |
| UK      | London          |
:::

::::

:::{note}

* A leading and trailing pipe is recommended for clarity of reading
* Spaces between pipes and cell content are trimmed
* Block-level elements cannot be inserted in a table

:::


## Table without header

::::{tab-set}

:::{tab-item} Output
|                   |         |
|-------------------|---------|
| **Country**       | Austria |
| **Capital**       | Vienna  |
| **Calling code**  | +43     |
| **ISO 3166 code** | AT      |
:::

:::{tab-item} Markdown
```markdown
|                   |         |
|-------------------|---------|
| **Country**       | Austria |
| **Capital**       | Vienna  |
| **Calling code**  | +43     |
| **ISO 3166 code** | AT      |
```
:::

::::

## Responsive Table

Every table is responsive by default. The table will automatically scroll horizontally when the content is wider than the viewport.

::::{tab-set}

:::{tab-item} Output
| Product Name | Price ($) | Stock  | Category  | Rating  | Color    | Weight (kg) | Warranty (months) |
|--------------|-----------|--------|-----------|---------|----------|-------------|-------------------|
| Laptop Pro   | 1299.99   | 45     | Computer  | 4.5     | Silver   | 1.8         | 24                |
| Smart Watch  | 299.99    | 120    | Wearable  | 4.2     | Black    | 0.045       | 12                |
| Desk Chair   | 199.50    | 78     | Furniture | 4.8     | Gray     | 12.5        | 36                |
:::

:::{tab-item} Markdown
```markdown
| Product Name | Price ($) | Stock  | Category  | Rating  | Color    | Weight (kg) | Warranty (months) |
|--------------|-----------|--------|-----------|---------|----------|-------------|-------------------|
| Laptop Pro   | 1299.99   | 45     | Computer  | 4.5     | Silver   | 1.8         | 24                |
| Smart Watch  | 299.99    | 120    | Wearable  | 4.2     | Black    | 0.045       | 12                |
| Desk Chair   | 199.50    | 78     | Furniture | 4.8     | Gray     | 12.5        | 36                |
:::
```

::::

## Table directive with column widths

The `{table}` directive wraps a pipe table and lets you control column widths using a 12-unit grid system (similar to Bootstrap). Use the `:widths:` option to specify how space is distributed across columns.

### Presets

- **`even`** (default): Columns share space evenly. Omit `:widths:` or set it to `even`.
- **`definition`**: First column 4 units, second column 8 units. Ideal for term/description tables.

### Custom widths

Use dash-separated integers that sum to 12. Each number is the grid units for that column:

- `4-8` — two columns: 33% and 67%
- `4-4-4` — three equal columns
- `3-3-3-3` — four equal columns

### Definition preset (term/description)

Ideal for glossaries, API parameter tables, or key-value lists where the first column is shorter.

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: definition

| Term | Description |
| --- | --- |
| Elasticsearch | A distributed search and analytics engine |
| Kibana | A visualization and management platform |
| Fleet | A host management solution for Elastic Agent |
| Beats | Lightweight data shippers for edge monitoring |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: definition

| Term | Description |
| --- | --- |
| Elasticsearch | A distributed search and analytics engine |
| Kibana | A visualization and management platform |
| Fleet | A host management solution for Elastic Agent |
| Beats | Lightweight data shippers for edge monitoring |
:::
```
::::

:::::

### Custom widths (4-8)

Two columns with a narrow first column (33%) and wider second column (67%).

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: 4-8

| Parameter | Value |
| --- | --- |
| `index` | The name of the index to search |
| `query` | The query DSL defining search criteria |
| `size` | Maximum number of hits to return (default: 10) |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: 4-8

| Parameter | Value |
| --- | --- |
| `index` | The name of the index to search |
| `query` | The query DSL defining search criteria |
| `size` | Maximum number of hits to return (default: 10) |
:::
```
::::

:::::

### Three equal columns (4-4-4)

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: 4-4-4

| Product | Version | Status |
| --- | --- | --- |
| Elasticsearch | 8.x | Current |
| Kibana | 8.x | Current |
| Logstash | 8.x | Current |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: 4-4-4

| Product | Version | Status |
| --- | --- | --- |
| Elasticsearch | 8.x | Current |
| Kibana | 8.x | Current |
| Logstash | 8.x | Current |
:::
```
::::

:::::

### Four equal columns (3-3-3-3)

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: 3-3-3-3

| Q1 | Q2 | Q3 | Q4 |
| --- | --- | --- | --- |
| 100 | 120 | 95 | 110 |
| 200 | 180 | 220 | 195 |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: 3-3-3-3

| Q1 | Q2 | Q3 | Q4 |
| --- | --- | --- | --- |
| 100 | 120 | 95 | 110 |
| 200 | 180 | 220 | 195 |
:::
```
::::

:::::

### Asymmetric layout (2-4-6)

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: 2-4-6

| ID | Name | Description |
| --- | --- | --- |
| 1 | Alpha | First item with a longer description |
| 2 | Beta | Second item with additional details |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: 2-4-6

| ID | Name | Description |
| --- | --- | --- |
| 1 | Alpha | First item with a longer description |
| 2 | Beta | Second item with additional details |
:::
```
::::

:::::

### Even preset (default)

When `:widths:` is omitted or set to `even`, columns share space evenly. No column width constraints are applied.

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: even

| Left | Center | Right |
| --- | --- | --- |
| 1 | 2 | 3 |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: even

| Left | Center | Right |
| --- | --- | --- |
| 1 | 2 | 3 |
:::
```
::::

:::::

### Validation

- Widths must sum to 12.
- The number of width values must match the table column count.
- The directive must contain exactly one pipe table.
