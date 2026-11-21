import type { LanguageFn, Mode } from 'highlight.js'

const methods = 'GET|POST|PUT|DELETE|HEAD|OPTIONS|PATCH|CONNECT|TRACE'

export function withHttpFileSupport(http: LanguageFn): LanguageFn {
    return (hljs) => {
        const variable: Mode = {
            scope: 'subst',
            begin: /\{\{/,
            end: /\}\}/,
        }
        const boundary = new RegExp(
            `(?=^[ \\t]*(?:#|//|@[A-Za-z_]|(?:${methods})[ \\t]))`,
            'm'
        )
        const header: Mode = {
            scope: 'attribute',
            begin: /^[A-Za-z][A-Za-z0-9-]*(?=:)/,
            starts: {
                end: /$/,
                contains: [{ scope: 'punctuation', begin: /:/ }, variable],
            },
        }
        const language = http(hljs)
        return {
            ...language,
            contains: [
                { scope: 'comment', begin: /^[ \t]*(?:#|\/\/)/, end: /$/ },
                { scope: 'variable', begin: /^@[A-Za-z_][\w.-]*(?=[ \t]*=)/ },
                variable,
                {
                    begin: new RegExp(`^(?=(?:${methods})[ \\t]+\\S)`),
                    end: /$/,
                    contains: [
                        { scope: 'keyword', begin: /^[A-Z]+/ },
                        { scope: 'meta', begin: /HTTP\/(?:[23]|1\.[01])\b/ },
                        {
                            scope: 'string',
                            begin: /[ \t]+(?!HTTP\/)(?=\S)/,
                            end: /(?=[ \t]+HTTP\/|$)/,
                            excludeBegin: true,
                            contains: [variable],
                        },
                    ],
                    starts: {
                        end: boundary,
                        contains: [
                            header,
                            {
                                begin: /\n[ \t]*\n(?=[ \t]*(?:[[{"\d-]|true\b|false\b|null\b))/,
                                end: boundary,
                                subLanguage: 'json',
                                relevance: 0,
                            },
                            {
                                begin: /\n[ \t]*\n/,
                                end: boundary,
                                subLanguage: [],
                                relevance: 0,
                            },
                        ],
                    },
                },
                ...(language.contains ?? []),
            ],
        }
    }
}
