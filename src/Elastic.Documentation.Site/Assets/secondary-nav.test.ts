import { initSecondaryNav, syncSecondaryNavActive } from './secondary-nav'

function renderNav() {
    document.body.innerHTML = `
        <nav>
            <details class="secondary-nav-dropdown" id="products">
                <summary>Products</summary>
                <div class="secondary-nav-dropdown-menu"><a href="/a/">A</a></div>
            </details>
            <details class="secondary-nav-dropdown" id="extend">
                <summary>Extend</summary>
                <div class="secondary-nav-dropdown-menu"><a href="/b/">B</a></div>
            </details>
        </nav>
        <main><p id="outside">page</p></main>
    `
    return {
        products: document.querySelector<HTMLDetailsElement>('#products')!,
        extend: document.querySelector<HTMLDetailsElement>('#extend')!,
        outside: document.querySelector<HTMLElement>('#outside')!,
    }
}

describe('initSecondaryNav', () => {
    beforeAll(() => initSecondaryNav())

    it('closes an open dropdown when clicking outside of it', () => {
        const { products, outside } = renderNav()
        products.open = true

        outside.dispatchEvent(new MouseEvent('click', { bubbles: true }))

        expect(products.open).toBe(false)
    })

    it('closes the other dropdowns when one is opened', () => {
        const { products, extend } = renderNav()
        products.open = true

        // the native <details> toggle opens this one, our listener closes the sibling
        extend
            .querySelector('summary')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))

        expect(products.open).toBe(false)
        expect(extend.open).toBe(true)
    })

    it('leaves clicks inside the open panel alone so links still work', () => {
        const { products } = renderNav()
        products.open = true

        products
            .querySelector('a')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))

        expect(products.open).toBe(true)
    })

    it('closes on Escape and returns focus to the summary', () => {
        const { products } = renderNav()
        products.open = true
        const summary = products.querySelector('summary')!
        summary.focus()

        document.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
        )

        expect(products.open).toBe(false)
        expect(document.activeElement).toBe(summary)
    })

    it('adds is-open on the next frame so the panel can transition in', () => {
        const queued: FrameRequestCallback[] = []
        const raf = jest
            .spyOn(window, 'requestAnimationFrame')
            .mockImplementation((cb: FrameRequestCallback) => {
                queued.push(cb)
                return queued.length
            })
        const { products } = renderNav()
        products.open = true
        products.dispatchEvent(new Event('toggle'))

        expect(products.classList.contains('is-open')).toBe(false)
        queued.forEach((cb) => cb(0))
        expect(products.classList.contains('is-open')).toBe(true)
        raf.mockRestore()
    })

    it('keeps is-closing on the panel until the EUI exit motion finishes', () => {
        jest.useFakeTimers()
        const { products, outside } = renderNav()
        products.open = true

        outside.dispatchEvent(new MouseEvent('click', { bubbles: true }))

        expect(products.open).toBe(false)
        expect(products.classList.contains('is-closing')).toBe(true)

        jest.advanceTimersByTime(349)
        expect(products.classList.contains('is-closing')).toBe(true)

        jest.advanceTimersByTime(1)
        expect(products.classList.contains('is-closing')).toBe(false)
        jest.useRealTimers()
    })
})

describe('syncSecondaryNavActive', () => {
    function renderTabs() {
        document.body.innerHTML = `
            <nav id="secondary-nav">
                <ul>
                    <li class="secondary-nav-item secondary-nav-item--active" data-section-ids="guides">Guides</li>
                    <li class="secondary-nav-item" data-section-ids="ref-section-id extra">Reference</li>
                    <li class="secondary-nav-item">Products</li>
                </ul>
            </nav>
        `
        return {
            guides: document.querySelectorAll('.secondary-nav-item')[0],
            reference: document.querySelectorAll('.secondary-nav-item')[1],
            products: document.querySelectorAll('.secondary-nav-item')[2],
        }
    }

    it('moves the active class to the item whose section ids include the current section', () => {
        const { guides, reference, products } = renderTabs()

        syncSecondaryNavActive('ref-section-id')

        expect(guides.classList.contains('secondary-nav-item--active')).toBe(
            false
        )
        expect(reference.classList.contains('secondary-nav-item--active')).toBe(
            true
        )
        expect(products.classList.contains('secondary-nav-item--active')).toBe(
            false
        )
    })

    it('clears every active tab when the page has no section', () => {
        const { guides, reference } = renderTabs()

        syncSecondaryNavActive(null)

        expect(guides.classList.contains('secondary-nav-item--active')).toBe(
            false
        )
        expect(reference.classList.contains('secondary-nav-item--active')).toBe(
            false
        )
    })
})
