import { initApiDocs } from './api-docs'

describe('property anchor copy', () => {
    const writeText = jest.fn().mockResolvedValue(undefined)

    beforeAll(() => {
        Object.defineProperty(navigator, 'clipboard', {
            value: { writeText },
            configurable: true,
        })
    })

    beforeEach(() => {
        writeText.mockClear()
        document.body.innerHTML = `
            <section id="elastic-api-v3">
                <dt id="query-pretty">
                    <a class="property-anchor" href="#query-pretty">
                        <span class="property-anchor-icon" data-tippy-content="Copy link" aria-hidden="true"></span>
                        <code>pretty</code>
                    </a>
                </dt>
            </section>
        `
        initApiDocs()
    })

    it('copies the full URL of the property when the icon is clicked', () => {
        document.querySelector<HTMLElement>('.property-anchor-icon')!.click()

        expect(writeText).toHaveBeenCalledWith(
            `${window.location.href.split('#')[0]}#query-pretty`
        )
    })

    it('does not copy when the property name is clicked', () => {
        document.querySelector<HTMLElement>('.property-anchor code')!.click()

        expect(writeText).not.toHaveBeenCalled()
    })
})
