import { initFloating } from './floating'
import { autoUpdate, computePosition } from '@floating-ui/dom'
import type { Mock } from 'vitest'

vi.mock('@floating-ui/dom', () => ({
    autoUpdate: vi.fn(),
    computePosition: vi.fn(),
    flip: vi.fn(() => 'flip'),
    offset: vi.fn(() => 'offset'),
    shift: vi.fn(() => 'shift'),
    size: vi.fn(() => 'size'),
}))

const stopTracking = vi.fn()
const hidePopover = vi.fn()

function dispatch(
    popover: HTMLElement,
    type: 'beforetoggle' | 'toggle',
    newState: 'open' | 'closed'
) {
    const event = new Event(type)
    Object.assign(event, { newState })
    popover.dispatchEvent(event)
}

/** What the browser does when a popover opens or closes. */
function setOpen(popover: HTMLElement, open: boolean) {
    const state = open ? 'open' : 'closed'
    dispatch(popover, 'beforetoggle', state)
    dispatch(popover, 'toggle', state)
}

const settle = () => new Promise((resolve) => setTimeout(resolve, 0))

describe('floating popovers', () => {
    beforeAll(() => {
        // jsdom has no popover API; initFloating only listens where it exists.
        Object.assign(HTMLElement.prototype, {
            showPopover: vi.fn(),
            hidePopover,
        })
        initFloating()
    })

    beforeEach(() => {
        // Releases what the previous test left open, the way a page swap does.
        document.dispatchEvent(new Event('htmx:beforeSwap'))
        vi.clearAllMocks()
        ;(autoUpdate as Mock).mockImplementation(
            (_anchor, _floating, update: () => void) => {
                update()
                return stopTracking
            }
        )
        ;(computePosition as Mock).mockResolvedValue({ x: 12, y: 34 })
        document.body.innerHTML = `
            <button id="trigger" popovertarget="menu"></button>
            <div id="menu" popover data-placement="bottom-end"></div>`
    })

    const menu = () => document.getElementById('menu')!
    const trigger = () => document.getElementById('trigger')!

    it('keeps the menu hidden from its first frame until it is placed', () => {
        dispatch(menu(), 'beforetoggle', 'open')

        expect(menu().hasAttribute('data-floating')).toBe(true)
        expect(menu().hasAttribute('data-placed')).toBe(false)
    })

    it('places the menu beside the button that opens it and follows it', async () => {
        setOpen(menu(), true)
        await settle()

        expect(trigger().getAttribute('aria-expanded')).toBe('true')
        expect(autoUpdate).toHaveBeenCalledTimes(1)
        expect(computePosition).toHaveBeenCalledWith(
            trigger(),
            menu(),
            expect.objectContaining({
                strategy: 'fixed',
                placement: 'bottom-end',
                middleware: ['offset', 'flip', 'shift', 'size'],
            })
        )
        expect(menu().style.left).toBe('12px')
        expect(menu().style.top).toBe('34px')
        expect(menu().hasAttribute('data-placed')).toBe(true)
    })

    it('opens below the start of its button when no placement is given', async () => {
        menu().removeAttribute('data-placement')

        setOpen(menu(), true)
        await settle()

        expect(computePosition).toHaveBeenCalledWith(
            trigger(),
            menu(),
            expect.objectContaining({ placement: 'bottom-start' })
        )
    })

    it('anchors to the element data-anchor names instead of the button', async () => {
        document.body.insertAdjacentHTML('beforeend', '<input id="search" />')
        menu().dataset.anchor = 'search'

        setOpen(menu(), true)
        await settle()

        expect(computePosition).toHaveBeenCalledWith(
            document.getElementById('search'),
            menu(),
            expect.anything()
        )
    })

    it('announces the first placement once, since the menu cannot take focus before', async () => {
        const placed = vi.fn()
        menu().addEventListener('floating:placed', placed)

        setOpen(menu(), true)
        await settle()
        // The anchor moves and the menu is placed again; that is not a first placement.
        const update = (autoUpdate as Mock).mock.calls[0][2] as () => void
        update()
        await settle()

        expect(placed).toHaveBeenCalledTimes(1)
        expect(menu().hasAttribute('data-placed')).toBe(true)
    })

    it('stops following and hides the menu again when it closes', async () => {
        setOpen(menu(), true)
        await settle()

        setOpen(menu(), false)

        expect(stopTracking).toHaveBeenCalledTimes(1)
        expect(trigger().getAttribute('aria-expanded')).toBe('false')
        expect(menu().hasAttribute('data-placed')).toBe(false)
    })

    it('closes open menus before an htmx swap', async () => {
        const matches = vi
            .spyOn(menu(), 'matches')
            .mockImplementation((selector) => selector === ':popover-open')
        setOpen(menu(), true)
        await settle()

        document.dispatchEvent(new Event('htmx:beforeSwap'))

        expect(hidePopover).toHaveBeenCalledTimes(1)
        expect(stopTracking).toHaveBeenCalledTimes(1)
        matches.mockRestore()
    })

    it('leaves alone a popover that has nothing to anchor to', () => {
        document.body.innerHTML = '<div id="other" popover></div>'
        const other = document.getElementById('other')!

        setOpen(other, true)

        expect(other.hasAttribute('data-floating')).toBe(false)
        expect(autoUpdate).not.toHaveBeenCalled()
    })
})
