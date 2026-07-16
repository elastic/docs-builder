import { initTocNav } from './toc-nav'

function createRect(top: number, height: number): DOMRect {
    return {
        bottom: top + height,
        height,
        left: 0,
        right: 0,
        top,
        width: 0,
        x: 0,
        y: top,
        toJSON: () => ({}),
    }
}

describe('TOC navigation', () => {
    it('marks a stepper heading with its own id as current', () => {
        document.body.innerHTML = `
            <main id="markdown-content">
                <div class="stepper">
                    <ol>
                        <li class="step">
                            <h2 id="install"><a href="#install">Install</a></h2>
                        </li>
                    </ol>
                </div>
            </main>
            <nav id="toc-nav">
                <div class="toc-progress-container">
                    <div class="toc-progress-indicator"></div>
                    <ul><li><a href="#install">Install</a></li></ul>
                </div>
            </nav>
        `

        Object.defineProperty(window, 'innerHeight', {
            configurable: true,
            value: 800,
        })
        Object.defineProperty(window, 'scrollY', {
            configurable: true,
            value: 0,
        })
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            configurable: true,
            value: 2000,
        })

        const heading = document.querySelector('h2')!
        const tocContainer = document.querySelector('.toc-progress-container')!
        const tocItem = document.querySelector('#toc-nav li')!
        jest.spyOn(heading, 'getBoundingClientRect').mockReturnValue(
            createRect(100, 30)
        )
        jest.spyOn(tocContainer, 'getBoundingClientRect').mockReturnValue(
            createRect(10, 100)
        )
        jest.spyOn(tocItem, 'getBoundingClientRect').mockReturnValue(
            createRect(20, 24)
        )

        initTocNav()

        const tocLink = document.querySelector('#toc-nav a')!
        const indicator = document.querySelector(
            '.toc-progress-indicator'
        ) as HTMLElement
        expect(tocLink).toHaveClass('current')
        expect(indicator).toHaveStyle({ top: '10px', height: '24px' })
    })
})
