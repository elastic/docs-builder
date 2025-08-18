# CSV files

The `{csv-file}` directive allows you to include and render CSV files as formatted tables in your documentation. The directive automatically parses CSV content and renders it using the standard table styles defined in `table.css`.

## Usage

:::::{tab-set}

::::{tab-item} Output

:::{csv-file} ../_snippets/sample-data.csv
:caption: Sample user data from the database
:::

::::

::::{tab-item} Markdown

```markdown
:::{csv-file} _snippets/sample-data.csv
:::
```

::::

:::::

## Options

The CSV file directive supports several options to customize the table rendering:

### Caption

Add a descriptive caption above the table:

```markdown
:::{csv-file} _snippets/sample-data.csv
:caption: Sample user data from the database
:::
```

### Custom separator

Specify a custom field separator (default is comma):

```markdown
:::{csv-file} _snippets/sample-data.csv
:separator: ;
:::
```

### Size and performance limits

Control how much data is loaded and displayed:

```markdown
:::{csv-file} _snippets/large-dataset.csv
:max-rows: 5000
:max-columns: 50
:max-size: 20MB
:::
```

- **max-rows**: Maximum number of rows to display (default: 10,000)
- **max-columns**: Maximum number of columns to display (default: 100)  
- **max-size**: Maximum file size to process (default: 10MB). Supports KB, MB, GB units.

### Preview mode

For very large files, enable preview mode to show only the first 100 rows:

```markdown
:::{csv-file} _snippets/huge-dataset.csv
:preview-only: true
:::
```

## Performance considerations

The CSV directive is optimized for large files:

- Files are processed using streaming to avoid loading everything into memory
- Size validation prevents processing of files that exceed the specified limits
- Row and column limits protect against accidentally rendering massive tables
- Warning messages are displayed when limits are exceeded

For optimal performance with large CSV files, consider:
- Setting appropriate `max-rows` and `max-columns` limits
- Using `preview-only: true` for exploratory data viewing
- Increasing `max-size` only when necessary
