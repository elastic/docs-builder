import { initSecondaryNav, syncSecondaryNavActive } from './secondary-nav'

describe('syncSecondaryNavActive', () => {
    beforeAll(() => initSecondaryNav())

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

    it('previews the clicked tab as active before the swap', () => {
        const { guides, reference } = renderTabs()

        reference.dispatchEvent(
            new MouseEvent('click', { bubbles: true, button: 0 })
        )

        expect(guides.classList.contains('secondary-nav-item--active')).toBe(
            false
        )
        expect(reference.classList.contains('secondary-nav-item--active')).toBe(
            true
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
