import { lockPageScroll } from './scroll-lock'

function open(dialog: HTMLDialogElement): void {
    dialog.showModal()
    // The dialog handles Escape and returns focus to the image; the page behind it would still scroll.
    const unlockScroll = lockPageScroll()
    const closing = new AbortController()
    // A click on the backdrop lands on the dialog itself, not on its content.
    dialog.addEventListener(
        'click',
        (event) => {
            if (event.target === dialog) dialog.close()
        },
        { signal: closing.signal }
    )
    dialog.addEventListener(
        'close',
        () => {
            unlockScroll()
            closing.abort()
        },
        { signal: closing.signal }
    )
}

// Opens the dialog named by [data-modal-id] on an image link. One document listener serves every page htmx swaps in.
export function bindImageModals(): void {
    document.addEventListener('click', (event) => {
        const link =
            event.target instanceof Element
                ? event.target.closest<HTMLElement>('[data-modal-id]')
                : null
        if (!link) return
        const dialog = document.getElementById(link.dataset.modalId ?? '')
        if (!(dialog instanceof HTMLDialogElement)) return
        event.preventDefault()
        open(dialog)
    })
}
