import { initFlyouts, initToasts } from './overlays'

describe('toast', () => {
    beforeEach(() => {
        document.body.innerHTML =
            '<div data-toast><p>Saved</p><button data-toast-dismiss hidden></button></div>'
        initToasts()
    })

    it('reveals the dismiss button and removes the toast when it is used', () => {
        const button = document.querySelector<HTMLButtonElement>(
            '[data-toast-dismiss]'
        )!
        expect(button.hidden).toBe(false)

        button.click()

        expect(document.querySelector('[data-toast]')).toBeNull()
    })
})

describe('flyout', () => {
    beforeEach(() => {
        document.body.innerHTML =
            '<button data-flyout-open="f" hidden>Open</button><dialog id="f"><p>x</p></dialog>'
        const dialog = document.getElementById('f') as HTMLDialogElement
        dialog.showModal = jest.fn()
        dialog.close = jest.fn()
        initFlyouts()
    })

    it('reveals the trigger and opens the dialog', () => {
        const trigger =
            document.querySelector<HTMLButtonElement>('[data-flyout-open]')!
        expect(trigger.hidden).toBe(false)

        trigger.click()

        expect(
            (document.getElementById('f') as HTMLDialogElement).showModal
        ).toHaveBeenCalled()
    })

    it('closes on a click on the backdrop only', () => {
        const dialog = document.getElementById('f') as HTMLDialogElement
        dialog
            .querySelector('p')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(dialog.close).not.toHaveBeenCalled()

        dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(dialog.close).toHaveBeenCalled()
    })

    it('leaves a trigger hidden when its dialog is missing', () => {
        document.body.innerHTML =
            '<button data-flyout-open="nope" hidden></button>'
        initFlyouts()

        expect(
            document.querySelector<HTMLButtonElement>('button')!.hidden
        ).toBe(true)
    })
})
