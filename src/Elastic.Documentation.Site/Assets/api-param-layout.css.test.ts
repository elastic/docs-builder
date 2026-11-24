/**
 * @vitest-environment node
 */
import { readFileSync } from 'node:fs'
import { join } from 'node:path'

/**
 * The parameter table needs subgrid, and a browser without it must keep the list and never see the switch. jsdom lays
 * nothing out, so this reads the stylesheet and checks that every rule which shows the switch or lays out the table sits
 * inside the subgrid support query, and that nothing outside it does.
 */
const css = readFileSync(
    join(__dirname, '../Components/Pages/ApiReference/api-reference.css'),
    'utf8'
)

const gateOpener = '@supports (grid-template-columns: subgrid) {'

/** The elements a browser without subgrid must never see; the options live inside the switch. */
const hiddenOutside =
    /\.api-param-layout(?!-option)|\.api-param-table-head|\.api-param-clamp-toggle/

interface Rule {
    selectors: string[]
    declarations: string
}

/** Every `selectors { declarations }` pair, however it is grouped; at-rule preludes hold a brace, so they never match. */
function rulesOf(block: string): Rule[] {
    const stripped = block.replace(/\/\*[\s\S]*?\*\//g, '')
    return [...stripped.matchAll(/([^{}]+)\{([^{}]*)\}/g)].map((m) => ({
        selectors: m[1]
            .split(',')
            .map((selector) => selector.trim())
            .filter(Boolean),
        declarations: m[2],
    }))
}

function gatedBlock(): string {
    const start = css.indexOf(gateOpener)
    expect(start).toBeGreaterThan(-1)
    let depth = 0
    for (let i = start; i < css.length; i++) {
        if (css[i] === '{') depth++
        else if (css[i] === '}' && --depth === 0) return css.slice(start, i)
    }
    throw new Error('the subgrid support query never closes')
}

describe('parameter table stylesheet', () => {
    const gated = gatedBlock()
    const outside = css.replace(gated, '')

    it('shows the switch only inside the subgrid support query', () => {
        expect(gated).toContain('.api-param-layout:not([hidden])')
        const shown = rulesOf(outside)
            .filter((rule) =>
                rule.selectors.some((selector) => hiddenOutside.test(selector))
            )
            .filter((rule) => {
                const display = rule.declarations.match(/display:\s*([a-z-]+)/)
                return display !== null && display[1] !== 'none'
            })
        expect(shown.map((rule) => rule.selectors.join(', '))).toEqual([])
    })

    it('lays out the table only inside the subgrid support query', () => {
        expect(gated).toContain("[data-layout='table']")
        expect(gated).toContain('grid-template-columns: subgrid')
        expect(outside).not.toContain("[data-layout='table']")
    })

    it('hides the switch, the label row and the clamp control by default', () => {
        expect(outside).toMatch(
            /\.api-param-layout,\s*\.api-param-table-head,\s*\.api-param-clamp-toggle\s*\{\s*display:\s*none;/
        )
    })
})
