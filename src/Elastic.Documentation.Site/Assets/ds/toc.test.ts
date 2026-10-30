import { initToc } from './toc'

function setup() {
    document.body.innerHTML = `
    <nav data-toc>
      <a data-toc-link href="#one">One</a>
      <a data-toc-link href="#two">Two</a>
    </nav>
    <h2 id="one">One</h2><h2 id="two">Two</h2>`
    const rect = (top: number) => ({ top }) as DOMRect
    document.getElementById('one')!.getBoundingClientRect = () => rect(-300)
    document.getElementById('two')!.getBoundingClientRect = () => rect(2000)
}

describe('initToc', () => {
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

    it('marks the heading above the activation line', () => {
        setup()
        initToc()

        expect(
            document
                .querySelector('[href="#one"]')!
                .getAttribute('aria-current')
        ).toBe('location')
        expect(
            document
                .querySelector('[href="#two"]')!
                .hasAttribute('aria-current')
        ).toBe(false)
    })

    it('moves the mark as the page scrolls', () => {
        setup()
        initToc()
        document.getElementById('two')!.getBoundingClientRect = () =>
            ({ top: 100 }) as DOMRect
        window.dispatchEvent(new Event('scroll'))

        expect(
            document
                .querySelector('[href="#two"]')!
                .getAttribute('aria-current')
        ).toBe('location')
        expect(
            document
                .querySelector('[href="#one"]')!
                .hasAttribute('aria-current')
        ).toBe(false)
    })

    it('falls back to the first entry before any heading is reached', () => {
        setup()
        document.getElementById('one')!.getBoundingClientRect = () =>
            ({ top: 500 }) as DOMRect
        initToc()

        expect(
            document
                .querySelector('[href="#one"]')!
                .getAttribute('aria-current')
        ).toBe('location')
    })

    it('marks the last heading at the bottom of the page, though it never reaches the line', () => {
        setup()
        document.getElementById('two')!.getBoundingClientRect = () =>
            ({ top: 700 }) as DOMRect
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            value: 800,
            configurable: true,
        })
        initToc()

        expect(
            document
                .querySelector('[href="#two"]')!
                .getAttribute('aria-current')
        ).toBe('location')
        expect(
            document
                .querySelector('[href="#one"]')!
                .hasAttribute('aria-current')
        ).toBe(false)
    })

    it('stops tracking an outline that left the page', () => {
        setup()
        const toc = document.querySelector('[data-toc]')!
        const oldHeading = document.getElementById('two')!
        initToc()
        // a new page arrives and its outline is bound too
        setup()
        document.getElementById('two')!.getBoundingClientRect = () =>
            ({ top: 100 }) as DOMRect
        initToc()
        // the old heading now crosses the line, so a spy still tracking the old page would mark its link
        oldHeading.getBoundingClientRect = () => ({ top: 100 }) as DOMRect
        window.dispatchEvent(new Event('scroll'))

        expect(
            toc.querySelector('[href="#two"]')!.hasAttribute('aria-current')
        ).toBe(false)
        expect(
            document
                .querySelector('[href="#two"]')!
                .getAttribute('aria-current')
        ).toBe('location')
    })
})
