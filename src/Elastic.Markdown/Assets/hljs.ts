import { mergeHTMLPlugin } from './hljs-merge-html-plugin'
import { LanguageFn } from 'highlight.js'
import hljs from 'highlight.js/lib/core'
import asciidoc from 'highlight.js/lib/languages/asciidoc'
import bash from 'highlight.js/lib/languages/bash'
import c from 'highlight.js/lib/languages/c'
import csharp from 'highlight.js/lib/languages/csharp'
import css from 'highlight.js/lib/languages/css'
import dockerfile from 'highlight.js/lib/languages/dockerfile'
import dos from 'highlight.js/lib/languages/dos'
import ebnf from 'highlight.js/lib/languages/ebnf'
import go from 'highlight.js/lib/languages/go'
import gradle from 'highlight.js/lib/languages/gradle'
import groovy from 'highlight.js/lib/languages/groovy'
import handlebars from 'highlight.js/lib/languages/handlebars'
import http from 'highlight.js/lib/languages/http'
import ini from 'highlight.js/lib/languages/ini'
import java from 'highlight.js/lib/languages/java'
import javascript from 'highlight.js/lib/languages/javascript'
import json from 'highlight.js/lib/languages/json'
import kotlin from 'highlight.js/lib/languages/kotlin'
import markdown from 'highlight.js/lib/languages/markdown'
import nginx from 'highlight.js/lib/languages/nginx'
import php from 'highlight.js/lib/languages/php'
import plaintext from 'highlight.js/lib/languages/plaintext'
import powershell from 'highlight.js/lib/languages/powershell'
import properties from 'highlight.js/lib/languages/properties'
import python from 'highlight.js/lib/languages/python'
import ruby from 'highlight.js/lib/languages/ruby'
import rust from 'highlight.js/lib/languages/rust'
import scala from 'highlight.js/lib/languages/scala'
import shell from 'highlight.js/lib/languages/shell'
import sql from 'highlight.js/lib/languages/sql'
import swift from 'highlight.js/lib/languages/swift'
import typescript from 'highlight.js/lib/languages/typescript'
import xml from 'highlight.js/lib/languages/xml'
import yaml from 'highlight.js/lib/languages/yaml'
import { $$ } from 'select-dom'

const languages: Array<{
    name: string
    module: LanguageFn
    aliases?: string[]
}> = [
    { name: 'asciidoc', module: asciidoc },
    { name: 'bash', module: bash },
    { name: 'c', module: c },
    { name: 'csharp', module: csharp },
    { name: 'css', module: css },
    { name: 'dockerfile', module: dockerfile },
    { name: 'dos', module: dos },
    { name: 'ebnf', module: ebnf },
    { name: 'go', module: go },
    { name: 'gradle', module: gradle },
    { name: 'groovy', module: groovy },
    { name: 'handlebars', module: handlebars },
    { name: 'http', module: http },
    { name: 'ini', module: ini },
    { name: 'java', module: java },
    { name: 'javascript', module: javascript },
    { name: 'json', module: json },
    { name: 'kotlin', module: kotlin },
    { name: 'markdown', module: markdown },
    { name: 'nginx', module: nginx },
    { name: 'php', module: php },
    { name: 'plaintext', module: plaintext },
    { name: 'powershell', module: powershell },
    { name: 'properties', module: properties },
    { name: 'python', module: python },
    { name: 'ruby', module: ruby },
    { name: 'rust', module: rust },
    { name: 'scala', module: scala },
    { name: 'shell', module: shell, aliases: ['sh'] },
    { name: 'sql', module: sql },
    { name: 'swift', module: swift },
    { name: 'typescript', module: typescript },
    { name: 'xml', module: xml },
    { name: 'yaml', module: yaml },
]

languages.forEach((lang) => {
    hljs.registerLanguage(lang.name, lang.module)
    if (lang.aliases) {
        hljs.registerAliases(lang.aliases, { languageName: lang.name })
    }
})

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

hljs.registerLanguage('esql', function () {
    return {
        case_insensitive: true, // language is case-insensitive
        keywords: {
            keyword:
                'FROM ROW SHOW DISSECT DROP ENRICH EVAL GROK KEEP LIMIT LOOKUP JOIN MV_EXPAND RENAME SORT STATS WHERE METADATA CHANGE_POINT',
            literal: ['false', 'true', 'null'],
            function: [
                // aggregate
                'AVG',
                'COUNT',
                'COUNT_DISTINCT',
                'MAX',
                'MEDIAN',
                'MEDIAN_ABSOLUTE_DEVIATION',
                'MIN',
                'PERCENTILE',
                'ST_CENTROID_AGG',
                'ST_EXTENT_AGG',
                'STD_DEV',
                'SUM',
                'TOP',
                'VALUES',
                'WEIGHTED_AVG',
                // grouping
                'BUCKET',
                'CATEGORIZE',
                // conditional
                'CASE',
                'COALESCE',
                'GREATEST',
                'LEAST',
                //Date
                'DATE_DIFF',
                'DATE_EXTRACT',
                'DATE_FORMAT',
                'DATE_PARSE',
                'DATE_TRUNC',
                'NOW',
                //ip
                'CIDR_MATCH',
                'IP_PREFIX',
                //math
                'ABS',
                'ACOS',
                'ASIN',
                'ATAN',
                'ATAN2',
                'CBRT',
                'CEIL',
                'COS',
                'COSH',
                'E',
                'EXP',
                'FLOOR',
                'HYPOT',
                'LOG',
                'LOG10',
                'PI',
                'POW',
                'ROUND',
                'ROUND_TO',
                'SCALB',
                'SIGNUM',
                'SIN',
                'SINH',
                'SQRT',
                'TAN',
                'TANH',
                'TAU',
                //search
                'KQL',
                'MATCH',
                'QSTR',
                //spatial
                'ST_DISTANCE',
                'ST_INTERSECTS',
                'ST_DISJOINT',
                'ST_CONTAINS',
                'ST_WITHIN',
                'ST_X',
                'ST_Y',
                'ST_ENVELOPE',
                'ST_XMAX',
                'ST_XMIN',
                'ST_YMAX',
                'ST_YMIN',
                //string
                'BIT_LENGTH',
                'BYTE_LENGTH',
                'CONCAT',
                'ENDS_WITH',
                'FROM_BASE64',
                'HASH',
                'LEFT',
                'LENGTH',
                'LOCATE',
                'LTRIM',
                'MD5',
                'REPEAT',
                'REPLACE',
                'REVERSE',
                'RIGHT',
                'RTRIM',
                'SHA1',
                'SHA256',
                'SPACE',
                'SPLIT',
                'STARTS_WITH',
                'SUBSTRING',
                'TO_BASE64',
                'TO_LOWER',
                'TO_UPPER',
                'TRIM',
                //type conversion
                'TO_BOOLEAN',
                'TO_CARTESIANPOINT',
                'TO_CARTESIANSHAPE',
                'TO_DATEPERIOD',
                'TO_DATETIME',
                'TO_DATE_NANOS',
                'TO_DEGREES',
                'TO_DOUBLE',
                'TO_GEOPOINT',
                'TO_GEOSHAPE',
                'TO_INTEGER',
                'TO_IP',
                'TO_LONG',
                'TO_RADIANS',
                'TO_STRING',
                'TO_TIMEDURATION',
                'TO_UNSIGNED_LONG',
                'TO_VERSION',
                //multivalued
                'MV_APPEND',
                'MV_AVG',
                'MV_CONCAT',
                'MV_COUNT',
                'MV_DEDUPE',
                'MV_FIRST',
                'MV_LAST',
                'MV_MAX',
                'MV_MEDIAN',
                'MV_MEDIAN_ABSOLUTE_DEVIATION',
                'MV_MIN',
                'MV_PERCENTILE',
                'MV_PSERIES_WEIGHTED_SUM',
                'MV_SORT',
                'MV_SLICE',
                'MV_SUM',
                'MV_ZIP',
            ],
        },
        contains: [
            hljs.QUOTE_STRING_MODE,
            hljs.C_LINE_COMMENT_MODE,
            {
                scope: 'operator', // (pathname: path1/path2/dothis) color #ab5656
                match: /(?:<|<=|==|::|\w+:|!=|>=|>|LIKE|RLIKE|IS NULL|IS NOT NULL)/,
            },
            {
                scope: 'punctuation', // (pathname: path1/path2/dothis) color #ab5656
                match: /(?:!?\[|\]|\|)/,
            },
            NUMBER,
        ],
    }
})

hljs.addPlugin(mergeHTMLPlugin)

// The unescaped HTML warning is caused by the mergeHTMLPlugin which we are using
// for code callouts
hljs.configure({ ignoreUnescapedHTML: true })

export function initHighlight() {
    $$('#markdown-content pre code:not([data-highlighted])').forEach(
        hljs.highlightElement
    )
}
