import { initListings } from './listing'

const markup = `
<div data-listing>
  <div data-listing-bar hidden>
    <input data-listing-filter>
    <button data-listing-chip="" aria-pressed="true">All</button>
    <button data-listing-chip="guides" aria-pressed="false">Guides</button>
    <button data-listing-chip="ref" aria-pressed="false">Reference</button>
  </div>
  <div data-listing-section="guides"><a data-listing-card data-listing-group="guides" data-listing-title="quick start"></a></div>
  <div data-listing-section="ref"><a data-listing-card data-listing-group="ref" data-listing-title="query dsl"></a></div>
  <p data-listing-empty hidden></p>
</div>`

const cards = () =>
    Array.from(document.querySelectorAll<HTMLElement>('[data-listing-card]'))
const chip = (group: string) =>
    document.querySelector<HTMLButtonElement>(`[data-listing-chip="${group}"]`)!

describe('listing filter', () => {
    beforeEach(() => {
        document.body.innerHTML = markup
        initListings()
    })

    it('reveals the filter bar', () => {
        expect(
            document.querySelector<HTMLElement>('[data-listing-bar]')!.hidden
        ).toBe(false)
    })

    it('filters cards by title and hides empty sections', () => {
        const input = document.querySelector<HTMLInputElement>(
            '[data-listing-filter]'
        )!
        input.value = 'query'
        input.dispatchEvent(new Event('input'))

        expect(cards().map((c) => c.hidden)).toEqual([true, false])
        expect(
            document.querySelector<HTMLElement>(
                '[data-listing-section="guides"]'
            )!.hidden
        ).toBe(true)
    })

    it('shows the empty notice when nothing matches', () => {
        const input = document.querySelector<HTMLInputElement>(
            '[data-listing-filter]'
        )!
        input.value = 'zzz'
        input.dispatchEvent(new Event('input'))

        expect(
            document.querySelector<HTMLElement>('[data-listing-empty]')!.hidden
        ).toBe(false)
    })

    it('selects one group on click and clears it on the second click', () => {
        chip('guides').click()
        expect(cards().map((c) => c.hidden)).toEqual([false, true])
        expect(chip('guides').getAttribute('aria-pressed')).toBe('true')
        expect(chip('').getAttribute('aria-pressed')).toBe('false')

        chip('guides').click()
        expect(cards().map((c) => c.hidden)).toEqual([false, false])
    })

    it('adds groups with a modifier click', () => {
        chip('guides').click()
        chip('ref').dispatchEvent(
            new MouseEvent('click', { bubbles: true, ctrlKey: true })
        )

        expect(cards().map((c) => c.hidden)).toEqual([false, false])
    })

    it('binds once', () => {
        initListings()
        expect(document.querySelectorAll('[data-listing-bound]')).toHaveLength(
            1
        )
    })
})
