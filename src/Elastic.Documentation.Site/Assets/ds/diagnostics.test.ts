import {
    applyEvent,
    clipboardTextOf,
    emptyState,
    initDiagnostics,
    locationOf,
    visibleDiagnostics,
    worstSeverity,
    type Diagnostic,
} from './diagnostics'

const error: Diagnostic = {
    severity: 'error',
    file: 'a.md',
    message: 'bad',
    line: 3,
    column: 2,
}
const hint: Diagnostic = { severity: 'hint', file: 'b.md', message: 'fyi' }
const warning: Diagnostic = {
    severity: 'warning',
    file: 'c.md',
    message: 'hmm',
}

describe('diagnostics state', () => {
    it('counts streamed diagnostics by severity', () => {
        let state = applyEvent(emptyState(), { type: 'build_start' })
        state = applyEvent(state, { type: 'diagnostic', diagnostic: error })
        state = applyEvent(state, { type: 'diagnostic', diagnostic: warning })

        expect(state.status).toBe('building')
        expect([state.errors, state.warnings, state.hints]).toEqual([1, 1, 0])
        expect(state.diagnostics).toHaveLength(2)
    })

    it('replaces everything from a state snapshot', () => {
        const state = applyEvent(emptyState(), {
            type: 'state',
            status: 'complete',
            errors: 1,
            diagnostics: [error],
        })

        expect(state.status).toBe('complete')
        expect(state.errors).toBe(1)
        expect(state.diagnostics).toEqual([error])
    })

    it('clears the previous build when a new one starts', () => {
        const before = applyEvent(emptyState(), {
            type: 'diagnostic',
            diagnostic: error,
        })
        const after = applyEvent(before, { type: 'build_start' })

        expect(after.diagnostics).toEqual([])
        expect(after.errors).toBe(0)
    })

    it('ignores unknown events', () => {
        const state = emptyState()

        expect(applyEvent(state, { type: 'wat' })).toBe(state)
    })

    it('sorts by severity and filters', () => {
        expect(
            visibleDiagnostics([hint, error, warning], 'all').map(
                (d) => d.severity
            )
        ).toEqual(['error', 'warning', 'hint'])
        expect(visibleDiagnostics([hint, error], 'hint')).toEqual([hint])
    })

    it('reports the worst severity', () => {
        expect(worstSeverity({ ...emptyState(), warnings: 1, hints: 2 })).toBe(
            'warning'
        )
        expect(worstSeverity(emptyState())).toBeNull()
    })

    it('formats the location and the clipboard text', () => {
        expect(locationOf(error)).toBe('a.md:3:2')
        expect(clipboardTextOf(error)).toBe('[ERROR] a.md:3:2: bad')
    })
})

describe('diagnostics panel', () => {
    const markup = `
<aside data-diagnostics hidden>
  <div data-diagnostics-hud hidden>
    <div data-diagnostics-resize></div>
    <button data-diagnostics-filter="all" aria-pressed="true"></button>
    <button data-diagnostics-filter="error" aria-pressed="false"></button>
    <button data-diagnostics-close></button>
    <ol data-diagnostics-list></ol><p data-diagnostics-empty></p>
  </div>
  <button data-diagnostics-toggle aria-expanded="false"><span data-diagnostics-summary></span><span data-diagnostics-action></span></button>
  <template data-diagnostics-row><li><span class="diagnostics__severity"></span><span class="diagnostics__location"></span><p class="diagnostics__message"></p><button data-diagnostics-copy></button></li></template>
</aside>`

    beforeEach(() => {
        document.body.innerHTML = markup
        sessionStorage.clear()
        global.fetch = jest.fn().mockResolvedValue({
            ok: true,
            json: async () => ({
                type: 'state',
                status: 'complete',
                errors: 1,
                diagnostics: [error],
            }),
        }) as unknown as typeof fetch
    })

    it('reveals itself and lists polled diagnostics', async () => {
        initDiagnostics()
        await new Promise((r) => setTimeout(r, 0))

        const root = document.querySelector<HTMLElement>('[data-diagnostics]')!
        expect(root.hidden).toBe(false)
        expect(root.dataset.severity).toBe('error')
        expect(
            document.querySelectorAll('[data-diagnostics-list] li')
        ).toHaveLength(1)
        expect(
            document.querySelector('[data-diagnostics-summary]')!.textContent
        ).toContain('1 issue')
    })

    it('opens and closes the console and remembers it', async () => {
        initDiagnostics()
        const toggle = document.querySelector<HTMLButtonElement>(
            '[data-diagnostics-toggle]'
        )!
        toggle.click()

        expect(
            document.querySelector<HTMLElement>('[data-diagnostics-hud]')!
                .hidden
        ).toBe(false)
        expect(toggle.getAttribute('aria-expanded')).toBe('true')
        expect(sessionStorage.getItem('diagnostics-hud-open')).toBe('true')

        document
            .querySelector<HTMLButtonElement>('[data-diagnostics-close]')!
            .click()
        expect(
            document.querySelector<HTMLElement>('[data-diagnostics-hud]')!
                .hidden
        ).toBe(true)
    })

    it('filters the list and marks the active filter', async () => {
        initDiagnostics()
        await new Promise((r) => setTimeout(r, 0))
        const hints = document.querySelector<HTMLButtonElement>(
            '[data-diagnostics-filter="error"]'
        )!
        hints.click()

        expect(hints.getAttribute('aria-pressed')).toBe('true')
        expect(
            document.querySelectorAll('[data-diagnostics-list] li')
        ).toHaveLength(1)
    })
})
