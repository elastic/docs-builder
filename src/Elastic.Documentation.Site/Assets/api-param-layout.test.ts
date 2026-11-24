import {
    applyParamLayout,
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
            <div class="api-param-section-body" id="${anchor}-list"></div>
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
})
