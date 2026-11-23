import { initApiDocs } from './api-docs'

// A group page is rendered inside #elastic-api-v3 like an operation page, so initApiDocs runs there too. Its On this page
// rail has data-toc hooks for the design system scroll spy, and the production bundle has its own (toc-nav.ts).
function rect(top: number): () => DOMRect {
    return () => ({ top }) as DOMRect
}

describe('initApiDocs outlines', () => {
    beforeEach(() => {
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            value: 5000,
            configurable: true,
        })
        Object.defineProperty(window, 'innerHeight', {
            value: 800,
            configurable: true,
        })
        window.requestAnimationFrame = (cb: FrameRequestCallback) => {
            cb(0)
            return 0
        }
    })

    it('leaves the On this page rail of a group page to the production scroll spy', () => {
        document.body.innerHTML = `
        <section id="elastic-api-v3" class="api-group"><h2 id="endpoints">Endpoints</h2></section>
        <nav id="toc-nav" class="api-rail-toc" data-toc>
          <a href="#endpoints" data-toc-link>Endpoints</a>
        </nav>`
        document.getElementById('endpoints')!.getBoundingClientRect = rect(-300)

        initApiDocs()

        const rail = document.getElementById('toc-nav')!
        expect(rail.hasAttribute('data-toc-bound')).toBe(false)
        expect(rail.querySelector('a')!.hasAttribute('aria-current')).toBe(
            false
        )
    })
})
