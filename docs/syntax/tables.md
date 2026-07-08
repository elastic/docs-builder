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

Every table is responsive by default. The table will automatically scroll horizontally when the content is wider than the viewport. Tables that are wider than the content column also get a fullscreen button that opens the table in a fullscreen view with a sticky header row — useful for reference matrices with many scored columns.

::::{tab-set}

:::{tab-item} Output
| Region | Revenue ($M) | YoY Growth (%) | New Customers | Churn Rate (%) | NPS Score | Retention (%) | Upsell Revenue ($M) | Overall Score |
|--------|---------------|------------------|-----------------|------------------|-----------|-----------------|-----------------------|----------------|
| North America   | 42.5 | 12.3 | 1450 | 4.2 | 58 | 91.2 | 6.8 | 8.4 |
| EMEA            | 31.2 | 9.8  | 1120 | 5.1 | 52 | 88.7 | 5.2 | 7.6 |
| APAC            | 27.8 | 15.6 | 1680 | 6.3 | 47 | 85.4 | 4.1 | 7.1 |
| LATAM           | 14.6 | 18.2 | 980  | 7.8 | 41 | 81.9 | 2.9 | 6.3 |
| ANZ             | 9.3  | 7.4  | 340  | 3.9 | 61 | 92.5 | 1.8 | 7.9 |
| Nordics         | 8.1  | 6.2  | 290  | 3.1 | 64 | 93.8 | 1.5 | 8.1 |
| Southern Europe | 11.4 | 8.9  | 510  | 5.6 | 49 | 86.1 | 2.2 | 6.9 |
| Middle East     | 6.7  | 22.1 | 410  | 8.4 | 38 | 78.3 | 1.1 | 5.8 |
:::

:::{tab-item} Markdown
```markdown
| Region | Revenue ($M) | YoY Growth (%) | New Customers | Churn Rate (%) | NPS Score | Retention (%) | Upsell Revenue ($M) | Overall Score |
|--------|---------------|------------------|-----------------|------------------|-----------|-----------------|-----------------------|----------------|
| North America   | 42.5 | 12.3 | 1450 | 4.2 | 58 | 91.2 | 6.8 | 8.4 |
| EMEA            | 31.2 | 9.8  | 1120 | 5.1 | 52 | 88.7 | 5.2 | 7.6 |
| APAC            | 27.8 | 15.6 | 1680 | 6.3 | 47 | 85.4 | 4.1 | 7.1 |
| LATAM           | 14.6 | 18.2 | 980  | 7.8 | 41 | 81.9 | 2.9 | 6.3 |
| ANZ             | 9.3  | 7.4  | 340  | 3.9 | 61 | 92.5 | 1.8 | 7.9 |
| Nordics         | 8.1  | 6.2  | 290  | 3.1 | 64 | 93.8 | 1.5 | 8.1 |
| Southern Europe | 11.4 | 8.9  | 510  | 5.6 | 49 | 86.1 | 2.2 | 6.9 |
| Middle East     | 6.7  | 22.1 | 410  | 8.4 | 38 | 78.3 | 1.1 | 5.8 |
```

::::

## Matrix highlight

The `{table}` directive's `:matrix:` option highlights the whole row *and* column of the hovered cell. It only makes sense for lookup-style tables: a header row across the top and a row-heading first column — like the sprint velocity table below — where readers cross-reference a row label against a column label to find a value. In the fullscreen view this is enabled for every table.

:::::{tab-set}

::::{tab-item} Output
:::{table}
:matrix:

| Team | Sprint 1 | Sprint 2 | Sprint 3 | Sprint 4 | Sprint 5 | Sprint 6 | Sprint 7 | Sprint 8 |
|------|-----------|-----------|-----------|-----------|-----------|-----------|-----------|-----------|
| Team Falcon | 34 | 38 | 41 | 36 | 44 | 47 | 42 | 50 |
| Team Nimbus | 28 | 31 | 27 | 33 | 30 | 35 | 38 | 40 |
| Team Orbit  | 45 | 42 | 48 | 51 | 47 | 53 | 55 | 58 |
| Team Vertex | 22 | 25 | 24 | 28 | 26 | 30 | 29 | 33 |
| Team Atlas  | 38 | 40 | 37 | 42 | 45 | 43 | 48 | 46 |
| Team Comet  | 31 | 29 | 34 | 32 | 36 | 38 | 35 | 39 |
| Team Pulse  | 50 | 52 | 49 | 55 | 58 | 54 | 60 | 57 |
| Team Nova   | 19 | 22 | 21 | 25 | 23 | 27 | 26 | 29 |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:matrix:

| Team | Sprint 1 | Sprint 2 | Sprint 3 | Sprint 4 | Sprint 5 | Sprint 6 | Sprint 7 | Sprint 8 |
|------|-----------|-----------|-----------|-----------|-----------|-----------|-----------|-----------|
| Team Falcon | 34 | 38 | 41 | 36 | 44 | 47 | 42 | 50 |
| Team Nimbus | 28 | 31 | 27 | 33 | 30 | 35 | 38 | 40 |
| Team Orbit  | 45 | 42 | 48 | 51 | 47 | 53 | 55 | 58 |
| Team Vertex | 22 | 25 | 24 | 28 | 26 | 30 | 29 | 33 |
| Team Atlas  | 38 | 40 | 37 | 42 | 45 | 43 | 48 | 46 |
| Team Comet  | 31 | 29 | 34 | 32 | 36 | 38 | 35 | 39 |
| Team Pulse  | 50 | 52 | 49 | 55 | 58 | 54 | 60 | 57 |
| Team Nova   | 19 | 22 | 21 | 25 | 23 | 27 | 26 | 29 |
:::
```
::::

:::::

## Table directive with column widths

The `{table}` directive wraps a pipe table and lets you control column widths using a 12-unit grid system (similar to Bootstrap). Use the `:widths:` option to specify how space is distributed across columns.

### Presets

- **`auto`** (default): Browser determines column widths based on content. Omit `:widths:` or set it to `auto`.
- **`description`**: First column 4 units, second column 8 units. Ideal for term/description tables.

### Custom widths

Use dash-separated integers that sum to 12. Each number is the grid units for that column:

- `4-8` — two columns: 33% and 67%
- `4-4-4` — three equal columns
- `3-3-3-3` — four equal columns

### Description preset (term/description)

Ideal for glossaries, API parameter tables, or key-value lists where the first column is shorter.

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: description

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
:widths: description

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

### Auto preset (default)

When `:widths:` is omitted or set to `auto`, the browser determines column widths based on content. No column width constraints are applied.

:::::{tab-set}

::::{tab-item} Output
:::{table}
:widths: auto

| Left | Center | Right |
| --- | --- | --- |
| 1 | 2 | 3 |
:::
::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: auto

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
