import {
    applyParamLayout,
    clampDescriptions,
    initParamLayout,
    paramLayoutStorageKey,
    storedParamLayout,
} from './api-param-layout'

/** Two flat parameter sections with the switch, and a request body without one, as an operation page renders them. */
function markup(): string {
    const section = (anchor: string) => `
        <div class="api-param-section expanded" data-param-section id="${anchor}-section">
            <h2 class="section-header api-param-section-header" id="${anchor}">
                <a class="headerlink api-param-section-title" href="#${anchor}">${anchor}</a>
                <button type="button" class="api-param-section-toggle" aria-expanded="true"></button>
                <span class="api-param-layout" role="group" data-param-layout hidden>
                    <button type="button" class="api-param-layout-option" data-layout="list" aria-pressed="true">List</button>
                    <button type="button" class="api-param-layout-option" data-layout="table" aria-pressed="false">Table</button>
                </span>
            </h2>
            <div class="api-param-section-body" id="${anchor}-list">
                <dl class="property-list">
                    <div class="property-item depth-0" id="${anchor}-long">
                        <dt id="${anchor}-keep_alive"><code>keep_alive</code><code class="schema-type">string</code></dt>
                        <dd><p>A long description.</p><p><a href="#deep" id="${anchor}-deep-link">deep link</a></p></dd>
                        <dd class="reference-link-row"><a href="#">Reference</a></dd>
                    </div>
                    <div class="property-item depth-0" id="${anchor}-short">
                        <dt id="${anchor}-typed_keys"><code>typed_keys</code><code class="schema-type">boolean</code></dt>
                        <dd><p>A short one.</p></dd>
                    </div>
                </dl>
            </div>
        </div>`
    return `
        <section id="elastic-api-v3">
            ${section('path-params')}
            ${section('query-params')}
            <div class="api-param-section expanded" data-param-section id="request-body-section">
                <h2 class="section-header api-param-section-header" id="request-body">
                    <button type="button" class="api-param-section-toggle" aria-expanded="true"></button>
                </h2>
            </div>
        </section>`
}

const layoutOf = (id: string) =>
    document.getElementById(id)!.dataset.layout ?? null
const pressed = (id: string, layout: string) =>
    document
        .querySelector(`#${id} [data-layout="${layout}"]`)!
        .getAttribute('aria-pressed')

describe('parameter layout switch', () => {
    beforeEach(() => {
        localStorage.clear()
        document.body.innerHTML = markup()
    })

    it('shows the control only once the script has taken it', () => {
        const control = document.querySelector<HTMLElement>(
            '#path-params-section [data-param-layout]'
        )!
        expect(control.hidden).toBe(true)
        initParamLayout()
        expect(control.hidden).toBe(false)
    })

    it('opens as a list when nothing is remembered', () => {
        initParamLayout()
        expect(layoutOf('path-params-section')).toBe('list')
        expect(pressed('path-params-section', 'list')).toBe('true')
        expect(pressed('path-params-section', 'table')).toBe('false')
    })

    it('lays out every section with a switch from one click and remembers it', () => {
        initParamLayout()
        document
            .querySelector<HTMLElement>(
                '#path-params-section [data-layout="table"]'
            )!
            .click()
        expect(layoutOf('path-params-section')).toBe('table')
        expect(layoutOf('query-params-section')).toBe('table')
        expect(pressed('query-params-section', 'table')).toBe('true')
        expect(pressed('query-params-section', 'list')).toBe('false')
        expect(localStorage.getItem(paramLayoutStorageKey)).toBe('table')
    })

    it('leaves a section without a switch alone', () => {
        applyParamLayout('table')
        expect(layoutOf('request-body-section')).toBeNull()
    })

    it('opens the next page in the remembered layout', () => {
        localStorage.setItem(paramLayoutStorageKey, 'table')
        initParamLayout()
        expect(layoutOf('query-params-section')).toBe('table')
        expect(pressed('query-params-section', 'table')).toBe('true')
    })

    it('forgets the choice when the reader goes back to the list', () => {
        localStorage.setItem(paramLayoutStorageKey, 'table')
        initParamLayout()
        document
            .querySelector<HTMLElement>(
                '#query-params-section [data-layout="list"]'
            )!
            .click()
        expect(layoutOf('path-params-section')).toBe('list')
        expect(localStorage.getItem(paramLayoutStorageKey)).toBeNull()
    })

    it('ignores a value in storage that is not a layout', () => {
        localStorage.setItem(paramLayoutStorageKey, 'cards')
        expect(storedParamLayout()).toBe('list')
    })

    describe('clamped descriptions', () => {
        // jsdom lays nothing out, so the fixture says which description overflows the clamp.
        const longOnly = (dd: HTMLElement) =>
            dd.closest('.property-item')!.id.endsWith('-long')

        // The handlers bind once per document; a test that clicks or focuses needs them bound.
        beforeEach(() => initParamLayout())
        const row = (id: string) => document.getElementById(id)!
        const toggle = (id: string) =>
            row(id).querySelector<HTMLButtonElement>('.api-param-clamp-toggle')

        it('clamps the rows that overflow and gives only them a control', () => {
            applyParamLayout('table', document, longOnly)
            expect(row('query-params-long').dataset.clamp).toBe('clamped')
            expect(toggle('query-params-long')!.textContent).toBe('Show more')
            expect(
                toggle('query-params-long')!.getAttribute('aria-controls')
            ).toBe('query-params-keep_alive-description')
            expect(row('query-params-short').dataset.clamp).toBeUndefined()
            expect(toggle('query-params-short')).toBeNull()
        })

        it('expands and clamps a row from its control', () => {
            applyParamLayout('table', document, longOnly)
            toggle('query-params-long')!.click()
            expect(row('query-params-long').dataset.clamp).toBe('expanded')
            expect(toggle('query-params-long')!.textContent).toBe('Show less')
            expect(
                toggle('query-params-long')!.getAttribute('aria-expanded')
            ).toBe('true')
            toggle('query-params-long')!.click()
            expect(row('query-params-long').dataset.clamp).toBe('clamped')
        })

        it('keeps a row the reader expanded when the layout is applied again', () => {
            applyParamLayout('table', document, longOnly)
            toggle('query-params-long')!.click()
            clampDescriptions(
                row('query-params-long').closest('[data-param-section]')!,
                longOnly
            )
            expect(row('query-params-long').dataset.clamp).toBe('expanded')
            expect(
                row('query-params-long').querySelectorAll(
                    '.api-param-clamp-toggle'
                )
            ).toHaveLength(1)
        })

        it('shows a clamped row when focus lands on a link in its description', () => {
            applyParamLayout('table', document, longOnly)
            document.getElementById('query-params-deep-link')!.focus()
            expect(row('query-params-long').dataset.clamp).toBe('expanded')
            expect(toggle('query-params-long')!.textContent).toBe('Show less')
        })

        it('removes the clamp and the control in list layout', () => {
            applyParamLayout('table', document, longOnly)
            applyParamLayout('list', document, longOnly)
            expect(row('query-params-long').dataset.clamp).toBeUndefined()
            expect(toggle('query-params-long')).toBeNull()
        })
    })
})
