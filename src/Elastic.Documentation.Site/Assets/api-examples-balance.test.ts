import { balanceCodeHeights, initExamplesBalance } from './api-examples-balance'

const floor = 112

describe('balanceCodeHeights', () => {
    it('shows both in full when they fit', () => {
        expect(
            balanceCodeHeights(500, { request: 200, response: 100 }, floor)
        ).toEqual({ request: 200, response: 100 })
    })

    it('gives a one-line response its full height and the rest to the request', () => {
        expect(
            balanceCodeHeights(300, { request: 360, response: 24 }, floor)
        ).toEqual({ request: 276, response: 24 })
    })

    it('gives a short request its full height and the rest to the response', () => {
        expect(
            balanceCodeHeights(400, { request: 120, response: 4644 }, floor)
        ).toEqual({ request: 120, response: 280 })
    })

    it('splits the room evenly between two long samples', () => {
        expect(
            balanceCodeHeights(400, { request: 360, response: 4644 }, floor)
        ).toEqual({ request: 200, response: 200 })
    })

    it('keeps five lines of each when the window is too short', () => {
        expect(
            balanceCodeHeights(-50, { request: 360, response: 4644 }, floor)
        ).toEqual({ request: 112, response: 112 })
    })

    it('never grows a sample past its own height to reach the floor', () => {
        expect(
            balanceCodeHeights(0, { request: 360, response: 42 }, floor)
        ).toEqual({ request: 112, response: 42 })
    })
})

interface Sample {
    lang: string
    scroll: number
    offset?: number
}

function measure(
    element: Element | null,
    sizes: {
        height?: number
        scroll?: number
        offset?: number
        client?: number
    }
) {
    if (!element) throw new Error('missing element')
    const height = sizes.height ?? 0
    element.getBoundingClientRect = () =>
        ({ top: 0, bottom: height, height }) as DOMRect
    for (const [key, value] of [
        ['scrollHeight', sizes.scroll],
        ['offsetHeight', sizes.offset ?? sizes.scroll],
        ['clientHeight', sizes.client ?? sizes.scroll],
    ] as const)
        if (value !== undefined)
            Object.defineProperty(element, key, { configurable: true, value })
}

/**
 * Around the code there are 202px: the rail's 100px outside the panel, and 102px in the panel outside the strip's
 * code and the response body. So the code gets the rail's max-height minus 302px.
 */
function rail(maxHeight: number, samples: Sample[], response: Sample) {
    const cards = samples
        .map(
            ({ lang }, i) =>
                `<section class="api-code-carousel-card${i === 0 ? ' is-active' : ''}" data-lang="${lang}"><header></header><div class="api-code-carousel-card-body"></div></section>`
        )
        .join('')
    const host = document.createElement('div')
    host.innerHTML = `
        <div class="api-page-rail" style="max-height: ${maxHeight}px">
            <div id="api-examples-panel" style="--api-code-cap: 360px">
                <div data-api-examples>
                    <div data-api-carousel><div data-carousel-strip>${cards}</div></div>
                    <div class="example-block--response"><div class="example-block-body"></div></div>
                </div>
            </div>
        </div>`
    const root = host.firstElementChild as HTMLElement
    document.body.append(root)
    measure(root, { height: 600 })
    measure(root.querySelector('#api-examples-panel'), { height: 500 })
    measure(root.querySelector('[data-api-examples]'), { height: 500 })
    measure(root.querySelector('[data-carousel-strip]'), { height: 200 })
    root.querySelectorAll('.api-code-carousel-card-body').forEach((body, i) =>
        measure(body, samples[i])
    )
    measure(root.querySelector('.example-block-body'), {
        ...response,
        height: 100,
    })
    return root
}

function heights(root: HTMLElement) {
    const panel = root.querySelector<HTMLElement>('#api-examples-panel')!
    return {
        request: panel.style.getPropertyValue('--api-request-code-max'),
        response: panel.style.getPropertyValue('--api-response-code-max'),
    }
}

const longResponse = { lang: 'JSON', scroll: 4644 }

describe('initExamplesBalance', () => {
    beforeEach(() => {
        vi.stubGlobal('matchMedia', () => ({ matches: true }))
        vi.stubGlobal('requestAnimationFrame', (run: FrameRequestCallback) => {
            run(0)
            return 1
        })
        vi.stubGlobal('cancelAnimationFrame', () => {})
        vi.stubGlobal(
            'ResizeObserver',
            class {
                observe() {}
                disconnect() {}
            }
        )
    })

    afterEach(() => {
        document.body.replaceChildren()
        vi.unstubAllGlobals()
    })

    it('stops balancing a panel once HTMX has swapped it out', () => {
        const old = rail(602, [{ lang: 'Console', scroll: 400 }], longResponse)
        initExamplesBalance()
        old.remove()
        const current = rail(
            602,
            [{ lang: 'Console', scroll: 400 }],
            longResponse
        )
        initExamplesBalance()

        old.style.maxHeight = '1002px'
        current.style.maxHeight = '1002px'
        window.dispatchEvent(new Event('resize'))

        expect([heights(old), heights(current)]).toEqual([
            { request: '150px', response: '150px' },
            { request: '350px', response: '350px' },
        ])
    })
})
