import { flashTooltip, initTooltips } from './tooltip'
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

// jsdom has no top layer: what the popover and modal dialog APIs put there, in order, last on top.
const topLayer: Element[] = []
const leave = (el: Element) => {
    const at = topLayer.indexOf(el)
    if (at >= 0) topLayer.splice(at, 1)
}

const tooltip = () => document.querySelector<HTMLElement>('[role="tooltip"]')
/** The text of the tooltip on screen, or null when none is. */
const shown = () => {
    const el = tooltip()
    return el && topLayer.includes(el) ? el.textContent : null
}

function pointer(type: string, target: Element, init: PointerEventInit = {}) {
    target.dispatchEvent(
        new PointerEvent(type, {
            bubbles: true,
            pointerType: 'mouse',
            ...init,
        })
    )
}

const byId = (id: string) => document.getElementById(id)!

describe('tooltips', () => {
    beforeAll(() => {
        Object.assign(HTMLElement.prototype, {
            showPopover(this: HTMLElement) {
                topLayer.push(this)
            },
            hidePopover(this: HTMLElement) {
                leave(this)
            },
        })
        HTMLDialogElement.prototype.showModal = function (
            this: HTMLDialogElement
        ) {
            this.setAttribute('open', '')
            topLayer.push(this)
        }
        // jsdom answers :focus-visible unreliably; here every focus is a keyboard one.
        const matches = Element.prototype.matches
        Element.prototype.matches = function (this: Element, selector: string) {
            return selector === ':focus-visible'
                ? this === document.activeElement
                : matches.call(this, selector)
        }
        initTooltips()
    })

    beforeEach(() => {
        vi.useFakeTimers()
        ;(autoUpdate as Mock).mockImplementation(
            (_anchor, _floating, update: () => void) => {
                update()
                return vi.fn()
            }
        )
        ;(computePosition as Mock).mockResolvedValue({
            x: 12,
            y: 34,
            placement: 'top',
        })
        document.body.innerHTML = `
            <span id="path" data-tooltip="hits.total" aria-describedby="hint">x</span>
            <span id="slow" data-tooltip="took" data-tooltip-delay="slow">y</span>
            <span id="mono" data-tooltip="a.b.c" data-tooltip-theme="mono">z</span>
            <span id="empty" data-tooltip="">e</span>
            <button id="copy" class="copybtn" data-tooltip="Copy">c</button>
            <button id="plain" data-tooltip="Back to top">b</button>
            <button id="icon-button" data-tooltip="Copy"><span id="icon">i</span></button>
            <p id="elsewhere">text</p>`
    })

    afterEach(() => {
        document.dispatchEvent(new Event('htmx:beforeSwap'))
        topLayer.length = 0
        vi.useRealTimers()
    })

    it('shows the text above its target once the pointer rests on it', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(79)
        expect(shown()).toBeNull()

        await vi.advanceTimersByTimeAsync(1)

        expect(shown()).toBe('hits.total')
        expect(computePosition).toHaveBeenCalledWith(
            byId('path'),
            tooltip(),
            expect.objectContaining({ placement: 'top' })
        )
        expect(tooltip()!.style.left).toBe('12px')
        expect(byId('path').getAttribute('aria-describedby')).toBe(
            `hint ${tooltip()!.id}`
        )
    })

    it('waits longer on a slow target, for lists the mouse crosses', async () => {
        pointer('pointerover', byId('slow'))
        await vi.advanceTimersByTimeAsync(499)
        expect(shown()).toBeNull()

        await vi.advanceTimersByTimeAsync(1)
        expect(shown()).toBe('took')
    })

    it('shows nothing for an empty text', async () => {
        pointer('pointerover', byId('empty'))
        await vi.advanceTimersByTimeAsync(500)

        expect(shown()).toBeNull()
    })

    it('sets a mono text in the mono theme', async () => {
        pointer('pointerover', byId('mono'))
        await vi.advanceTimersByTimeAsync(80)

        expect(tooltip()!.dataset.theme).toBe('mono')
    })

    it('hides when the pointer leaves and gives back the description it borrowed', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(80)

        pointer('pointerout', byId('path'), {
            relatedTarget: byId('elsewhere'),
        })
        // A mouse has a moment to reach the tooltip before it goes.
        await vi.advanceTimersByTimeAsync(149)
        expect(shown()).toBe('hits.total')
        await vi.advanceTimersByTimeAsync(1)

        expect(shown()).toBeNull()
        expect(byId('path').getAttribute('aria-describedby')).toBe('hint')
    })

    it('stays open while the pointer crosses to the tooltip and rests on it, and closes when it leaves', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(80)

        pointer('pointerout', byId('path'), { relatedTarget: tooltip() })
        await vi.advanceTimersByTimeAsync(100)
        pointer('pointerover', tooltip()!, { relatedTarget: byId('path') })
        await vi.advanceTimersByTimeAsync(1000)
        expect(shown()).toBe('hits.total')

        // A click or a text selection in the tooltip is reading it.
        pointer('pointerdown', tooltip()!)
        expect(shown()).toBe('hits.total')

        // Back on the target it stays too; leaving the tooltip for elsewhere closes it.
        pointer('pointerout', tooltip()!, { relatedTarget: byId('path') })
        pointer('pointerover', byId('path'), { relatedTarget: tooltip() })
        await vi.advanceTimersByTimeAsync(1000)
        expect(shown()).toBe('hits.total')

        pointer('pointerout', byId('path'), { relatedTarget: tooltip() })
        pointer('pointerover', tooltip()!, { relatedTarget: byId('path') })
        pointer('pointerout', tooltip()!, {
            relatedTarget: byId('elsewhere'),
        })
        await vi.advanceTimersByTimeAsync(150)
        expect(shown()).toBeNull()
    })

    it('hides at once when a touch lifts, since a touch cannot reach the tooltip', async () => {
        const touch = { pointerType: 'touch' }
        pointer('pointerover', byId('path'), touch)
        await vi.advanceTimersByTimeAsync(300)
        expect(shown()).toBe('hits.total')

        pointer('pointerout', byId('path'), touch)

        expect(shown()).toBeNull()
    })

    it('never shows when the pointer leaves before the delay', async () => {
        pointer('pointerover', byId('path'))
        pointer('pointerout', byId('path'))
        await vi.advanceTimersByTimeAsync(500)

        expect(shown()).toBeNull()
    })

    it('hides on Escape', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(80)

        document.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
        )

        expect(shown()).toBeNull()
    })

    it('stays away after Escape until the pointer leaves, even when the target content changes under it', async () => {
        pointer('pointerover', byId('icon'))
        await vi.advanceTimersByTimeAsync(80)
        document.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
        )

        // The icon is swapped while the pointer rests on the button.
        byId('icon').replaceWith(
            Object.assign(document.createElement('span'), { id: 'icon' })
        )
        pointer('pointerover', byId('icon'))
        await vi.advanceTimersByTimeAsync(80)
        expect(shown()).toBeNull()

        pointer('pointerout', byId('icon'), {
            relatedTarget: byId('elsewhere'),
        })
        pointer('pointerover', byId('icon'), {
            relatedTarget: byId('elsewhere'),
        })
        await vi.advanceTimersByTimeAsync(80)
        expect(shown()).toBe('Copy')
    })

    it('shows when keyboard focus lands on a focusable element inside the target', async () => {
        document.body.insertAdjacentHTML(
            'beforeend',
            `<span id="optional" data-tooltip="Optional. Leave it out."><a id="param" href="#x">id</a></span>`
        )

        byId('param').focus()
        await vi.advanceTimersByTimeAsync(0)

        expect(shown()).toBe('Optional. Leave it out.')

        byId('param').blur()
        expect(shown()).toBeNull()
    })

    it('shows on the first hover after Escape closed a tooltip that keyboard focus opened', async () => {
        byId('plain').focus()
        await vi.advanceTimersByTimeAsync(0)
        document.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
        )
        expect(shown()).toBeNull()

        pointer('pointerover', byId('plain'))
        await vi.advanceTimersByTimeAsync(80)

        expect(shown()).toBe('Back to top')
    })

    it('stays away after Escape while the pointer moves between a target and a target nested in it', async () => {
        document.body.insertAdjacentHTML(
            'beforeend',
            `<div id="chip" tabindex="0" data-tooltip="Long title"><span id="count" data-tooltip="3 languages">3</span></div>`
        )
        pointer('pointerover', byId('count'))
        await vi.advanceTimersByTimeAsync(80)
        byId('chip').focus()
        await vi.advanceTimersByTimeAsync(0)
        expect(shown()).toBe('Long title')
        document.dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
        )

        pointer('pointerout', byId('count'), { relatedTarget: byId('chip') })
        pointer('pointerover', byId('chip'), { relatedTarget: byId('count') })
        await vi.advanceTimersByTimeAsync(80)

        expect(shown()).toBeNull()
    })

    it('hides before an htmx swap, so it keeps nothing of the old page', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(80)

        document.dispatchEvent(new Event('htmx:beforeSwap'))

        expect(shown()).toBeNull()
    })

    it('hides when its target leaves the page', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(80)
        const update = (autoUpdate as Mock).mock.lastCall![2] as () => void

        byId('path').remove()
        update()

        expect(shown()).toBeNull()
    })

    it('hides when its target loses its text', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(80)
        const update = (autoUpdate as Mock).mock.lastCall![2] as () => void

        byId('path').removeAttribute('data-tooltip')
        update()

        expect(shown()).toBeNull()
    })

    it('shows on a touch held for 300ms, not on a tap', async () => {
        const touch = { pointerType: 'touch' }
        pointer('pointerover', byId('path'), touch)
        await vi.advanceTimersByTimeAsync(100)
        pointer('pointerout', byId('path'), touch)
        await vi.advanceTimersByTimeAsync(300)
        expect(shown()).toBeNull()

        pointer('pointerover', byId('path'), touch)
        await vi.advanceTimersByTimeAsync(299)
        expect(shown()).toBeNull()
        await vi.advanceTimersByTimeAsync(1)
        expect(shown()).toBe('hits.total')
    })

    it('shows at once on keyboard focus and hides when focus moves on', async () => {
        byId('plain').focus()
        await vi.advanceTimersByTimeAsync(0)
        expect(shown()).toBe('Back to top')

        byId('copy').focus()
        await vi.advanceTimersByTimeAsync(0)
        expect(shown()).toBe('Copy')

        byId('copy').blur()
        expect(shown()).toBeNull()
    })

    it('shows at once when keyboard focus reaches a target the pointer only queued', async () => {
        pointer('pointerover', byId('slow'))
        await vi.advanceTimersByTimeAsync(100)
        expect(shown()).toBeNull()

        byId('slow').tabIndex = 0
        byId('slow').focus()
        await vi.advanceTimersByTimeAsync(0)

        expect(shown()).toBe('took')
        // The queued timer is gone, so it neither re-shows nor hides it later.
        await vi.advanceTimersByTimeAsync(500)
        expect(shown()).toBe('took')
    })

    it('hides on a click, except on a copy button whose text changes in place', async () => {
        pointer('pointerover', byId('plain'))
        await vi.advanceTimersByTimeAsync(80)
        pointer('pointerdown', byId('plain'))
        expect(shown()).toBeNull()

        pointer('pointerover', byId('copy'))
        await vi.advanceTimersByTimeAsync(80)
        pointer('pointerdown', byId('copy'))
        expect(shown()).toBe('Copy')
    })

    it('shows above a modal dialog opened after it was first shown', async () => {
        pointer('pointerover', byId('path'))
        await vi.advanceTimersByTimeAsync(80)
        pointer('pointerout', byId('path'))
        const dialog = document.createElement('dialog')
        dialog.innerHTML = '<button id="close" data-tooltip="Close">x</button>'
        document.body.append(dialog)
        dialog.showModal()

        pointer('pointerover', byId('close'))
        await vi.advanceTimersByTimeAsync(80)

        expect(shown()).toBe('Close')
        expect(topLayer[topLayer.length - 1]).toBe(tooltip())
    })

    it('swaps the text of the tooltip on screen for a moment', async () => {
        pointer('pointerover', byId('copy'))
        await vi.advanceTimersByTimeAsync(80)

        flashTooltip(byId('copy'), 'Copied!')
        expect(shown()).toBe('Copied!')

        await vi.advanceTimersByTimeAsync(1500)
        expect(shown()).toBe('Copy')
    })
})

describe('flashTooltip', () => {
    beforeEach(() => vi.useFakeTimers())
    afterEach(() => vi.useRealTimers())

    it('swaps the text and restores it after the delay', () => {
        const el = document.createElement('button')
        el.dataset.tooltip = 'Copy'
        flashTooltip(el, 'Copied!')
        expect(el.dataset.tooltip).toBe('Copied!')
        vi.advanceTimersByTime(1500)
        expect(el.dataset.tooltip).toBe('Copy')
    })

    it('keeps the original text across repeated flashes', () => {
        const el = document.createElement('button')
        el.dataset.tooltip = 'Copy'
        flashTooltip(el, 'Copied!')
        flashTooltip(el, 'Copied!')
        vi.advanceTimersByTime(1500)
        expect(el.dataset.tooltip).toBe('Copy')
    })

    it('restarts the delay on a repeated flash', () => {
        const el = document.createElement('button')
        el.dataset.tooltip = 'Copy'
        flashTooltip(el, 'Copied!')
        vi.advanceTimersByTime(1000)
        flashTooltip(el, 'Copied!')
        vi.advanceTimersByTime(1000)
        expect(el.dataset.tooltip).toBe('Copied!')
        vi.advanceTimersByTime(500)
        expect(el.dataset.tooltip).toBe('Copy')
    })
})
