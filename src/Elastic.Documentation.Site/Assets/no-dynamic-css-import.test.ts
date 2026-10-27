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

describe('stylesheet loading', () => {
    // Vite compiles import('./x.css') to a no-op here: the chunk is emitted but nothing adds a <link>
    // for it, so the styles never apply (KaTeX shipped unstyled this way).
    it('never imports a stylesheet dynamically', () => {
        const offenders = sources(__dirname).filter((file) =>
            /import\(\s*['"][^'"]+\.css(\?[^'"]*)?['"]\s*\)/.test(
                readFileSync(file, 'utf8')
            )
        )

        expect(offenders).toEqual([])
    })
})
