import { bindImageModals } from './image-modal'

// The markup ImageView.cshtml renders for one image.
const image = `
<a class="reference internal image-reference" href="javascript:void(0)" data-modal-id="modal-a">
    <img src="/a.png" alt="A">
</a>
<dialog id="modal-a" class="modal" aria-label="A">
    <div class="modal-content">
        <form method="dialog" class="modal-close">
            <button type="submit" class="close-button" aria-label="Close">&times;</button>
        </form>
        <a class="reference internal image-reference" href="/a.png" target="_blank"><img src="/a.png" alt="A"></a>
    </div>
</dialog>`

const dialog = () => document.getElementById('modal-a') as HTMLDialogElement
const scroll = () => document.documentElement.style.overflow

function click(target: Element): MouseEvent {
    const event = new MouseEvent('click', { bubbles: true, cancelable: true })
    target.dispatchEvent(event)
    return event
}

describe('legacy image modal', () => {
    beforeAll(() => {
        bindImageModals()
        // jsdom has no modal dialogs; these do what the browser does to the attribute and events.
        HTMLDialogElement.prototype.showModal = function (
            this: HTMLDialogElement
        ) {
            this.setAttribute('open', '')
        }
        HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
            if (!this.open) return
            this.removeAttribute('open')
            this.dispatchEvent(new Event('close'))
        }
    })

    beforeEach(() => {
        document.body.innerHTML = image
    })

    afterEach(() => {
        // A test that leaves the dialog open leaves the page scroll locked.
        document.documentElement.removeAttribute('style')
    })

    it('opens the image in a modal dialog that locks the page scroll', () => {
        const showModal = vi.spyOn(HTMLDialogElement.prototype, 'showModal')

        const event = click(document.querySelector('img')!)

        expect(event.defaultPrevented).toBe(true)
        expect(showModal).toHaveBeenCalledTimes(1)
        expect(dialog().open).toBe(true)
        expect(scroll()).toBe('hidden')
    })

    it('closes on a click on the backdrop, and stays open for a click inside', () => {
        click(document.querySelector('[data-modal-id]')!)

        click(dialog().querySelector('img')!)
        expect(dialog().open).toBe(true)

        click(dialog())
        expect(dialog().open).toBe(false)
        expect(scroll()).toBe('')
    })

    it('unlocks the page scroll when the browser closes it, as it does on Escape', () => {
        click(document.querySelector('[data-modal-id]')!)

        dialog().close()

        expect(scroll()).toBe('')
    })

    it('closes an open dialog and unlocks the page scroll before htmx swaps the page', () => {
        click(document.querySelector('[data-modal-id]')!)
        expect(scroll()).toBe('hidden')

        document.dispatchEvent(new Event('htmx:beforeSwap'))

        expect(dialog().open).toBe(false)
        expect(scroll()).toBe('')
    })

    it('opens and closes again after a close', () => {
        const opener = document.querySelector('[data-modal-id]')!
        click(opener)
        dialog().close()

        click(opener)
        expect(dialog().open).toBe(true)
        expect(scroll()).toBe('hidden')
        click(dialog())
        expect(dialog().open).toBe(false)
        expect(scroll()).toBe('')
    })

    it('opens the dialog of a carousel slide', () => {
        document.body.innerHTML = `
<div class="carousel-container"><div class="carousel-track"><div class="carousel-slide">
    <a class="carousel-image-reference" href="javascript:void(0)" data-modal-id="modal-a"><img src="/a.png"></a>
</div></div></div>${image}`

        click(document.querySelector('.carousel-image-reference img')!)

        expect(dialog().open).toBe(true)
    })
})
