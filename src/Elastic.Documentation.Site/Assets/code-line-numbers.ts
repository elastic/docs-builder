const GUTTER_TARGET = '[data-line-numbers] pre code'

function countLines(text: string): number {
    if (!text) return 1
    const parts = text.split(/\r?\n/)
    if (parts[parts.length - 1] === '') parts.pop()
    return Math.max(1, parts.length)
}

/**
 * Add a non-selectable line-number gutter to code blocks that opt in with
 * `data-line-numbers`. Uses a sibling <div> (font metrics come from the shared
 * --code-* variables in code-block.css) so numbers stay aligned and mouse
 * selection / copy omit them. Run after highlighting: the count comes from the
 * final textContent.
 */
export function initCodeLineNumbers(root: ParentNode = document): void {
    root.querySelectorAll<HTMLElement>(GUTTER_TARGET).forEach((code) => {
        const pre = code.parentElement
        if (!(pre instanceof HTMLPreElement)) return
        if (pre.parentElement?.classList.contains('code-lines')) return

        const wrapper = document.createElement('div')
        wrapper.className = 'code-lines'
        const gutter = document.createElement('div')
        gutter.className = 'code-line-gutter'
        gutter.setAttribute('aria-hidden', 'true')
        gutter.textContent = Array.from(
            { length: countLines(code.textContent ?? '') },
            (_, index) => String(index + 1)
        ).join('\n')

        pre.replaceWith(wrapper)
        wrapper.append(gutter, pre)
    })
}
