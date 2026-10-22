import { initLightboxes } from './lightbox'

describe('lightbox', () => {
    beforeEach(() => {
        document.body.innerHTML = `
<a href="/a.png" data-lightbox="box"><img src="/a.png"></a>
<dialog class="lightbox" id="box"><img src="/a.png"></dialog>`
        const dialog = document.getElementById('box') as HTMLDialogElement
        dialog.showModal = jest.fn()
        dialog.close = jest.fn()
        initLightboxes()
    })

    it('opens the dialog instead of following the link', () => {
        const event = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
        })
        document.querySelector('a')!.dispatchEvent(event)

        expect(event.defaultPrevented).toBe(true)
        expect(
            (document.getElementById('box') as HTMLDialogElement).showModal
        ).toHaveBeenCalled()
    })

    it('closes on a click on the backdrop but not on the image', () => {
        const dialog = document.getElementById('box') as HTMLDialogElement
        dialog
            .querySelector('img')!
            .dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(dialog.close).not.toHaveBeenCalled()

        dialog.dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(dialog.close).toHaveBeenCalled()
    })

    it('leaves the link alone when the dialog is missing', () => {
        document.body.innerHTML = '<a href="/a.png" data-lightbox="nope"></a>'
        initLightboxes()
        const event = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
        })
        document.querySelector('a')!.dispatchEvent(event)

        expect(event.defaultPrevented).toBe(false)
    })
})
