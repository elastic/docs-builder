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

## Column widths

By default, column widths are determined automatically by the browser based on content. To control column widths explicitly, wrap the table in a `{table}` directive with the `:widths:` option.

The `:widths:` option takes space-separated integers representing relative column widths. These are normalized to percentages. For example, `:widths: 30 70` produces columns at 30% and 70%, while `:widths: 1 2 3` produces columns at ~17%, ~33%, and ~50%.

### Fixed-width columns

This is useful when a table has a narrow label column alongside a wider description column, and you want the layout to remain consistent regardless of cell content:

:::::{tab-set}

::::{tab-item} Output

:::{table}
:widths: 25 75

| Setting              | Description                                                                                |
| -------------------- | ------------------------------------------------------------------------------------------ |
| `max_retries`        | The maximum number of times the client retries a failed request before returning an error.  |
| `timeout`            | How long the client waits for a response, in seconds. Set to `0` to turn off the timeout.   |
| `bulk_max_size`      | The maximum number of events to include in a single bulk API request.                       |
:::

::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: 25 75

| Setting              | Description                                                       |
| -------------------- | ----------------------------------------------------------------- |
| `max_retries`        | The maximum number of times the client retries a failed request.  |
| `timeout`            | How long the client waits for a response, in seconds.             |
| `bulk_max_size`      | The maximum number of events in a single bulk API request.        |
:::
```
::::

:::::

### Even distribution

Use equal values to distribute columns evenly, regardless of content length:

:::::{tab-set}

::::{tab-item} Output

:::{table}
:widths: 1 1 1

| Plan       | Storage | Support            |
| ---------- | ------- | ------------------ |
| Standard   | 8 GB    | Community forum    |
| Gold       | 150 GB  | Email, 24h SLA     |
| Platinum   | 3 TB    | Dedicated engineer |
:::

::::

::::{tab-item} Markdown
```markdown
:::{table}
:widths: 1 1 1

| Plan       | Storage | Support            |
| ---------- | ------- | ------------------ |
| Standard   | 8 GB    | Community forum    |
| Gold       | 150 GB  | Email, 24h SLA     |
| Platinum   | 3 TB    | Dedicated engineer |
:::
```
::::

:::::

### Table caption

You can add a caption by providing it as the directive argument:

:::::{tab-set}

::::{tab-item} Output

:::{table} Cluster health indicators
:widths: 20 15 65

| Indicator       | Status  | Description                                                        |
| --------------- | ------- | ------------------------------------------------------------------ |
| Disk            | green   | All shards have enough available disk space.                       |
| SLM             | yellow  | One or more snapshot lifecycle policies have not run on schedule.   |
| Repository      | red     | One or more snapshot repositories are not accessible.              |
:::

::::

::::{tab-item} Markdown
```markdown
:::{table} Cluster health indicators
:widths: 20 15 65

| Indicator       | Status  | Description                                           |
| --------------- | ------- | ----------------------------------------------------- |
| Disk            | green   | All shards have enough available disk space.           |
| SLM             | yellow  | One or more SLM policies have not run on schedule.    |
| Repository      | red     | One or more snapshot repositories are not accessible.  |
:::
```
::::

:::::

:::{note}

* The number of values in `:widths:` must match the number of columns in the table.
* Use `:widths: auto` to explicitly delegate column sizing to the browser (the default behavior).
* The `{table}` directive can also be used without `:widths:` — for example, to add a caption only.

:::

## Responsive table

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
