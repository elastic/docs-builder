# Tables

A table is an arrangement of data with rows and columns. Each row consists of cells containing arbitrary text in which inlines are parsed, separated by pipes `|`. The rows of a table consist of:

* a single header row
* a delimiter row separating the header from the data
* zero or more data rows

## Notes

* A leading and trailing pipe is recommended for clarity of reading
* Spaces between pipes and cell content are trimmed
* Block-level elements cannot be inserted in a table

## GitHub Flavored Markdown (GFM) Table

You can use the GFM table syntax to create a table:

```
| Country | Capital         |
| ------- | --------------- |
| USA     | Washington D.C. |
| Canada  | Ottawa          |
```

| Country | Capital         |
| ------- | --------------- |
| USA     | Washington D.C. |
| Canada  | Ottawa          |
