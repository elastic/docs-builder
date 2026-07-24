import { Marked, Renderer, RendererObject, Tokens } from 'marked'

const defaultRenderer = new Renderer()

const renderer: RendererObject = {
    code(token: Tokens.Code): string {
        return `<div class="highlight">${defaultRenderer.code.call(this, token)}</div>`
    },
    table(token: Tokens.Table): string {
        return `<div class="table-wrapper">${defaultRenderer.table.call(this, token)}</div>`
    },
}

export const markdownRenderer = new Marked({ renderer })
