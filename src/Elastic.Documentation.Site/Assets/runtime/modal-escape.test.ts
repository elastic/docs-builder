import { bindModalEscape } from './modal-escape'
import {
    afterEach,
    beforeAll,
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from 'vitest'

// The elastic.co header prevents every Escape, which cancels the browser's own closing of a modal dialog.
const preventEscape = (event: KeyboardEvent) => {
    if (event.key === 'Escape') event.preventDefault()
}

function openDialog(id: string): HTMLDialogElement {
    const dialog = document.createElement('dialog')
    dialog.id = id
    dialog.innerHTML = `<button id="${id}-button">Yes</button>`
    document.body.append(dialog)
    dialog.showModal()
    return dialog
}

function press(target: Element, init: KeyboardEventInit = {}): KeyboardEvent {
    const event = new KeyboardEvent('keydown', {
        key: 'Escape',
        bubbles: true,
        cancelable: true,
        ...init,
    })
    target.dispatchEvent(event)
    return event
}

describe('modal dialogs and Escape', () => {
    beforeAll(() => {
        bindModalEscape()
        // jsdom has no modal dialogs.
        HTMLDialogElement.prototype.showModal = function (
            this: HTMLDialogElement
        ) {
            this.setAttribute('open', '')
            this.dataset.modal = ''
        }
        // jsdom does not know the :modal pseudo-class.
        const matches = Element.prototype.matches
        HTMLDialogElement.prototype.matches = function (
            this: HTMLDialogElement,
            selector: string
        ) {
            return selector === ':modal'
                ? this.hasAttribute('open') && 'modal' in this.dataset
                : matches.call(this, selector)
        }
        HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
            this.removeAttribute('open')
            this.dispatchEvent(new Event('close'))
        }
    })

    beforeEach(() => {
        vi.useFakeTimers()
    })

    afterEach(() => {
        document.removeEventListener('keydown', preventEscape)
        document.body.innerHTML = ''
        vi.useRealTimers()
    })

    it('closes the dialog when the page prevents the key', () => {
        const dialog = openDialog('a')
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('a-button')!)
        vi.runAllTimers()

        expect(dialog.open).toBe(false)
    })

    it('closes the dialog when focus is outside it', () => {
        const dialog = openDialog('a')
        document.addEventListener('keydown', preventEscape)

        press(document.body)
        vi.runAllTimers()

        expect(dialog.open).toBe(false)
    })

    it('does nothing for a key the browser acts on, as the browser closes the dialog itself', () => {
        const dialog = openDialog('a')
        const cancel = vi.fn()
        dialog.addEventListener('cancel', cancel)

        press(document.getElementById('a-button')!)
        vi.runAllTimers()

        expect(cancel).not.toHaveBeenCalled()
        expect(dialog.open).toBe(true)
    })

    it('does nothing when the browser has closed the dialog by the time the key is over', () => {
        const dialog = openDialog('a')
        const cancel = vi.fn()
        dialog.addEventListener('cancel', cancel)
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('a-button')!)
        dialog.close()
        vi.runAllTimers()

        expect(cancel).not.toHaveBeenCalled()
    })

    it('leaves the key to every other handler, unchanged', () => {
        openDialog('a')
        const seen = vi.fn()
        document.addEventListener('keydown', seen)
        document.addEventListener('keydown', preventEscape)

        const event = press(document.getElementById('a-button')!)

        expect(seen).toHaveBeenCalledTimes(1)
        expect(event.defaultPrevented).toBe(true)
        document.removeEventListener('keydown', seen)
    })

    it('lets a dialog stay open by cancelling its cancel event', () => {
        const dialog = openDialog('a')
        dialog.addEventListener('cancel', (event) => event.preventDefault())
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('a-button')!)
        vi.runAllTimers()

        expect(dialog.open).toBe(true)
    })

    it('asks the browser to close the dialog where it can', () => {
        const dialog = openDialog('a') as HTMLDialogElement & {
            requestClose?: () => void
        }
        dialog.requestClose = vi.fn(() => dialog.close())
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('a-button')!)
        vi.runAllTimers()

        expect(dialog.requestClose).toHaveBeenCalledTimes(1)
        expect(dialog.open).toBe(false)
    })

    it('closes the dialog that holds the key, not another one', () => {
        const first = openDialog('first')
        const second = openDialog('second')
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('first-button')!)
        vi.runAllTimers()

        expect(first.open).toBe(false)
        expect(second.open).toBe(true)
    })

    it('closes the last dialog opened when focus is outside both', () => {
        const first = openDialog('first')
        const second = openDialog('second')
        document.addEventListener('keydown', preventEscape)

        press(document.body)
        vi.runAllTimers()

        expect(first.open).toBe(true)
        expect(second.open).toBe(false)
    })

    it('closes the dialog opened last, not the last one in the document', () => {
        const first = document.createElement('dialog')
        const second = document.createElement('dialog')
        first.id = 'first'
        second.id = 'second'
        document.body.append(first, second)
        document.addEventListener('keydown', preventEscape)

        // The one earlier in the document is the one on top.
        second.showModal()
        first.showModal()
        press(document.body)
        vi.runAllTimers()

        expect(first.open).toBe(false)
        expect(second.open).toBe(true)
    })

    it('goes by the latest opening of a dialog that was closed and opened again', async () => {
        const first = openDialog('first')
        const second = openDialog('second')
        document.addEventListener('keydown', preventEscape)

        first.close()
        // Let the observer see the closing before the dialog opens again.
        await Promise.resolve()
        first.showModal()
        press(document.body)
        vi.runAllTimers()

        expect(first.open).toBe(false)
        expect(second.open).toBe(true)
    })

    it('goes by the reopening of a dialog that closed and opened again in one task', () => {
        const first = openDialog('first')
        const second = openDialog('second')
        document.addEventListener('keydown', preventEscape)

        // No wait between the two: the observer reports both changes together.
        first.close()
        first.showModal()
        press(document.body)
        vi.runAllTimers()

        expect(first.open).toBe(false)
        expect(second.open).toBe(true)
    })

    it('leaves a dialog that is not modal alone', () => {
        const dialog = openDialog('a')
        // Opened with show(), not showModal().
        delete dialog.dataset.modal
        document.addEventListener('keydown', preventEscape)

        press(document.body)
        vi.runAllTimers()

        expect(dialog.open).toBe(true)
    })

    it('does nothing when no dialog is open', () => {
        document.addEventListener('keydown', preventEscape)

        expect(() => {
            press(document.body)
            vi.runAllTimers()
        }).not.toThrow()
    })

    it.each([
        ['another key', { key: 'a' }],
        ['an Escape that cancels an IME composition', { isComposing: true }],
        ['a held key repeating', { repeat: true }],
    ])('ignores %s', (_, init) => {
        const dialog = openDialog('a')
        document.addEventListener('keydown', preventEscape)

        const event = new KeyboardEvent('keydown', {
            key: 'Escape',
            bubbles: true,
            cancelable: true,
            ...init,
        })
        document.body.dispatchEvent(event)
        vi.runAllTimers()

        expect(dialog.open).toBe(true)
    })
})

describe('popover menus and Escape', () => {
    const POPOVERS = '[popover]:not([popover="manual"]):popover-open'
    const query = document.querySelectorAll.bind(document)

    /** jsdom has no popovers: `open` is what the browser's `:popover-open` would say. */
    function popover(id: string, mode = 'auto', open = true): HTMLElement {
        const menu = document.createElement('div')
        menu.id = id
        menu.setAttribute('popover', mode)
        menu.innerHTML = `<button id="${id}-item">Item</button>`
        menu.matches = ((selector: string) =>
            selector === ':popover-open'
                ? open
                : Element.prototype.matches.call(menu, selector)) as never
        menu.hidePopover = vi.fn(() => {
            open = false
        })
        document.body.append(menu)
        return menu
    }

    beforeAll(() => {
        bindModalEscape()
        document.querySelectorAll = ((selector: string) =>
            selector === POPOVERS
                ? Array.from(query('[popover]')).filter(
                      (el) =>
                          el.matches(':popover-open') &&
                          el.getAttribute('popover') !== 'manual'
                  )
                : query(selector)) as never
    })

    beforeEach(() => {
        vi.useFakeTimers()
    })

    afterEach(() => {
        document.removeEventListener('keydown', preventEscape)
        document.body.innerHTML = ''
        vi.useRealTimers()
    })

    it('hides the open menu when the page prevents the key', () => {
        const menu = popover('menu')
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('menu-item')!)
        vi.runAllTimers()

        expect(menu.hidePopover).toHaveBeenCalledTimes(1)
    })

    it('hides the open menu when focus is outside it', () => {
        const menu = popover('menu')
        document.addEventListener('keydown', preventEscape)

        press(document.body)
        vi.runAllTimers()

        expect(menu.hidePopover).toHaveBeenCalledTimes(1)
    })

    it('does nothing for a key the browser acts on, as the browser hides the menu itself', () => {
        const menu = popover('menu')

        press(document.getElementById('menu-item')!)
        vi.runAllTimers()

        expect(menu.hidePopover).not.toHaveBeenCalled()
    })

    it('hides the menu that holds the key, not another one', () => {
        const first = popover('first')
        const second = popover('second')
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('first-item')!)
        vi.runAllTimers()

        expect(first.hidePopover).toHaveBeenCalledTimes(1)
        expect(second.hidePopover).not.toHaveBeenCalled()
    })

    it('hides only the innermost of nested menus that holds the key', () => {
        const outer = popover('outer')
        const inner = popover('inner')
        outer.append(inner)
        document.addEventListener('keydown', preventEscape)

        press(document.getElementById('inner-item')!)
        vi.runAllTimers()

        expect(inner.hidePopover).toHaveBeenCalledTimes(1)
        expect(outer.hidePopover).not.toHaveBeenCalled()
    })

    it('leaves a manual popover to its owner', () => {
        const menu = popover('menu', 'manual')
        document.addEventListener('keydown', preventEscape)

        press(document.body)
        vi.runAllTimers()

        expect(menu.hidePopover).not.toHaveBeenCalled()
    })
})
