// The design-system bundle must stay free of React, EUI and Emotion. Parcel's output lists every chunk of the build in a shared import map,
// so scanning the built file would give false alarms; this walks the source import graph from the entry instead.
import { existsSync, readFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'

const assets = resolve(__dirname, '..')
const forbidden = [
    /^@elastic\/(eui|charts)/,
    /^react($|\/)/,
    /^react-dom/,
    /^@emotion\//,
    /^@tanstack\/react/,
    /^zustand/,
    /^@r2wc\//,
]
const importPattern =
    /(?:import|export)\s[^'"`;]*?from\s*['"]([^'"]+)['"]|import\s*['"]([^'"]+)['"]|import\(\s*['"]([^'"]+)['"]\s*\)/g

function resolveRelative(from: string, specifier: string): string | null {
    const base = resolve(dirname(from), specifier)
    for (const candidate of [
        base,
        `${base}.ts`,
        `${base}.tsx`,
        join(base, 'index.ts'),
    ]) {
        if (existsSync(candidate) && /\.(ts|tsx)$/.test(candidate))
            return candidate
    }
    return null
}

function walk(entry: string) {
    const files = new Set<string>()
    const packages = new Set<string>()
    const queue = [entry]
    while (queue.length > 0) {
        const file = queue.pop()!
        if (files.has(file)) continue
        files.add(file)
        const source = readFileSync(file, 'utf8')
        for (const match of source.matchAll(importPattern)) {
            const specifier = match[1] ?? match[2] ?? match[3]
            if (specifier.startsWith('.')) {
                const resolved = resolveRelative(file, specifier)
                if (resolved) queue.push(resolved)
            } else {
                packages.add(specifier)
            }
        }
    }
    return { files, packages }
}

describe('design-system bundle', () => {
    const { files, packages } = walk(join(assets, 'ds.ts'))

    it('reaches the runtime it needs', () => {
        expect(files.size).toBeGreaterThan(20)
        expect(packages.has('htmx.org')).toBe(true)
    })

    it('imports no React, EUI or Emotion package', () => {
        const found = [...packages].filter((name) =>
            forbidden.some((pattern) => pattern.test(name))
        )

        expect(found).toEqual([])
    })

    it('includes no React component source', () => {
        const tsx = [...files].filter((file) => file.endsWith('.tsx'))

        expect(tsx).toEqual([])
    })
})
