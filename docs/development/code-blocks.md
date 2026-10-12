---
navigation_title: Code blocks
---

# Code blocks

Markdown fences and the API explorer render code blocks through one shared markup contract. Each side emits the markup with its own code (`Code.cshtml` for Markdown, `_ApiCodeBlock.cshtml` for the API explorer). The styling and behaviour are shared:

| Concern | File |
|---|---|
| Layout, syntax colours, callouts, line-number gutter, header copy button | `Assets/code-block.css` |
| Syntax highlighting (client side) | `Assets/hljs.ts` |
| Copy button | `Assets/copybutton.ts` |
| Line-number gutter | `Assets/code-line-numbers.ts` |

## Markup contract

```html
<div class="highlight-{lang} notranslate" data-line-numbers>
  <div class="highlight">
    <pre><code class="language-{lang}">...</code></pre>
  </div>
</div>
```

`data-line-numbers` is optional. It adds a non-selectable line-number gutter after highlighting.

A block that owns a header, such as the API examples rail, wraps itself in a card:

| Attribute | Purpose |
|---|---|
| `data-code-card` | The card container. |
| `data-code-actions` | Header slot. The copy button mounts here instead of over the code. |
| `data-code-panel="key"` | One of several switchable panels, such as a language or a status code. The copy button follows its panel's key and `hidden` state. |

## Theming

All colours and metrics are `--code-*` custom properties declared on `:root` in `code-block.css`. Override them on any ancestor to restyle a block or card. Syntax token colours use `--code-token-*`.

Spacing around a block belongs to the surrounding context, not the component. Prose spacing lives in `Assets/markdown/code.css`, and the API card sets its own.
