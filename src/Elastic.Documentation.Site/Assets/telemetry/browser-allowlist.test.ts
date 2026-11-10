import * as semconv from './semconv'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'

/**
 * browser-allowlist.json lists the attribute keys the collectors keep from the browser's OTLP exports.
 * The collector repos (docs-internal-workflows and codex-environments) render it into their collector
 * configs at deploy time, using the file at the docs-builder version they deploy. A key that is not on
 * the list is dropped by the collector without an error, so these tests are where a new key is caught.
 */

// Vitest runs from the package root. jsdom's URL does not support file URLs, so import.meta.url is not used.
const telemetryFile = (name: string) =>
    resolve(process.cwd(), 'Assets/telemetry', name)

type Allowlist = { resource: string[]; signal: Record<string, string[]> }

const allowlist = JSON.parse(
    readFileSync(telemetryFile('browser-allowlist.json'), 'utf8')
) as Allowlist
const signalKeys = Object.values(allowlist.signal).flat()
const resourceKeys = allowlist.resource

// Custom constants written as `export const ATTR_X = 'key'` in semconv.ts.
const customKeys = [
    ...readFileSync(telemetryFile('semconv.ts'), 'utf8').matchAll(
        /^export const ATTR_[A-Z0-9_]+ = '([^']+)'/gm
    ),
].map((match) => match[1])

describe('browser-allowlist.json', () => {
    it('lists no key twice', () => {
        for (const keys of [resourceKeys, signalKeys]) {
            expect(keys.filter((k, i) => keys.indexOf(k) !== i)).toEqual([])
        }
    })

    it('lists only plain attribute keys', () => {
        // The collector matches keys exactly and renders them into a quoted OTTL list.
        for (const key of [...resourceKeys, ...signalKeys]) {
            expect(key).toMatch(/^[a-z][a-z0-9_]*(\.[a-z0-9_]+)*$/)
        }
    })

    it('has every custom semconv.ts key on the signal list', () => {
        expect(customKeys.length).toBeGreaterThan(0)
        expect(customKeys.filter((k) => !signalKeys.includes(k))).toEqual([])
    })

    it('has every re-exported standard key on the resource or signal list', () => {
        const standard = (Object.values(semconv) as unknown[]).filter(
            (value): value is string =>
                typeof value === 'string' && !customKeys.includes(value)
        )
        expect(standard.length).toBeGreaterThan(0)
        expect(
            standard.filter(
                (k) => !signalKeys.includes(k) && !resourceKeys.includes(k)
            )
        ).toEqual([])
    })
})
