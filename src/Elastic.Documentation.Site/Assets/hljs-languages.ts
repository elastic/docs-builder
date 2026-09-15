import esql from '@elastic/highlightjs-esql'
import { LanguageFn } from 'highlight.js'
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

function toLanguageFn(mod: unknown): LanguageFn {
    const m = mod as { default?: LanguageFn }
    return (typeof mod === 'function' ? mod : m.default) as LanguageFn
}

// All curated grammars in one async chunk. Loaded only when a page or message
// contains code blocks.
export const languages: Record<string, LanguageFn> = {
    asciidoc: toLanguageFn(asciidoc),
    bash: toLanguageFn(bash),
    c: toLanguageFn(c),
    csharp: toLanguageFn(csharp),
    css: toLanguageFn(css),
    dockerfile: toLanguageFn(dockerfile),
    dos: toLanguageFn(dos),
    ebnf: toLanguageFn(ebnf),
    esql: toLanguageFn(esql),
    go: toLanguageFn(go),
    gradle: toLanguageFn(gradle),
    groovy: toLanguageFn(groovy),
    handlebars: toLanguageFn(handlebars),
    http: toLanguageFn(http),
    ini: toLanguageFn(ini),
    java: toLanguageFn(java),
    javascript: toLanguageFn(javascript),
    json: toLanguageFn(json),
    kotlin: toLanguageFn(kotlin),
    markdown: toLanguageFn(markdown),
    nginx: toLanguageFn(nginx),
    php: toLanguageFn(php),
    plaintext: toLanguageFn(plaintext),
    powershell: toLanguageFn(powershell),
    properties: toLanguageFn(properties),
    python: toLanguageFn(python),
    ruby: toLanguageFn(ruby),
    rust: toLanguageFn(rust),
    scala: toLanguageFn(scala),
    shell: toLanguageFn(shell),
    sql: toLanguageFn(sql),
    swift: toLanguageFn(swift),
    typescript: toLanguageFn(typescript),
    xml: toLanguageFn(xml),
    yaml: toLanguageFn(yaml),
}
