import { initApiDocs } from './api-docs'
import { beforeAll, describe, expect, it } from 'vitest'

describe('section links', () => {
    beforeAll(() => initApiDocs())

    it('marks the first link of the bar on an operation page, through the outline scroll-spy', () => {
        document.body.innerHTML = `
            <section id="elastic-api-v3">
                <nav class="api-section-links" data-toc>
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
})
