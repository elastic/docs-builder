// Opens an image in its native dialog. The link works on its own, so without script it opens the image itself.
// Hooks: [data-lightbox] on the link holds the id of the dialog.
export function initLightboxes(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-lightbox]:not([data-lightbox-bound])'
        )
        .forEach((link) => {
            link.dataset.lightboxBound = ''
            link.addEventListener('click', (event) => {
                const dialog = document.getElementById(
                    link.dataset.lightbox ?? ''
                )
                if (!(dialog instanceof HTMLDialogElement)) return
                if (typeof dialog.showModal !== 'function') return
                event.preventDefault()
                dialog.showModal()
            })
        })

    scope
        .querySelectorAll<HTMLDialogElement>(
            'dialog.lightbox:not([data-lightbox-bound])'
        )
        .forEach((dialog) => {
            dialog.dataset.lightboxBound = ''
            // A click on the backdrop lands on the dialog itself, not on its content.
            dialog.addEventListener('click', (event) => {
                if (event.target === dialog) dialog.close()
            })
        })
}
