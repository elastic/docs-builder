# Keyboard shortcuts

You can represent keyboard keys and shortcuts in your documentation using the `{kbd}` role. This is useful for showing keyboard commands and shortcuts in a visually consistent way.

## Basic usage

To display a keyboard key, use the syntax `` {kbd}`key-name` ``. For example, writing `` {kbd}`enter` `` will render as a styled keyboard key.

::::{tab-set}

:::{tab-item} Output
Press {kbd}`enter` to submit.
:::

:::{tab-item} Markdown
```markdown
Press {kbd}`enter` to submit.
```
:::

::::

## Combining keys

For keyboard shortcuts involving multiple keys, you can combine them within a single `{kbd}` role by separating the key names with a `+`.

::::{tab-set}

:::{tab-item} Output
Use {kbd}`cmd+shift+enter` to execute the command.
:::

:::{tab-item} Markdown
```markdown
Use {kbd}`cmd+shift+enter` to execute the command.
```
:::

::::

Alternatively, you can use multiple `{kbd}` roles to describe a shortcut. This approach is useful when you want to visually separate keys. Use a `+` to represent a combination and a `/` to represent alternative keys.

::::{tab-set}

:::{tab-item} Output
{kbd}`ctrl` + {kbd}`c` to copy text, or {kbd}`cmd` + {kbd}`c` on Mac.
:::

:::{tab-item} Markdown
```markdown
{kbd}`ctrl` + {kbd}`c` to copy text, or {kbd}`cmd` + {kbd}`c` on Mac.
```
:::

::::

::::{tab-set}

:::{tab-item} Output
{kbd}`ctrl` / {kbd}`cmd` + {kbd}`c` to copy text.
:::


:::{tab-item} Markdown
```markdown
{kbd}`ctrl` / {kbd}`cmd` + {kbd}`c` to copy text.
```
:::

::::

## Common shortcuts by platform

The platform-specific examples below demonstrate how to combine special keys and regular characters.

::::{tab-set}

:::{tab-item} Output

| Mac              | Windows/Linux     | Description                 |
|------------------|-------------------|-----------------------------|
| {kbd}`cmd+c`     | {kbd}`ctrl+c`     | Copy                        |
| {kbd}`cmd+v`     | {kbd}`ctrl+v`     | Paste                       |
| {kbd}`cmd+z`     | {kbd}`ctrl+z`     | Undo                        |
| {kbd}`cmd+enter` | {kbd}`ctrl+enter` | Run a query                 |
| {kbd}`cmd+/`     | {kbd}`ctrl+/`     | Comment or uncomment a line |

:::

:::{tab-item} Markdown
```markdown
| Mac              | Windows/Linux     | Description                 |
|------------------|-------------------|-----------------------------|
| {kbd}`cmd+c`     | {kbd}`ctrl+c`     | Copy                        |
| {kbd}`cmd+v`     | {kbd}`ctrl+v`     | Paste                       |
| {kbd}`cmd+z`     | {kbd}`ctrl+z`     | Undo                        |
| {kbd}`cmd+enter` | {kbd}`ctrl+enter` | Run a query                 |
| {kbd}`cmd+/`     | {kbd}`ctrl+/`     | Comment or uncomment a line |
```
:::

::::

## Available keys

The `{kbd}` role recognizes a set of special keywords for modifier, navigation, and function keys. Any other text will be rendered as a literal key.

Here is the full list of available keywords:

| Keyword     | Rendered Output  |
|-------------|------------------|
| `shift`     | {kbd}`shift`     |
| `ctrl`      | {kbd}`ctrl`      |
| `alt`       | {kbd}`alt`       |
| `option`    | {kbd}`option`    |
| `cmd`       | {kbd}`cmd`       |
| `win`       | {kbd}`win`       |
| `up`        | {kbd}`up`        |
| `down`      | {kbd}`down`      |
| `left`      | {kbd}`left`      |
| `right`     | {kbd}`right`     |
| `space`     | {kbd}`space`     |
| `tab`       | {kbd}`tab`       |
| `enter`     | {kbd}`enter`     |
| `esc`       | {kbd}`esc`       |
| `backspace` | {kbd}`backspace` |
| `del`       | {kbd}`delete`    |
| `ins`       | {kbd}`insert`    |
| `pageup`    | {kbd}`pageup`    |
| `pagedown`  | {kbd}`pagedown`  |
| `home`      | {kbd}`home`      |
| `end`       | {kbd}`end`       |
| `f1`        | {kbd}`f1`        |
| `f2`        | {kbd}`f2`        |
| `f3`        | {kbd}`f3`        |
| `f4`        | {kbd}`f4`        |
| `f5`        | {kbd}`f5`        |
| `f6`        | {kbd}`f6`        |
| `f7`        | {kbd}`f7`        |
| `f8`        | {kbd}`f8`        |
| `f9`        | {kbd}`f9`        |
| `f10`       | {kbd}`f10`       |
| `f11`       | {kbd}`f11`       |
| `f12`       | {kbd}`f12`       |
| `plus`      | {kbd}`plus`      |
| `fn`        | {kbd}`fn`        |
