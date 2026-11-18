import { initApiDocs } from './api-docs'
import { beforeAll, describe, expect, it, vi } from 'vitest'

// An operation page with the pinned bar: its wrapper, a section link, and whatever else the test puts in the bar
function mountPinnedBar(extra = ''): HTMLElement {
    document.body.innerHTML = `
        <section id="elastic-api-v3">
            <div class="api-section-pin" data-section-pin>
                <nav class="api-section-links" data-toc data-toc-sticky>
                    <a href="#responses" data-toc-link>Response</a>
                    ${extra}
                </nav>
            </div>
            <h2 id="responses"></h2>
        </section>`
    return document.querySelector<HTMLElement>('[data-section-pin]')!
}

describe('section links', () => {
    beforeAll(() => initApiDocs())

    it('marks the first link of the bar on an operation page, through the outline scroll-spy', () => {
        document.body.innerHTML = `
            <section id="elastic-api-v3">
                <nav class="api-section-links" data-toc data-toc-sticky>
                    <a href="#query-params" data-toc-link>Query</a>
                    <a href="#responses" data-toc-link>Response</a>
                </nav>
                <h2 id="query-params"></h2>
                <h2 id="responses"></h2>
            </section>`
        // jsdom has no layout: a page taller than the window, scrolled to the top
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            value: 5000,
            configurable: true,
        })
        // jsdom has no layout: the first section is in view, the second is below the fold
        const top = { 'query-params': -100, responses: 900 }
        for (const [id, y] of Object.entries(top))
            document.getElementById(id)!.getBoundingClientRect = () =>
                ({ top: y }) as DOMRect
        initApiDocs()

        const current = document.querySelectorAll('[aria-current]')
        expect(current).toHaveLength(1)
        expect(current[0].getAttribute('href')).toBe('#query-params')
    })

    it('marks no link while the bar is on screen above every section', () => {
        document.body.innerHTML = `
            <section id="elastic-api-v3">
                <nav class="api-section-links" data-toc data-toc-sticky>
                    <a href="#query-params" data-toc-link>Query</a>
                    <a href="#responses" data-toc-link>Response</a>
                </nav>
                <h2 id="query-params"></h2>
                <h2 id="responses"></h2>
            </section>`
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            value: 5000,
            configurable: true,
        })
        const top = { 'query-params': 600, responses: 1500 }
        for (const [id, y] of Object.entries(top))
            document.getElementById(id)!.getBoundingClientRect = () =>
                ({ top: y }) as DOMRect
        initApiDocs()

        expect(document.querySelectorAll('[aria-current]')).toHaveLength(0)
    })

    it('flags the pin as stuck while it sits at its pinned offset, and clears the flag above it', async () => {
        const bar = mountPinnedBar()
        bar.style.top = '56px'
        let top = 900
        bar.getBoundingClientRect = () => ({ top }) as DOMRect
        initApiDocs()
        expect(bar.hasAttribute('data-stuck')).toBe(false)

        top = 56
        window.dispatchEvent(new Event('scroll'))
        await new Promise((resolve) => requestAnimationFrame(resolve))
        expect(bar.hasAttribute('data-stuck')).toBe(true)

        top = 300
        window.dispatchEvent(new Event('scroll'))
        await new Promise((resolve) => requestAnimationFrame(resolve))
        expect(bar.hasAttribute('data-stuck')).toBe(false)
    })

    it('scrolls to the top of the page from the back to top button of the pinned bar', () => {
        mountPinnedBar(
            '<button type="button" data-api-back-to-top>Back to top</button>'
        )
        const scrollTo = vi.fn()
        window.scrollTo = scrollTo as unknown as typeof window.scrollTo

        document
            .querySelector<HTMLButtonElement>('[data-api-back-to-top]')!
            .click()

        expect(scrollTo).toHaveBeenCalledWith({ top: 0, behavior: 'smooth' })
    })

    it('scrolls to the top at once when the reader prefers reduced motion', () => {
        mountPinnedBar(
            '<button type="button" data-api-back-to-top>Back to top</button>'
        )
        const scrollTo = vi.fn()
        window.scrollTo = scrollTo as unknown as typeof window.scrollTo
        vi.stubGlobal('matchMedia', () => ({ matches: true }))

        document
            .querySelector<HTMLButtonElement>('[data-api-back-to-top]')!
            .click()

        expect(scrollTo).toHaveBeenCalledWith({ top: 0, behavior: 'auto' })
        vi.unstubAllGlobals()
    })
})
