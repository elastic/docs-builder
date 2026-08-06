import { mergeHTMLPlugin } from './hljs-merge-html-plugin'
import { LanguageFn } from 'highlight.js'
import hljs from 'highlight.js/lib/core'

// highlight.js language modules and the esql plugin default-export the LanguageFn.
// Parcel's dynamic import() resolves to the module's exports directly (the function),
// whereas other bundlers/test runners (Babel/Jest) wrap it as a namespace with a
// `default`. Normalize both so registerLanguage always receives the function.
export function toLanguageFn(mod: unknown): LanguageFn {
    const m = mod as { default?: LanguageFn }
    return (typeof mod === 'function' ? mod : m.default) as LanguageFn
}

const CODE_BLOCK_SELECTOR = 'pre code:not([data-highlighted])'

let allLanguagesReady: Promise<void> | null = null

function ensureHighlightReady(): Promise<void> {
    if (allLanguagesReady) return allLanguagesReady

    allLanguagesReady = import('./hljs-languages').then(({ languages }) => {
        for (const [name, languageFn] of Object.entries(languages)) {
            hljs.registerLanguage(name, languageFn)
        }
        hljs.registerAliases(['sh'], { languageName: 'shell' })
    })

    return allLanguagesReady
}

export async function highlightCodeBlocks(root: ParentNode): Promise<void> {
    const blocks = root.querySelectorAll<HTMLElement>(CODE_BLOCK_SELECTOR)
    if (blocks.length === 0) return

    await ensureHighlightReady()

    for (const block of blocks) {
        if (!block.dataset.highlighted) hljs.highlightElement(block)
    }
}

export async function initHighlight(): Promise<void> {
    const root = document.querySelector('#markdown-content')
    if (!root) return
    await highlightCodeBlocks(root)
}

hljs.registerLanguage('apiheader', function () {
    return {
        case_insensitive: true, // language is case-insensitive
        keywords: 'GET POST PUT DELETE HEAD OPTIONS PATCH',
        contains: [
            hljs.HASH_COMMENT_MODE,
            {
                className: 'subst', // (pathname: path1/path2/dothis) color #ab5656
                begin: /(?<=(?:\/|GET |POST |PUT |DELETE |HEAD |OPTIONS |PATH))[^?\n\r/]+/,
            },
        ],
    }
})

const decimalDigits = '[0-9](_?[0-9])*'
const frac = `\\.(${decimalDigits})`
const decimalInteger = `0|[1-9](_?[0-9])*|0[0-7]*[89][0-9]*`
const NUMBER = {
    className: 'number',
    variants: [
        { begin: `\\b(${decimalInteger})\\b((${frac})\\b|\\.)?|(${frac})\\b` },
        { begin: `\\b(0|[1-9](_?[0-9])*)n\\b` },
        { begin: '\\b0[xX][0-9a-fA-F](_?[0-9a-fA-F])*n?\\b' },
        { begin: '\\b0[bB][0-1](_?[0-1])*n?\\b' },
        { begin: '\\b0[oO][0-7](_?[0-7])*n?\\b' },
        { begin: '\\b0[0-7]+n?\\b' },
    ],
    relevance: 0,
}

hljs.registerLanguage('eql', function () {
    return {
        case_insensitive: true, // language is case-insensitive
        keywords: {
            keyword: 'where sequence sample untill and or not in in~',
            literal: ['false', 'true', 'null'],
            subst: 'add between cidrMatch concat divide endsWith indexOf length modulo multiply number startsWith string stringContains substring subtract',
        },
        contains: [
            hljs.QUOTE_STRING_MODE,
            hljs.C_LINE_COMMENT_MODE,
            {
                scope: 'operator', // (pathname: path1/path2/dothis) color #ab5656
                match: /(?:<|<=|==|:|!=|>=|>|like~?|regex~?)/,
            },
            {
                scope: 'punctuation', // (pathname: path1/path2/dothis) color #ab5656
                match: /(?:!?\[|\]|\|)/,
            },
            NUMBER,
        ],
    }
})

hljs.registerLanguage('painless', function () {
    return {
        case_insensitive: true, // language is case-insensitive
        keywords: {
            keyword: 'where sequence sample untill and or not in in~',
            literal: ['false', 'true', 'null'],
            subst: 'add between cidrMatch concat divide endsWith indexOf length modulo multiply number startsWith string stringContains substring subtract',
        },
        contains: [
            hljs.QUOTE_STRING_MODE,
            hljs.C_LINE_COMMENT_MODE,
            {
                scope: 'operator', // (pathname: path1/path2/dothis) color #ab5656
                match: /(?:<|<=|==|:|!=|>=|>|like~?|regex~?)/,
            },
            {
                scope: 'punctuation', // (pathname: path1/path2/dothis) color #ab5656
                match: /(?:!?\[|\]|\|)/,
            },
            NUMBER,
        ],
    }
})

hljs.registerLanguage('kuery', function () {
    return {
        case_insensitive: true,
        keywords: {
            keyword: 'and or not',
            literal: ['true', 'false', 'null'],
        },
        contains: [
            // Field names followed by : or range operators
            {
                scope: 'attribute',
                match: /[a-zA-Z_][a-zA-Z0-9._]*(?=\s*(?::|<=|>=|<|>))/,
            },
            // Quoted strings
            {
                scope: 'string',
                begin: /"/,
                end: /"/,
                contains: [
                    {
                        scope: 'char.escape',
                        match: /\\[\\"\t\r\n]|\\u[0-9a-fA-F]{4}/,
                    },
                ],
            },
            // Range and match operators
            {
                scope: 'operator',
                match: /<=|>=|<|>|:/,
            },
            // Wildcards
            {
                scope: 'operator',
                match: /\*/,
            },
            // Parentheses and braces (grouping / nested queries)
            {
                scope: 'punctuation',
                match: /[(){}]/,
            },
            NUMBER,
        ],
    }
})
hljs.registerAliases(['kql'], { languageName: 'kuery' })

hljs.addPlugin(mergeHTMLPlugin)

// The unescaped HTML warning is caused by the mergeHTMLPlugin which we are using
// for code callouts
hljs.configure({ ignoreUnescapedHTML: true })

// Export the configured hljs instance for reuse
export { hljs }
