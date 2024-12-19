---
title: Tables
---

## GFM Table

You can use the GFM (GitHub Flavored Markdown) table syntax to create a table.

| Country | Capital         |
| ------- | --------------- |
| USA     | Washington D.C. |
| Canada  | Ottawa          |

## `{table}` Directive

You can use the `table` directive which gives you configuration captions such as caption and alignment.

```{table} Countries and their capitals
:widths: auto
:align: center

| Country | Capital         |
| ------- | --------------- |
| USA     | Washington D.C. |
| Canada  | Ottawa          |
```

## `{list-table}` Directive

You can also use the `list-table` directive to create a table from data.

```{list-table} Frozen Delights!
:widths: 15 10 30
:header-rows: 1

*   - Treat
    - Quantity
    - Description
*   - Albatross
    - 2.99
    - On a stick!
*   - Crunchy Frog
    - 1.49
    - If we took the bones out, it wouldn't be
 crunchy, now would it?
*   - Gannet Ripple
    - 1.99
    - On a stick!
```
