import { initSmoothScroll } from './smooth-scroll'

function click(id: string): MouseEvent {
    const event = new MouseEvent('click', { bubbles: true, cancelable: true })
    document.getElementById(id)!.dispatchEvent(event)
    return event
}

describe('smooth scroll for in-page links', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <article id="markdown-content">
                <a id="doc-link" href="#doc-target">doc</a>
                <h2 id="doc-target">Target</h2>
                <section id="elastic-api-v3">
                    <a id="api-link" href="#api-target">api</a>
                    <div hidden="until-found"><dt id="api-target">field</dt></div>
                </section>
            </article>`
        Element.prototype.scrollIntoView = jest.fn()
        initSmoothScroll()
    })

    it('scrolls to a docs page target without a hash navigation', () => {
        expect(click('doc-link').defaultPrevented).toBe(true)
        expect(Element.prototype.scrollIntoView).toHaveBeenCalled()
    })

    it('leaves API reference links to the hashchange that reveals their target', () => {
        expect(click('api-link').defaultPrevented).toBe(false)
        expect(Element.prototype.scrollIntoView).not.toHaveBeenCalled()
    })
})
