import { mergeHTMLPlugin } from './hljs-merge-html-plugin'
import { LanguageFn } from 'highlight.js'
import hljs from 'highlight.js/lib/core'
import { $$optional } from 'select-dom'

// highlight.js language modules and the esql plugin default-export the LanguageFn.
// Parcel's dynamic import() resolves to the module's exports directly (the function),
// whereas other bundlers/test runners (Babel/Jest) wrap it as a namespace with a
// `default`. Normalize both so registerLanguage always receives the function.
export function toLanguageFn(mod: unknown): LanguageFn {
    const m = mod as { default?: LanguageFn }
    return (typeof mod === 'function' ? mod : m.default) as LanguageFn
}

// Each entry lazily imports one highlight.js language module (or the esql plugin) so
// only the languages actually present on a page are downloaded, instead of eagerly
// bundling all of them into the entry chunk.
const languageLoaders: Record<string, () => Promise<LanguageFn>> = {
    asciidoc: () =>
        import('highlight.js/lib/languages/asciidoc').then(toLanguageFn),
    bash: () => import('highlight.js/lib/languages/bash').then(toLanguageFn),
    c: () => import('highlight.js/lib/languages/c').then(toLanguageFn),
    csharp: () =>
        import('highlight.js/lib/languages/csharp').then(toLanguageFn),
    css: () => import('highlight.js/lib/languages/css').then(toLanguageFn),
    dockerfile: () =>
        import('highlight.js/lib/languages/dockerfile').then(toLanguageFn),
    dos: () => import('highlight.js/lib/languages/dos').then(toLanguageFn),
    ebnf: () => import('highlight.js/lib/languages/ebnf').then(toLanguageFn),
    esql: () => import('@elastic/highlightjs-esql').then(toLanguageFn),
    go: () => import('highlight.js/lib/languages/go').then(toLanguageFn),
    gradle: () =>
        import('highlight.js/lib/languages/gradle').then(toLanguageFn),
    groovy: () =>
        import('highlight.js/lib/languages/groovy').then(toLanguageFn),
    handlebars: () =>
        import('highlight.js/lib/languages/handlebars').then(toLanguageFn),
    http: () => import('highlight.js/lib/languages/http').then(toLanguageFn),
    ini: () => import('highlight.js/lib/languages/ini').then(toLanguageFn),
    java: () => import('highlight.js/lib/languages/java').then(toLanguageFn),
    javascript: () =>
        import('highlight.js/lib/languages/javascript').then(toLanguageFn),
    json: () => import('highlight.js/lib/languages/json').then(toLanguageFn),
    kotlin: () =>
        import('highlight.js/lib/languages/kotlin').then(toLanguageFn),
    markdown: () =>
        import('highlight.js/lib/languages/markdown').then(toLanguageFn),
    nginx: () => import('highlight.js/lib/languages/nginx').then(toLanguageFn),
    php: () => import('highlight.js/lib/languages/php').then(toLanguageFn),
    plaintext: () =>
        import('highlight.js/lib/languages/plaintext').then(toLanguageFn),
    powershell: () =>
        import('highlight.js/lib/languages/powershell').then(toLanguageFn),
    properties: () =>
        import('highlight.js/lib/languages/properties').then(toLanguageFn),
    python: () =>
        import('highlight.js/lib/languages/python').then(toLanguageFn),
    ruby: () => import('highlight.js/lib/languages/ruby').then(toLanguageFn),
    rust: () => import('highlight.js/lib/languages/rust').then(toLanguageFn),
    scala: () => import('highlight.js/lib/languages/scala').then(toLanguageFn),
    shell: () => import('highlight.js/lib/languages/shell').then(toLanguageFn),
    sql: () => import('highlight.js/lib/languages/sql').then(toLanguageFn),
    swift: () => import('highlight.js/lib/languages/swift').then(toLanguageFn),
    typescript: () =>
        import('highlight.js/lib/languages/typescript').then(toLanguageFn),
    xml: () => import('highlight.js/lib/languages/xml').then(toLanguageFn),
    yaml: () => import('highlight.js/lib/languages/yaml').then(toLanguageFn),
}

// Alias -> canonical language name. Aliases are registered with hljs once their
// canonical module has loaded so `language-sh` etc. resolve correctly.
const languageAliases: Record<string, string> = {
    sh: 'shell',
}

// Caches the registration promise per canonical language so concurrent code blocks
// (and later htmx swaps) share a single import instead of re-fetching the module.
const registrations = new Map<string, Promise<void>>()

function resolveLanguageName(name: string): string {
    return languageAliases[name] ?? name
}

// Imports and registers a single language (plus any aliases pointing at it) exactly
// once. Unknown names resolve to undefined and are skipped by the caller.
function ensureLanguage(name: string): Promise<void> | undefined {
    const canonical = resolveLanguageName(name)
    const loader = languageLoaders[canonical]
    if (!loader) return undefined

    const cached = registrations.get(canonical)
    if (cached) return cached

    const registration = loader().then((languageFn) => {
        hljs.registerLanguage(canonical, languageFn)
        for (const [alias, target] of Object.entries(languageAliases)) {
            if (target === canonical) {
                hljs.registerAliases([alias], { languageName: canonical })
            }
        }
    })
    registrations.set(canonical, registration)
    return registration
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

function getLanguageFromClassList(element: Element): string | undefined {
    for (const className of element.classList) {
        if (className.startsWith('language-')) {
            return className.slice('language-'.length)
        }
    }
    return undefined
}

export async function initHighlight() {
    const blocks = $$optional(
        '#markdown-content pre code:not([data-highlighted])'
    )
    if (blocks.length === 0) return

    // Import only the language modules referenced by the unprocessed blocks before
    // highlighting. Unknown/plain-text languages resolve to undefined and are skipped;
    // hljs.highlightElement then degrades gracefully without breaking other blocks.
    const requiredLanguages = new Set<string>()
    for (const block of blocks) {
        const language = getLanguageFromClassList(block)
        if (language) requiredLanguages.add(language)
    }

    await Promise.all(
        [...requiredLanguages]
            .map((language) => ensureLanguage(language))
            .filter(
                (promise): promise is Promise<void> => promise !== undefined
            )
    )

    blocks.forEach(hljs.highlightElement)
}

// Export the configured hljs instance for reuse
export { hljs }
