# Keyboard shortcuts

You can represent keyboard keys and shortcuts in your documentation using the `{kbd}` role. This is useful for showing keyboard commands and shortcuts in a visually consistent way.

## Basic usage

To display a keyboard key, use the syntax `` {kbd}`key-name` ``. For example, writing `` {kbd}`Enter` `` will render as a styled keyboard key.

::::{tab-set}

:::{tab-item} Output
Press {kbd}`Enter` to submit.
:::

:::{tab-item} Markdown
```markdown
Press {kbd}`Enter` to submit.
```
:::

::::

## Keyboard combinations

You can represent keyboard combinations by joining multiple `{kbd}` roles with a plus sign (+).

::::{tab-set}

:::{tab-item} Output
{kbd}`ctrl` + {kbd}`C` to copy text.

{kbd}`Shift` + {kbd}`Alt` + {kbd}`F` to format the document.
:::

:::{tab-item} Markdown
```markdown
{kbd}`Ctrl` + {kbd}`C` to copy text.

{kbd}`Shift` + {kbd}`Alt` + {kbd}`F` to format the document.
```
:::

::::

## Common shortcuts by platform

Here are some common keyboard shortcuts across different platforms:

::::{tab-set}

:::{tab-item} Output
| Mac                     | Windows/Linux              | Description                 |
|-------------------------|----------------------------|-----------------------------|
| {kbd}`⌘` + {kbd}`C`     | {kbd}`Ctrl` + {kbd}`C`     | Copy                        |
| {kbd}`⌘` + {kbd}`V`     | {kbd}`Ctrl` + {kbd}`V`     | Paste                       |
| {kbd}`⌘` + {kbd}`Z`     | {kbd}`Ctrl` + {kbd}`Z`     | Undo                        |
| {kbd}`⌘` + {kbd}`Enter` | {kbd}`Ctrl` + {kbd}`Enter` | Run a query                 |
| {kbd}`⌘` + {kbd}`/`     | {kbd}`Ctrl` + {kbd}`/`     | Comment or uncomment a line |
:::

:::{tab-item} Markdown
```markdown
| Mac                     | Windows/Linux              | Description                 |
|-------------------------|----------------------------|-----------------------------|
| {kbd}`⌘` + {kbd}`C`     | {kbd}`Ctrl` + {kbd}`C`     | Copy                        |
| {kbd}`⌘` + {kbd}`V`     | {kbd}`Ctrl` + {kbd}`V`     | Paste                       |
| {kbd}`⌘` + {kbd}`Z`     | {kbd}`Ctrl` + {kbd}`Z`     | Undo                        |
| {kbd}`⌘` + {kbd}`Enter` | {kbd}`Ctrl` + {kbd}`Enter` | Run a query                 |
| {kbd}`⌘` + {kbd}`/`     | {kbd}`Ctrl` + {kbd}`/`     | Comment or uncomment a line |
```
:::

::::

## Special keys

Some commonly used special keys:

::::{tab-set}

:::{tab-item} Output
| Symbol    | Key Description  |
|-----------|------------------|
| {kbd}`⌘`  | Command (Mac)    |
| {kbd}`⌥`  | Option/Alt (Mac) |
| {kbd}`⇧`  | Shift            |
| {kbd}`⌃`  | Control          |
| {kbd}`↩`  | Return/Enter     |
| {kbd}`⌫`  | Delete/Backspace |
| {kbd}`⇥`  | Tab              |
| {kbd}`↑`  | Up Arrow         |
| {kbd}`↓`  | Down Arrow       |
| {kbd}`←`  | Left Arrow       |
| {kbd}`→`  | Right Arrow      |
| {kbd}`⎋`  | Escape           |
:::

:::{tab-item} Markdown
```markdown
| Symbol    | Key Description  |
|-----------|------------------|
| {kbd}`⌘`  | Command (Mac)    |
| {kbd}`⌥`  | Option/Alt (Mac) |
| {kbd}`⇧`  | Shift            |
| {kbd}`⌃`  | Control          |
| {kbd}`↩`  | Return/Enter     |
| {kbd}`⌫`  | Delete/Backspace |
| {kbd}`⇥`  | Tab              |
| {kbd}`↑`  | Up Arrow         |
| {kbd}`↓`  | Down Arrow       |
| {kbd}`←`  | Left Arrow       |
| {kbd}`→`  | Right Arrow      |
| {kbd}`⎋`  | Escape           |
```
:::

::::
