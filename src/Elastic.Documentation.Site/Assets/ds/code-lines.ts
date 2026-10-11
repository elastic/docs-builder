// Wraps each line of a highlighted code block in its own element, so CSS can number lines, mark a shell prompt and tint a row.
// Tokens that span lines (a multi-line string or comment) are closed at the end of one line and reopened on the next.
// The newline stays inside the line, so the text content, and the copy button, are unchanged.
const TOKEN = /<\/?[^>]+>|\n|[^<\n]+/g
const LINE_CLASS = 'code-block__line'
const NUMBERED_FROM = 6
const SHELL = new Set(['sh', 'bash', 'shell', 'zsh', 'console'])

export function splitLines(html: string): string[] {
    const open: string[] = []
    const lines: string[] = []
    let line = ''

    for (const token of html.match(TOKEN) ?? []) {
        if (token === '\n') {
            lines.push(line + '</span>'.repeat(open.length) + '\n')
            line = open.join('')
        } else {
            if (token.startsWith('</')) open.pop()
            else if (token.startsWith('<')) open.push(token)
            line += token
        }
    }
    if (line.replace(/<[^>]+>/g, '') !== '')
        lines.push(line + '</span>'.repeat(open.length))

    return lines.map((l) => `<span class="${LINE_CLASS}">${l}</span>`)
}

export function initCodeLines(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '.code-block pre > code:only-child:not([data-lines])'
        )
        .forEach((code) => {
            const lines = splitLines(code.innerHTML)
            code.innerHTML = lines.join('')
            code.dataset.lines = String(lines.length)
            const block = code.closest<HTMLElement>('.code-block')
            if (!block) return
            if (lines.length >= NUMBERED_FROM) block.dataset.numbered = ''
            if (lines.length === 1 && SHELL.has(block.dataset.language ?? ''))
                block.dataset.prompt = ''
        })
}
