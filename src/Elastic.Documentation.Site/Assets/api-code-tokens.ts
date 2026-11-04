const IDENT_LANG =
    /(?:^|\s)language-(?:javascript|js|typescript|ts|python|ruby|php|java)(?:\s|$)/

const IDENTIFIER = /[A-Za-z_$][\w$]*/g

/**
 * highlight.js bundles quotes into `.hljs-string` / `.hljs-attr` and leaves
 * identifiers in the same text node as `=` / `(`. Split those so the API
 * examples rail can color keywords, variables, properties, strings, and signs
 * the same way across JS, Python, Ruby, PHP, Java, Console, curl, and JSON.
 */
export function decorateApiCodeTokens(root: ParentNode = document): void {
    root.querySelectorAll<HTMLElement>('.api-code-card pre code').forEach(
        (code) => {
            if (code.dataset.highlighted !== 'yes') return
            markQuotedPropertyKeys(code)
            splitWrappingQuotes(code)
            splitTrailingSymbolColons(code)
            reclassifyKeywordAssignments(code)
            if (IDENT_LANG.test(code.className)) wrapBareIdentifiers(code)
        }
    )
}

export const decorateApiJsTokens = decorateApiCodeTokens

function markQuotedPropertyKeys(code: HTMLElement): void {
    code.querySelectorAll('.hljs-string').forEach((el) => {
        if (el.classList.contains('hljs-attr')) return
        if (!isKeySeparator(nextNonSpaceText(el))) return
        el.classList.remove('hljs-string')
        el.classList.add('hljs-attr')
    })
}

function splitWrappingQuotes(code: HTMLElement): void {
    code.querySelectorAll('.hljs-string, .hljs-attr').forEach((el) => {
        const text = el.textContent ?? ''
        if (text.length < 2) return
        const quote = text[0]
        if (!isQuote(quote) || !text.endsWith(quote)) return
        el.textContent = text.slice(1, -1)
        el.before(punctuationSpan(quote))
        el.after(punctuationSpan(quote))
    })
}

function splitTrailingSymbolColons(code: HTMLElement): void {
    code.querySelectorAll('.hljs-symbol').forEach((el) => {
        const text = el.textContent ?? ''
        if (!text.endsWith(':') || text.length < 2) return
        el.className = 'hljs-attr'
        el.textContent = text.slice(0, -1)
        el.after(punctuationSpan(':'))
    })
}

function reclassifyKeywordAssignments(code: HTMLElement): void {
    code.querySelectorAll('.hljs-keyword').forEach((el) => {
        if (!nextNonSpaceText(el).startsWith('=')) return
        el.classList.remove('hljs-keyword')
        el.classList.add('hljs-attr')
    })
}

function wrapBareIdentifiers(code: HTMLElement): void {
    const nodes = [...code.childNodes]
    for (const node of nodes) {
        if (node.nodeType !== Node.TEXT_NODE) continue
        const text = node.textContent ?? ''
        if (!/[A-Za-z_$]/.test(text)) continue

        const frag = document.createDocumentFragment()
        let last = 0
        for (const match of text.matchAll(IDENTIFIER)) {
            const start = match.index ?? 0
            if (start > last) frag.append(text.slice(last, start))
            const span = document.createElement('span')
            span.className = identifierClass(
                text.slice(start + match[0].length)
            )
            span.textContent = match[0]
            frag.append(span)
            last = start + match[0].length
        }
        if (last < text.length) frag.append(text.slice(last))
        node.replaceWith(frag)
    }
}

function identifierClass(after: string): string {
    const rest = after.trimStart()
    if (rest.startsWith('(')) return 'hljs-title function_'
    if (after.startsWith('=')) return 'hljs-attr'
    return 'hljs-variable'
}

function isKeySeparator(text: string): boolean {
    return text.startsWith(':') || text.startsWith('=>')
}

function isQuote(char: string): boolean {
    return char === '"' || char === "'" || char === '`'
}

function punctuationSpan(char: string): HTMLSpanElement {
    const span = document.createElement('span')
    span.className = 'hljs-punctuation'
    span.textContent = char
    return span
}

function nextNonSpaceText(el: Element): string {
    let next = el.nextSibling
    while (next) {
        const value = next.textContent ?? ''
        if (next.nodeType === Node.TEXT_NODE) {
            const trimmed = value.trimStart()
            if (trimmed.length > 0) return trimmed
        } else if (value.trim().length > 0) {
            return ''
        }
        next = next.nextSibling
    }
    return ''
}
