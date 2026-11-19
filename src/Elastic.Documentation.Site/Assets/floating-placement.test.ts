import { initFloating } from './floating'

// Unlike floating.test.ts, Floating UI is not mocked: the real middleware places the menu against stubbed geometry,
// so this fails when the menu can run off the screen or lose its alignment with the button.

const VIEWPORT = { width: 375, height: 800 }

function rect(left: number, top: number, width: number, height: number) {
    return {
        x: left,
        y: top,
        left,
        top,
        width,
        height,
        right: left + width,
        bottom: top + height,
        toJSON: () => ({}),
    } as DOMRect
}

function open(menu: HTMLElement) {
    for (const type of ['beforetoggle', 'toggle']) {
        const event = new Event(type)
        Object.assign(event, { newState: 'open' })
        menu.dispatchEvent(event)
    }
}

const settle = () => new Promise((resolve) => setTimeout(resolve, 0))

describe('menu placement on a phone', () => {
    beforeAll(() => {
        Object.assign(HTMLElement.prototype, {
            showPopover: vi.fn(),
            hidePopover: vi.fn(),
        })
        for (const [name, value] of [
            ['clientWidth', VIEWPORT.width],
            ['clientHeight', VIEWPORT.height],
        ] as const)
            Object.defineProperty(document.documentElement, name, {
                configurable: true,
                value,
            })
        initFloating()
    })

    afterEach(() => {
        document.dispatchEvent(new Event('htmx:beforeSwap'))
        document.body.innerHTML = ''
    })

    function setUp(chevron: DOMRect, size: { width: number; height: number }) {
        document.body.innerHTML = `
            <button id="chevron" popovertarget="menu"></button>
            <div id="menu" popover data-placement="bottom-end"></div>`
        const menu = document.getElementById('menu')!
        document.getElementById('chevron')!.getBoundingClientRect = () =>
            chevron
        menu.getBoundingClientRect = () => rect(0, 0, size.width, size.height)
        for (const [name, value] of [
            ['offsetWidth', size.width],
            ['offsetHeight', size.height],
        ] as const)
            Object.defineProperty(menu, name, { configurable: true, value })
        return menu
    }

    it('keeps a menu wider than the room left of the chevron inside the screen, under the chevron', async () => {
        // Copy page's chevron at the right edge of a 375px screen, with a 273px menu: aligning the menu's right edge to
        // the chevron's would leave 1px past the 8px margin.
        const chevron = rect(341, 146, 32, 30)
        const menu = setUp(chevron, { width: 273, height: 157 })

        open(menu)
        await settle()

        const left = parseFloat(menu.style.left)
        const top = parseFloat(menu.style.top)
        expect(left).toBeGreaterThanOrEqual(8)
        expect(left + 273).toBeLessThanOrEqual(VIEWPORT.width - 8)
        expect(left + 273).toBeLessThanOrEqual(chevron.right)
        expect(top).toBeGreaterThanOrEqual(chevron.bottom)
        expect(top).toBeLessThan(chevron.bottom + 16)
    })

    it('opens above the chevron when there is no room below', async () => {
        const chevron = rect(341, 740, 32, 30)
        const menu = setUp(chevron, { width: 273, height: 157 })

        open(menu)
        await settle()

        expect(parseFloat(menu.style.top) + 157).toBeLessThanOrEqual(
            chevron.top
        )
    })
})
