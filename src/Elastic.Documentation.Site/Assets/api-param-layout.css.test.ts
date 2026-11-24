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

/** A nested prelude resolved against its parents: `&` stands for the parent, and a prelude without one descends from it. */
function resolve(prelude: string, parents: string[]): string[] {
    const own = prelude
        .split(',')
        .map((selector) => selector.trim())
        .filter(Boolean)
    if (parents.length === 0) return own
    return parents.flatMap((parent) =>
        own.map((selector) =>
            selector.includes('&')
                ? selector.split('&').join(parent)
                : `${parent} ${selector}`
        )
    )
}

/**
 * Every rule in the block with its selectors resolved through nesting, however deep, so a rule written as
 * `.a { &:hover { … } }` is checked as `.a:hover`. An at-rule prelude keeps its parent's selectors.
 */
function rulesOf(block: string): Rule[] {
    const stripped = block.replace(/\/\*[\s\S]*?\*\//g, '')
    const rules: Rule[] = []
    const open: Rule[] = []
    let text = ''
    for (const char of stripped) {
        if (char === '{') {
            const parent = open[open.length - 1]
            const preludeStart = text.lastIndexOf(';') + 1
            if (parent) parent.declarations += text.slice(0, preludeStart)
            const prelude = text.slice(preludeStart).trim()
            const parents = parent?.selectors ?? []
            open.push({
                selectors: prelude.startsWith('@')
                    ? parents
                    : resolve(prelude, parents),
                declarations: '',
            })
            text = ''
        } else if (char === '}') {
            const closed = open.pop()
            if (closed)
                rules.push({
                    selectors: closed.selectors,
                    declarations: closed.declarations + text,
                })
            text = ''
        } else text += char
    }
    return rules
}

/** The rules in the block that display an element a browser without subgrid must never see. */
function shownRules(block: string): string[] {
    return rulesOf(block)
        .filter((rule) =>
            rule.selectors.some((selector) => hiddenOutside.test(selector))
        )
        .filter((rule) => {
            const display = rule.declarations.match(/display:\s*([a-z-]+)/)
            return display !== null && display[1] !== 'none'
        })
        .map((rule) => rule.selectors.join(', '))
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
        expect(shownRules(outside)).toEqual([])
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

    it('keeps the description cell at its own height so the clamp can be measured', () => {
        expect(gated).toMatch(/> dd \{[^}]*\balign-self:\s*start;/)
    })
})

describe('the stylesheet reader', () => {
    it('resolves nested selectors before checking them', () => {
        const nested = `
            .api-param-layout {
                display: none;
                &:not([hidden]) { display: inline-flex; }
                .ready & { opacity: 1; }
                @media (min-width: 1px) { & .inner { color: red; } }
            }`
        expect(
            rulesOf(nested).map((rule) => [
                rule.selectors.join(', '),
                rule.declarations.trim(),
            ])
        ).toEqual([
            ['.api-param-layout:not([hidden])', 'display: inline-flex;'],
            ['.ready .api-param-layout', 'opacity: 1;'],
            ['.api-param-layout .inner', 'color: red;'],
            ['.api-param-layout', ''],
            ['.api-param-layout', 'display: none;'],
        ])
        expect(shownRules(nested)).toEqual(['.api-param-layout:not([hidden])'])
    })

    it('keeps grouped selectors apart', () => {
        expect(
            rulesOf('.a, .b { & > .c, .d { display: flex; } }').map(
                (rule) => rule.selectors
            )
        ).toEqual([
            ['.a > .c', '.a .d', '.b > .c', '.b .d'],
            ['.a', '.b'],
        ])
    })
})
