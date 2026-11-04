import { initApiDocs } from './api-docs'

function enumValuesMarkup(): string {
    return `
        <section id="elastic-api-v3">
            <dl class="property-list">
                <div class="property-item collapsed has-children">
                    <dd class="enum-values">
                        <span class="values-label">Values:</span>
                        <code class="enum-value">a</code>
                        <span class="enum-values-folded" hidden="until-found">
                            <code class="enum-value">b</code>
                            <code class="enum-value">c</code>
                        </span>
                        <button class="expand-toggle enum-values-toggle" aria-expanded="false">
                            <span class="toggle-icon">+</span>
                            <span class="toggle-label">show 2 more values</span>
                        </button>
                    </dd>
                </div>
            </dl>
        </section>
    `
}

describe('enum values toggle', () => {
    beforeAll(() => initApiDocs())

    beforeEach(() => {
        document.body.innerHTML = enumValuesMarkup()
    })

    const toggle = () =>
        document.querySelector<HTMLButtonElement>('.enum-values-toggle')!
    const folded = () =>
        document.querySelector<HTMLElement>('.enum-values-folded')!

    it('unfolds the remaining values without toggling the enclosing property', () => {
        toggle().click()

        expect(folded().hasAttribute('hidden')).toBe(false)
        expect(toggle().getAttribute('aria-expanded')).toBe('true')
        expect(toggle().textContent).toContain('hide 2 more values')
        expect(
            document
                .querySelector('.property-item')!
                .classList.contains('collapsed')
        ).toBe(true)
    })

    it('folds the values again on a second click', () => {
        toggle().click()
        toggle().click()

        expect(folded().hasAttribute('hidden')).toBe(true)
        expect(toggle().textContent).toContain('show 2 more values')
    })

    it('unfolds when find-in-page matches a folded value', () => {
        folded()
            .querySelector('code')!
            .dispatchEvent(new Event('beforematch', { bubbles: true }))

        expect(folded().hasAttribute('hidden')).toBe(false)
        expect(toggle().getAttribute('aria-expanded')).toBe('true')
    })
})
