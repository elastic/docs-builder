import { initApiDocs } from './api-docs'
import type { Instance } from 'tippy.js'

describe('property anchor copy', () => {
    const writeText = vi.fn().mockResolvedValue(undefined)

    beforeAll(() => {
        Object.defineProperty(navigator, 'clipboard', {
            value: { writeText },
            configurable: true,
        })
        // The link still scrolls to the property, which jsdom cannot do
        Element.prototype.scrollIntoView = vi.fn()
    })

    beforeEach(() => {
        writeText.mockClear()
        document.body.innerHTML = `
            <section id="elastic-api-v3">
                <dt id="query-pretty">
                    <a class="property-anchor" href="#query-pretty" aria-label="Link to pretty" data-tippy-content="Copy link"></a>
                    <code>pretty</code>
                </dt>
            </section>
        `
        initApiDocs()
    })

    it('copies the full URL of the property when the icon is clicked', () => {
        document.querySelector<HTMLElement>('a.property-anchor')!.click()

        expect(writeText).toHaveBeenCalledWith(
            `${window.location.href.split('#')[0]}#query-pretty`
        )
    })

    it('shows "Copied!" in the icon tooltip after the copy', async () => {
        const icon = document.querySelector<HTMLElement>('a.property-anchor')!
        icon.click()
        await Promise.resolve()

        expect(icon.dataset.tippyContent).toBe('Copied!')
    })

    it('keeps the icon tooltip open on click', () => {
        const icon = document.querySelector<HTMLElement>('a.property-anchor')!
        icon.dispatchEvent(new MouseEvent('mouseover', { bubbles: true }))
        const instance = (icon as HTMLElement & { _tippy?: Instance })._tippy

        expect(instance?.props.hideOnClick).toBe(false)
    })

    it('does not copy when the property name is clicked', () => {
        document.querySelector<HTMLElement>('dt > code')!.click()

        expect(writeText).not.toHaveBeenCalled()
    })
})
