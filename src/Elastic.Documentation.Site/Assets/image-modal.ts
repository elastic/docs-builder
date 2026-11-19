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

// Closes the open image or diagram dialogs inside scope; their close handlers release the scroll lock.
export function closeOpenZoomDialogs(scope: ParentNode = document): void {
    scope
        .querySelectorAll<HTMLDialogElement>(
            'dialog.modal[open], dialog.mermaid-modal[open]'
        )
        .forEach((dialog) => dialog.close())
}

type BeforeSwapDetail = { shouldSwap?: boolean; target?: Element }

// A swap removes the dialogs inside its target without firing their close event, which would leave the page scroll
// locked. A response htmx will not swap, such as an error, leaves the page and its dialogs alone.
function closeDialogsTheSwapRemoves(event: Event): void {
    const { shouldSwap, target } =
        (event as CustomEvent<BeforeSwapDetail>).detail ?? {}
    if (shouldSwap && target) closeOpenZoomDialogs(target)
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
    document.addEventListener('htmx:beforeSwap', closeDialogsTheSwapRemoves)
}
