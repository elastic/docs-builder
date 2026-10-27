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
})
