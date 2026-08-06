import { initSecondaryNav } from './secondary-nav'

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
})
