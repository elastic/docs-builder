/**
 * @vitest-environment node
 */
import { readdirSync, readFileSync } from 'node:fs'
import path from 'node:path'

function sources(dir: string): string[] {
    return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
        const full = path.join(dir, entry.name)
        if (entry.isDirectory()) return sources(full)
        return /\.tsx?$/.test(entry.name) && !/\.test\.tsx?$/.test(entry.name)
            ? [full]
            : []
    })
}

// import('x.css'), import 'x.css' and import x from 'x.css' (with or without a ?query).
const cssImport =
    /(?:(?<![@\w])import\s*\(\s*|(?<![@\w])import\s+(?:[^'"]*\sfrom\s+)?)['"][^'"]+\.css(?:\?[^'"]*)?['"]/

// katex-stylesheet.ts resolves the URL and adds the <link> itself, so it is the one allowed import.
const allowed = new Set(['katex-stylesheet.ts'])

describe('stylesheet loading', () => {
    // Vite extracts a stylesheet imported from a lazily loaded module into its own chunk, and nothing
    // adds a <link> for it, so the styles never apply (KaTeX shipped unstyled this way). Entry
    // stylesheets are loaded by <link> tags in _Head.cshtml instead.
    it('imports no stylesheet from TypeScript', () => {
        const offenders = sources(__dirname)
            .filter((file) => !allowed.has(path.basename(file)))
            .filter((file) => cssImport.test(readFileSync(file, 'utf8')))
            .map((file) => path.relative(__dirname, file))

        expect(offenders).toEqual([])
    })

    it.each([
        "import('./x.css')",
        'import("./x.css?inline")',
        "import './x.css'",
        "import styles from './x.css'",
        "import url from './x.css?url'",
    ])('flags %s', (line) => {
        expect(cssImport.test(line)).toBe(true)
    })

    it.each([
        "import { a } from './x'",
        "import('./x')",
        "@import './x.css'",
        '// see x.css for the styles',
    ])('ignores %s', (line) => {
        expect(cssImport.test(line)).toBe(false)
    })
})
