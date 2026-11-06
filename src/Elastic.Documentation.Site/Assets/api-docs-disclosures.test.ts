import { initApiDocs } from './api-docs'
import { initSmoothScroll } from './smooth-scroll'

/** One of each collapsible on an operation page, nested the way the property tree nests them. */
function markup(): string {
    return `
        <article id="markdown-content">
        <ul id="paths"><li><a id="path-jump" href="#deep">{rule}</a></li></ul>
        <section id="elastic-api-v3">
            <a id="jump" href="#deep">deep</a>
            <div class="api-param-section collapsed" data-param-section id="section">
                <h2 class="section-header api-param-section-header" id="query-params">
                    <a class="headerlink api-param-section-title" href="#query-params">Query parameters</a>
                    <button type="button" class="api-param-section-toggle" aria-expanded="false"></button>
                </h2>
                <div class="api-param-section-body" id="section-body" hidden="until-found">
                    <div class="property-item collapsed" id="prop">
                        <dt>rule</dt>
                        <dd class="expand-toggle-row">
                            <button class="expand-toggle" id="prop-toggle" aria-expanded="false" data-count="6">
                                <span class="toggle-icon">+</span><span class="toggle-label">Show 6 properties</span>
                            </button>
                        </dd>
                        <div class="nested-properties" hidden="until-found">
                            <div class="union-variants-container" id="options">
                                <div class="union-variants">
                                    <div class="union-variant-item collapsed" id="variant">
                                        <div class="expand-toggle-row union-expand-toggle">
                                            <button class="expand-toggle" id="variant-toggle" aria-expanded="false" data-count="1">
                                                <span class="toggle-label">Show 1 property</span>
                                            </button>
                                        </div>
                                        <div class="nested-properties" id="variant-body" hidden="until-found"><dt id="deep">x</dt></div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
        </article>`
}

const byId = (id: string) => document.getElementById(id)!
const isExpanded = (id: string) => byId(id).classList.contains('expanded')
const label = (id: string) =>
    byId(id).querySelector('.toggle-label')!.textContent

describe('disclosure clicks on API pages', () => {
    beforeAll(() => initApiDocs())

    beforeEach(() => {
        document.body.innerHTML = markup()
        history.replaceState(null, '', location.pathname)
    })

    it('toggles a param section from its button', () => {
        const toggle = document.querySelector<HTMLButtonElement>(
            '.api-param-section-toggle'
        )!
        toggle.click()
        expect(isExpanded('section')).toBe(true)
        expect(toggle.getAttribute('aria-expanded')).toBe('true')
        expect(byId('section-body').hasAttribute('hidden')).toBe(false)

        toggle.click()
        expect(isExpanded('section')).toBe(false)
        expect(toggle.getAttribute('aria-expanded')).toBe('false')
        expect(byId('section-body').hasAttribute('hidden')).toBe(true)
    })

    it('toggles a param section on a topic page, where the content sits in #elastic-docs-v3', () => {
        document.body.innerHTML = markup().replace(
            'id="elastic-api-v3"',
            'id="elastic-docs-v3"'
        )

        document
            .querySelector<HTMLButtonElement>('.api-param-section-toggle')!
            .click()

        expect(isExpanded('section')).toBe(true)
    })

    it('toggles a param section from its title and sets the hash without a jump', () => {
        const pushState = vi.spyOn(history, 'pushState')
        document.querySelector<HTMLAnchorElement>('.headerlink')!.click()

        expect(isExpanded('section')).toBe(true)
        expect(pushState).toHaveBeenCalledWith(null, '', '#query-params')
        pushState.mockRestore()
    })

    it('leaves a modified click on the title to the browser', () => {
        const title = document.querySelector<HTMLAnchorElement>('.headerlink')!
        const event = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
            metaKey: true,
        })
        title.dispatchEvent(event)

        expect(event.defaultPrevented).toBe(false)
        expect(isExpanded('section')).toBe(false)
    })

    it('toggles a property and relabels its button', () => {
        byId('prop-toggle').click()
        expect(isExpanded('prop')).toBe(true)
        expect(label('prop-toggle')).toBe('Hide 6 properties')
        expect(
            byId('prop')
                .querySelector('.nested-properties')!
                .hasAttribute('hidden')
        ).toBe(false)
    })

    it('the page control opens everything, then folds it again, and relabels itself', () => {
        const button = document.createElement('button')
        button.dataset.apiExpandAll = ''
        button.textContent = 'Expand all'
        byId('section').append(button)

        button.click()
        for (const id of ['section', 'prop', 'variant'])
            expect(isExpanded(id)).toBe(true)
        expect(label('prop-toggle')).toBe('Hide 6 properties')
        expect(button.textContent).toBe('Collapse all')

        button.click()
        for (const id of ['section', 'prop', 'variant'])
            expect(isExpanded(id)).toBe(false)
        expect(label('prop-toggle')).toBe('Show 6 properties')
        expect(button.textContent).toBe('Expand all')
    })

    it('Expand all scrolls to the first row it opened when none is on screen', () => {
        const scrollIntoView = vi.fn()
        Element.prototype.scrollIntoView = scrollIntoView
        const button = document.createElement('button')
        button.dataset.apiExpandAll = ''
        byId('section').append(button)

        button.click()

        expect(scrollIntoView).toHaveBeenCalledTimes(1)
        expect(scrollIntoView.mock.contexts[0]).toBe(
            byId('section').firstElementChild
        )
        button.click()
        expect(scrollIntoView).toHaveBeenCalledTimes(1)
    })

    it('toggles a union variant, not the property around it', () => {
        byId('variant-toggle').click()
        expect(isExpanded('variant')).toBe(true)
        expect(label('variant-toggle')).toBe('Hide 1 property')
        expect(isExpanded('prop')).toBe(false)
    })

    it('opens every collapsible around an in-page link target and scrolls to it smoothly', () => {
        const scroll = vi.fn()
        byId('deep').scrollIntoView = scroll
        const pushState = vi.spyOn(history, 'pushState')

        byId('jump').click()

        for (const id of ['variant', 'prop', 'section'])
            expect(isExpanded(id)).toBe(true)
        expect(scroll).toHaveBeenCalledWith({ behavior: 'smooth' })
        expect(pushState).toHaveBeenCalledWith(null, '', '#deep')
        pushState.mockRestore()
    })

    it('opens the collapsibles for a path placeholder link, even when its hash is already set', () => {
        initSmoothScroll()
        const scroll = vi.fn()
        byId('deep').scrollIntoView = scroll
        history.replaceState(null, '', '#deep')

        byId('path-jump').click()

        for (const id of ['variant', 'prop', 'section'])
            expect(isExpanded(id)).toBe(true)
        expect(scroll).toHaveBeenCalledWith({ behavior: 'smooth' })
    })

    it('opens every collapsible around a find-in-page match', () => {
        byId('variant-body').dispatchEvent(
            new Event('beforematch', { bubbles: true })
        )
        for (const id of ['variant', 'prop', 'section'])
            expect(isExpanded(id)).toBe(true)
    })
})
