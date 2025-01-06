---
title: Tables
---

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

% Neither of these work
% ## `{table}` Directive
%
% You can use the `table` directive which gives you configuration captions such as caption and alignment.
%
% ```{table} Countries and their capitals
% :widths: auto
% :align: right
%
% | Country | Capital         |
% | ------- | --------------- |
% | USA     | Washington D.C. |
% | Canada  | Ottawa          |
% ``` -->
%
% <!-- ## `{list-table}` Directive
%
% You can also use the `list-table` directive to create a table from data.
%
% ```{list-table} Frozen Delights!
% :widths: 15 10 30
% :header-rows: 1
%
% *   - Treat
%     - Quantity
%     - Description
% *   - Albatross
%     - 2.99
%     - On a stick!
% *   - Crunchy Frog
%     - 1.49
%     - If we took the bones out, it wouldn't be
%  crunchy, now would it?
% *   - Gannet Ripple
%     - 1.99
%     - On a stick!
% ```
