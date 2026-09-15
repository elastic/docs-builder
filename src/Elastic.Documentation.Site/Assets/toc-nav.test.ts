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

interface TocFixture {
    headings: HTMLElement[]
    headingRects: jest.SpyInstance<DOMRect, []>[]
    links: HTMLAnchorElement[]
    indicator: HTMLElement
}

let animationFrames: FrameRequestCallback[]

function setWindowValue(property: string, value: number) {
    Object.defineProperty(window, property, {
        configurable: true,
        value,
    })
}

function flushAnimationFrame() {
    const callbacks = animationFrames
    animationFrames = []
    callbacks.forEach((callback) => callback(0))
}

function scrollTo(scrollY: number) {
    setWindowValue('scrollY', scrollY)
    window.dispatchEvent(new Event('scroll'))
    flushAnimationFrame()
}

function createTocFixture(documentTops: number[]): TocFixture {
    const headings = documentTops
        .map(
            (_, index) => `
                <div class="heading-wrapper" id="heading-${index}">
                    <h2>Heading ${index}</h2>
                </div>
            `
        )
        .join('')
    const links = documentTops
        .map(
            (_, index) =>
                `<li><a href="#heading-${index}">Heading ${index}</a></li>`
        )
        .join('')

    document.body.innerHTML = `
        <main id="markdown-content">${headings}</main>
        <nav id="toc-nav">
            <div class="toc-progress-container">
                <div class="toc-progress-indicator"></div>
                <ul>${links}</ul>
            </div>
        </nav>
    `

    const headingElements = Array.from(
        document.querySelectorAll<HTMLElement>('#markdown-content h2')
    )
    const headingRects = headingElements.map((heading, index) =>
        jest
            .spyOn(heading, 'getBoundingClientRect')
            .mockImplementation(() =>
                createRect(documentTops[index] - window.scrollY, 30)
            )
    )
    const linkElements = Array.from(
        document.querySelectorAll<HTMLAnchorElement>('#toc-nav a')
    )
    linkElements.forEach((link, index) => {
        jest.spyOn(
            link.closest('li')!,
            'getBoundingClientRect'
        ).mockImplementation(() => createRect(20 + index * 30, 24))
    })
    jest.spyOn(
        document.querySelector('.toc-progress-container')!,
        'getBoundingClientRect'
    ).mockReturnValue(createRect(10, documentTops.length * 30))

    return {
        headings: headingElements,
        headingRects,
        links: linkElements,
        indicator: document.querySelector('.toc-progress-indicator')!,
    }
}

describe('TOC navigation', () => {
    beforeEach(() => {
        animationFrames = []
        setWindowValue('innerHeight', 800)
        setWindowValue('scrollY', 0)
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            configurable: true,
            value: 2000,
        })
        jest.spyOn(window, 'requestAnimationFrame').mockImplementation(
            (callback) => {
                animationFrames.push(callback)
                return animationFrames.length
            }
        )
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

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
        expect(tocLink).toHaveClass('toc-current')
        expect(indicator).toHaveStyle({
            top: '0px',
            height: '24px',
            transform: 'translateY(10px)',
        })
    })

    it('marks an API heading using its data-section anchor as current', () => {
        document.body.innerHTML = `
            <main id="elastic-api-v3">
                <h3 data-section="responses">Responses</h3>
            </main>
            <nav id="toc-nav">
                <div class="toc-progress-container">
                    <div class="toc-progress-indicator"></div>
                    <ul><li><a href="#responses">Responses</a></li></ul>
                </div>
            </nav>
        `
        jest.spyOn(
            document.querySelector('h3')!,
            'getBoundingClientRect'
        ).mockReturnValue(createRect(100, 30))
        jest.spyOn(
            document.querySelector('.toc-progress-container')!,
            'getBoundingClientRect'
        ).mockReturnValue(createRect(10, 100))
        jest.spyOn(
            document.querySelector('#toc-nav li')!,
            'getBoundingClientRect'
        ).mockReturnValue(createRect(20, 24))

        initTocNav()

        expect(document.querySelector('#toc-nav a')).toHaveClass('toc-current')
    })

    it('tracks headings while scrolling forward and backward', () => {
        const fixture = createTocFixture([100, 300, 500])

        initTocNav()

        expect(fixture.links[0]).toHaveClass('toc-current')

        scrollTo(200)
        expect(fixture.links[0]).not.toHaveClass('toc-current')
        expect(fixture.links[1]).toHaveClass('toc-current')

        scrollTo(0)
        expect(fixture.links[0]).toHaveClass('toc-current')
        expect(fixture.links[1]).not.toHaveClass('toc-current')
    })

    it('marks headings at the same position as current', () => {
        const fixture = createTocFixture([100, 300, 300, 500])

        initTocNav()
        scrollTo(200)

        expect(fixture.links[1]).toHaveClass('toc-current')
        expect(fixture.links[2]).toHaveClass('toc-current')
    })

    it('marks all visible headings at the bottom of the page', () => {
        setWindowValue('innerHeight', 400)
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            configurable: true,
            value: 1000,
        })
        const fixture = createTocFixture([100, 650, 850, 950])

        initTocNav()
        scrollTo(600)

        expect(fixture.links[1]).not.toHaveClass('toc-current')
        expect(fixture.links[2]).toHaveClass('toc-current')
        expect(fixture.links[3]).toHaveClass('toc-current')
        expect(fixture.indicator).toHaveStyle({
            transform: 'translateY(70px)',
            height: '54px',
        })
    })

    it('marks a visible heading at the bottom before it reaches the header', () => {
        Object.defineProperty(document.documentElement, 'scrollHeight', {
            configurable: true,
            value: 1000,
        })
        const fixture = createTocFixture([400])

        initTocNav()
        scrollTo(200)

        expect(fixture.links[0]).toHaveClass('toc-current')
    })

    it('coalesces repeated scroll events into one animation frame', () => {
        const fixture = createTocFixture([100, 300, 500])
        initTocNav()
        fixture.headingRects.forEach((mock) => mock.mockClear())
        jest.mocked(window.requestAnimationFrame).mockClear()

        window.dispatchEvent(new Event('scroll'))
        window.dispatchEvent(new Event('scroll'))
        window.dispatchEvent(new Event('scroll'))

        expect(window.requestAnimationFrame).toHaveBeenCalledTimes(1)
        expect(
            fixture.headingRects.reduce(
                (calls, mock) => calls + mock.mock.calls.length,
                0
            )
        ).toBe(0)

        flushAnimationFrame()
        expect(
            fixture.headingRects.reduce(
                (calls, mock) => calls + mock.mock.calls.length,
                0
            )
        ).toBeGreaterThan(0)
    })

    it('does not rewrite classes when the current heading is unchanged', () => {
        const fixture = createTocFixture([100, 300, 500])
        initTocNav()
        const removeSpies = fixture.links.map((link) =>
            jest.spyOn(link.classList, 'remove')
        )
        const addSpies = fixture.links.map((link) =>
            jest.spyOn(link.classList, 'add')
        )

        scrollTo(10)

        expect(removeSpies.every((spy) => spy.mock.calls.length === 0)).toBe(
            true
        )
        expect(addSpies.every((spy) => spy.mock.calls.length === 0)).toBe(true)
    })

    it('only reads nearby heading geometry during steady scrolling', () => {
        const fixture = createTocFixture(
            Array.from({ length: 50 }, (_, index) => 100 + index * 200)
        )
        initTocNav()
        fixture.headingRects.forEach((mock) => mock.mockClear())

        scrollTo(10)

        const geometryReads = fixture.headingRects.reduce(
            (calls, mock) => calls + mock.mock.calls.length,
            0
        )
        expect(geometryReads).toBeLessThanOrEqual(3)
    })

    it('removes listeners from the previous initialization', () => {
        const previousFixture = createTocFixture([100, 300])
        initTocNav()
        previousFixture.headingRects.forEach((mock) => mock.mockClear())

        createTocFixture([100, 400])
        initTocNav()
        previousFixture.headingRects.forEach((mock) => mock.mockClear())

        scrollTo(10)

        expect(
            previousFixture.headingRects.every(
                (mock) => mock.mock.calls.length === 0
            )
        ).toBe(true)
    })
})
