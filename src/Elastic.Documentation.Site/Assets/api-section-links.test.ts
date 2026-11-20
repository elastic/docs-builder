import { initApiDocs } from './api-docs'
import { beforeAll, describe, expect, it, vi } from 'vitest'

// An operation page with the section bar: an Overview link back to the top, and the section it links to
function mountBar(): void {
    document.body.innerHTML = `
        <section id="elastic-api-v3">
            <nav class="api-section-links" data-toc data-toc-sticky>
                <a href="#api-operation-overview" data-toc-link data-api-back-to-top>Overview</a>
                <a href="#responses" data-toc-link>Responses</a>
            </nav>
            <h2 id="responses"></h2>
        </section>`
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

    it('scrolls to the top of the page from the Overview link of the bar', () => {
        mountBar()
        const scrollTo = vi.fn()
        window.scrollTo = scrollTo as unknown as typeof window.scrollTo

        document
            .querySelector<HTMLAnchorElement>('[data-api-back-to-top]')!
            .click()

        expect(scrollTo).toHaveBeenCalledWith({ top: 0, behavior: 'smooth' })
    })

    it('scrolls to the top at once when the reader prefers reduced motion', () => {
        mountBar()
        const scrollTo = vi.fn()
        window.scrollTo = scrollTo as unknown as typeof window.scrollTo
        vi.stubGlobal('matchMedia', () => ({ matches: true }))

        document
            .querySelector<HTMLAnchorElement>('[data-api-back-to-top]')!
            .click()

        expect(scrollTo).toHaveBeenCalledWith({ top: 0, behavior: 'auto' })
        vi.unstubAllGlobals()
    })

    it('puts the Overview link in the address, so a reload does not reopen the section left behind', () => {
        mountBar()
        window.scrollTo = vi.fn() as unknown as typeof window.scrollTo
        history.replaceState(null, '', '#responses')

        document
            .querySelector<HTMLAnchorElement>('[data-api-back-to-top]')!
            .click()

        expect(window.location.hash).toBe('#api-operation-overview')
    })

    it('adds the history entry before scrolling, so Back returns to the section left behind', () => {
        mountBar()
        history.replaceState(null, '', '#responses')
        const calls: string[] = []
        window.scrollTo = (() =>
            calls.push('scroll')) as unknown as typeof window.scrollTo
        const pushState = vi
            .spyOn(history, 'pushState')
            .mockImplementation(() => calls.push('push'))
        vi.stubGlobal('matchMedia', () => ({ matches: true }))

        document
            .querySelector<HTMLAnchorElement>('[data-api-back-to-top]')!
            .click()

        expect(calls).toEqual(['push', 'scroll'])
        pushState.mockRestore()
        vi.unstubAllGlobals()
    })

    it('leaves a modified click on the Overview link to the browser', () => {
        mountBar()
        const scrollTo = vi.fn()
        window.scrollTo = scrollTo as unknown as typeof window.scrollTo
        const link = document.querySelector<HTMLAnchorElement>(
            '[data-api-back-to-top]'
        )!
        const click = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
            metaKey: true,
        })
        // The page's handler listens on the document; the window hears the click after it. Read whether the handler
        // cancelled it there, then cancel it so jsdom does not try to navigate.
        let canceledByHandler: boolean | undefined
        const afterHandler = (e: Event) => {
            canceledByHandler = e.defaultPrevented
            e.preventDefault()
        }
        window.addEventListener('click', afterHandler)

        link.dispatchEvent(click)
        window.removeEventListener('click', afterHandler)

        expect(canceledByHandler).toBe(false)
        expect(scrollTo).not.toHaveBeenCalled()
    })
})
