# CSV file directive

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
