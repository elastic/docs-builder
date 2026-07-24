import { Marked, RendererObject, Tokens } from 'marked'

function escapeHtml(text: string): string {
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
}

export function createMarkdownRenderer(): Marked {
    const renderer: RendererObject = {
        code({ text, lang }: Tokens.Code): string {
            const cls = lang ? ` class="language-${lang}"` : ''
            return `<div class="highlight"><pre><code${cls}>${escapeHtml(text)}</code></pre></div>`
        },
        table(token: Tokens.Table): string {
            const defaultMarked = new Marked()
            const defaultTableHtml = defaultMarked.parse(token.raw)
            return `<div class="table-wrapper">${defaultTableHtml}</div>`
        },
    }
    return new Marked({ renderer })
}
