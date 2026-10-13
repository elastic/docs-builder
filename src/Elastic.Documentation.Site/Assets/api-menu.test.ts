import { initApiMenus } from './api-menu'
import { autoUpdate, computePosition } from '@floating-ui/dom'

jest.mock('@floating-ui/dom', () => ({
    autoUpdate: jest.fn(),
    computePosition: jest.fn(),
    flip: jest.fn(() => 'flip'),
    offset: jest.fn(() => 'offset'),
    shift: jest.fn(() => 'shift'),
    size: jest.fn(() => 'size'),
}))

const stopTracking = jest.fn()
const hidePopover = jest.fn()

function toggle(menu: HTMLElement, newState: 'open' | 'closed') {
    const event = new Event('toggle')
    Object.assign(event, { newState })
    menu.dispatchEvent(event)
}

/** Floating UI loads on a dynamic import, so let that settle. */
const settle = () => new Promise((resolve) => setTimeout(resolve, 0))

describe('api menus', () => {
    beforeAll(() => {
        // jsdom has no popover API; initApiMenus only listens where it exists.
        Object.assign(HTMLElement.prototype, {
            showPopover: jest.fn(),
            hidePopover,
        })
        initApiMenus()
    })

    beforeEach(() => {
        jest.clearAllMocks()
        ;(autoUpdate as jest.Mock).mockImplementation(
            (_trigger, _menu, update: () => void) => {
                update()
                return stopTracking
            }
        )
        ;(computePosition as jest.Mock).mockResolvedValue({ x: 12, y: 34 })
        document.body.innerHTML = `
            <button id="trigger" popovertarget="menu"></button>
            <div id="menu" class="api-menu" popover data-placement="bottom-end"></div>`
    })

    it('tracks the button and places the menu when it opens', async () => {
        const menu = document.getElementById('menu')!

        toggle(menu, 'open')
        await settle()

        expect(
            document.getElementById('trigger')!.getAttribute('aria-expanded')
        ).toBe('true')
        expect(autoUpdate).toHaveBeenCalledTimes(1)
        expect(computePosition).toHaveBeenCalledWith(
            document.getElementById('trigger'),
            menu,
            expect.objectContaining({
                strategy: 'fixed',
                placement: 'bottom-end',
            })
        )
        expect(menu.style.left).toBe('12px')
        expect(menu.style.top).toBe('34px')
        expect(menu.hasAttribute('data-placed')).toBe(true)
    })

    it('stops tracking and hides the menu again when it closes', async () => {
        const menu = document.getElementById('menu')!
        toggle(menu, 'open')
        await settle()

        toggle(menu, 'closed')

        expect(stopTracking).toHaveBeenCalledTimes(1)
        expect(
            document.getElementById('trigger')!.getAttribute('aria-expanded')
        ).toBe('false')
        expect(menu.hasAttribute('data-placed')).toBe(false)
    })

    it('does not track a menu that closed while Floating UI was loading', async () => {
        const menu = document.getElementById('menu')!

        toggle(menu, 'open')
        toggle(menu, 'closed')
        await settle()

        expect(autoUpdate).not.toHaveBeenCalled()
        expect(menu.hasAttribute('data-placed')).toBe(false)
    })

    it('closes open menus before an htmx swap', async () => {
        const menu = document.getElementById('menu')!
        toggle(menu, 'open')
        await settle()

        document.dispatchEvent(new Event('htmx:beforeSwap'))

        expect(hidePopover).toHaveBeenCalledTimes(1)
    })

    it('ignores popovers that are not API menus', () => {
        document.body.innerHTML = '<div id="other" popover></div>'

        toggle(document.getElementById('other')!, 'open')

        expect(autoUpdate).not.toHaveBeenCalled()
    })
})
