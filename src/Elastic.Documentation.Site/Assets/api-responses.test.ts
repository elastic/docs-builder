import { initApiDocs } from './api-docs'

function responseMarkup(): string {
    return `
        <section id="elastic-api-v3">
            <p id="elsewhere">Unrelated operation description.</p>
            <div class="response-panel collapsed" data-status="200">
                <div class="response-summary">
                    <div class="response-status-row">
                        <button
                            type="button"
                            class="response-status-toggle"
                            aria-expanded="false"
                            aria-controls="response-200-fields">
                            <span class="response-status-chip status-success">200</span>
                            <span class="content-type-tag">application/json</span>
                            <span class="response-toggle-icon" aria-hidden="true"></span>
                        </button>
                    </div>
                    <div class="response-description">
                        See the <a href="https://www.elastic.co/guide">guide</a>.
                    </div>
                </div>
                <div class="response-panel-body" id="response-200-fields" hidden="until-found"></div>
            </div>
        </section>
    `
}

/** Each response holds one foldable field; the page opens `defaultStatus` on load, as the server marks it. */
function syncMarkup(
    statuses: string[],
    { rail = statuses, defaultStatus = statuses[0] } = {}
): string {
    const panels = statuses
        .map(
            (s) => `
            <div class="response-panel collapsed" data-status="${s}" data-api-response-default="${s === defaultStatus}">
                <div class="response-summary">
                    <button type="button" class="response-status-toggle" aria-expanded="false">${s}</button>
                </div>
                <div class="response-panel-body" id="r-${s}" hidden="until-found">
                    <div class="property-item collapsed" id="field-${s}">
                        <dd class="expand-toggle-row">
                            <button class="expand-toggle" aria-expanded="false" data-count="1">
                                <span class="toggle-label">Show 1 property</span>
                            </button>
                        </dd>
                        <div class="nested-properties" hidden="until-found"></div>
                    </div>
                </div>
            </div>`
        )
        .join('')
    const tabs = rail
        .map(
            (s, i) =>
                `<button type="button" class="example-response-tab${i === 0 ? ' is-active' : ''}" data-status="${s}">${s}</button>`
        )
        .join('')
    const railPanels = rail
        .map(
            (s, i) =>
                `<div class="example-response-panel" data-status="${s}"${i === 0 ? '' : ' hidden'}></div>`
        )
        .join('')
    return `
        <section id="elastic-api-v3">
            <button type="button" data-api-expand-all>Expand all</button>
            <div data-api-responses>${panels}</div>
            <div data-api-response-samples>${tabs}${railPanels}</div>
        </section>`
}

describe('responses accordion and examples rail sync', () => {
    beforeAll(() => {
        initApiDocs()
        // jsdom does not implement scrollIntoView
        Element.prototype.scrollIntoView = vi.fn()
    })

    const byStatus = (cls: string, s: string) =>
        document.querySelector<HTMLElement>(`.${cls}[data-status="${s}"]`)!
    const panel = (s: string) => byStatus('response-panel', s)
    const toggle = (s: string) =>
        panel(s).querySelector<HTMLButtonElement>('.response-status-toggle')!
    const tab = (s: string) => byStatus('example-response-tab', s)
    const railPanel = (s: string) => byStatus('example-response-panel', s)
    const field = (s: string) => document.getElementById(`field-${s}`)!
    const expanded = (el: HTMLElement) => el.classList.contains('expanded')
    const expandAll = () =>
        document.querySelector<HTMLButtonElement>('[data-api-expand-all]')!

    it('closes the open response when another one opens', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        toggle('200').click()
        toggle('400').click()

        expect(expanded(panel('200'))).toBe(false)
        expect(expanded(panel('400'))).toBe(true)
    })

    it('closes the open response when its own header is clicked', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        toggle('200').click()
        toggle('200').click()

        expect(expanded(panel('200'))).toBe(false)
        expect(toggle('200').getAttribute('aria-expanded')).toBe('false')
    })

    it('moves the rail to the status opened in the main column', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        toggle('400').click()

        expect(tab('400').classList.contains('is-active')).toBe(true)
        expect(railPanel('400').hasAttribute('hidden')).toBe(false)
        expect(railPanel('200').hasAttribute('hidden')).toBe(true)
    })

    it('opens the main response for a clicked rail tab and closes the others', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        toggle('200').click()
        tab('400').click()

        expect(expanded(panel('400'))).toBe(true)
        expect(expanded(panel('200'))).toBe(false)
    })

    it('leaves the main column alone for a rail status it does not list', () => {
        document.body.innerHTML = syncMarkup(['200'], { rail: ['200', '404'] })
        toggle('200').click()
        tab('404').click()

        expect(expanded(panel('200'))).toBe(true)
    })

    it('closes the open response when a hash target opens another', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        panel('400').querySelector('.response-panel-body')!.innerHTML =
            '<span id="inside-400"></span>'
        toggle('200').click()
        window.location.hash = '#inside-400'
        window.dispatchEvent(new HashChangeEvent('hashchange'))

        expect(expanded(panel('400'))).toBe(true)
        expect(expanded(panel('200'))).toBe(false)
    })

    it('Expand all opens the default response and its fields, and no other response', () => {
        // The spec lists 400 first, but the page opens 200 on load
        document.body.innerHTML = syncMarkup(['400', '200'], {
            defaultStatus: '200',
        })
        expandAll().click()

        expect(expanded(panel('200'))).toBe(true)
        expect(expanded(field('200'))).toBe(true)
        expect(expanded(panel('400'))).toBe(false)
        expect(expanded(field('400'))).toBe(false)
        expect(expandAll().textContent).toBe('Collapse all')
    })

    it('Expand all keeps the open response and opens its fields', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        toggle('400').click()
        expandAll().click()

        expect(expanded(panel('400'))).toBe(true)
        expect(expanded(field('400'))).toBe(true)
        expect(expanded(panel('200'))).toBe(false)
        expect(expandAll().textContent).toBe('Collapse all')
    })

    it('offers Expand all again when a rail tab opens a response with folded fields', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        expandAll().click()
        tab('400').click()

        expect(expanded(panel('400'))).toBe(true)
        expect(expandAll().textContent).toBe('Expand all')
    })

    it('Collapse all closes the open response and its fields', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        expandAll().click()
        expandAll().click()

        for (const el of [panel('200'), panel('400'), field('200')])
            expect(expanded(el)).toBe(false)
        expect(expandAll().textContent).toBe('Expand all')
    })

    it('Collapse all also folds fields left open inside a closed response', () => {
        document.body.innerHTML = syncMarkup(['200', '400'])
        toggle('400').click()
        expandAll().click()
        toggle('200').click()
        expect(expanded(field('400'))).toBe(true)

        expandAll().click() // Expand all: opens 200's fields
        expandAll().click() // Collapse all

        expect(expanded(panel('200'))).toBe(false)
        expect(expanded(field('400'))).toBe(false)
    })
})

describe('response status row', () => {
    beforeAll(() => initApiDocs())

    beforeEach(() => {
        document.body.innerHTML = responseMarkup()
    })

    const panel = () => document.querySelector<HTMLElement>('.response-panel')!
    const toggle = () =>
        document.querySelector<HTMLButtonElement>('.response-status-toggle')!

    it('expands when the media type in the header is clicked', () => {
        document.querySelector<HTMLElement>('.content-type-tag')!.click()

        expect(panel().classList.contains('expanded')).toBe(true)
        expect(toggle().getAttribute('aria-expanded')).toBe('true')
    })

    it('leaves the panel collapsed when a description link is clicked', () => {
        const link = document.querySelector('a')!
        const event = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
        })
        link.dispatchEvent(event)

        expect(event.defaultPrevented).toBe(false)
        expect(panel().classList.contains('collapsed')).toBe(true)
        expect(toggle().getAttribute('aria-expanded')).toBe('false')
    })

    it('lets a description link with the expand-toggle class navigate', () => {
        const link = document.querySelector('a')!
        link.className = 'expand-toggle'
        const event = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
        })
        link.dispatchEvent(event)

        expect(event.defaultPrevented).toBe(false)
    })

    it('lets a description link with the response-status-toggle class navigate', () => {
        const link = document.querySelector('a')!
        link.className = 'response-status-toggle'
        const event = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
        })
        link.dispatchEvent(event)

        expect(event.defaultPrevented).toBe(false)
        expect(panel().classList.contains('collapsed')).toBe(true)
    })

    it('expands when text elsewhere on the page is selected', () => {
        window
            .getSelection()
            ?.selectAllChildren(document.getElementById('elsewhere')!)

        toggle().click()
        window.getSelection()?.removeAllRanges()

        expect(panel().classList.contains('expanded')).toBe(true)
        expect(toggle().getAttribute('aria-expanded')).toBe('true')
    })

    it('leaves the panel collapsed when summary text is selected', () => {
        window
            .getSelection()
            ?.selectAllChildren(
                document.querySelector('.response-description')!
            )

        toggle().click()
        window.getSelection()?.removeAllRanges()

        expect(panel().classList.contains('collapsed')).toBe(true)
        expect(toggle().getAttribute('aria-expanded')).toBe('false')
    })
})
