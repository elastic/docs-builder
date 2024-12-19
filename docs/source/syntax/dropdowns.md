---
title: Dropdowns
---

Dropdowns allow you to hide and reveal content on user interaction.

### Syntax

By default, dropdowns are collapsed. This hides content until a user clicks the title of the collapsible block.

```markdown
:::{dropdown} Dropdown Title
Dropdown content
:::
```

:::{dropdown} Dropdown Title
Dropdown content
:::

### Asciidoc syntax

```asciidoc
.The `elasticsearch-setup-passwords` tool is deprecated.
[%collapsible]
====
Details::
The `elasticsearch-setup-passwords` tool is deprecated in 8.0. To
manually reset the password for built-in users (including the `elastic` user), use
the {ref}/reset-password.html[`elasticsearch-reset-password`] tool, the {es}
{ref}/security-api-change-password.html[change passwords API], or the
User Management features in {kib}.
`elasticsearch-setup-passwords` will be removed in a future release.

Impact::
Passwords are generated automatically for the `elastic` user when you start {es}
for the first time. If you run `elasticsearch-setup-passwords` after
starting {es}, it will fail because the `elastic`
user password is already configured.
====
```

### Open by default

You can also specify that the content should be visible by default, but can also be collapsed by the user. Do this by specifying the `open` option.

```markdown
:::{dropdown} Dropdown Title
:open:
Dropdown content
:::
```

```{dropdown} Dropdown Title
:open:
Dropdown content
```