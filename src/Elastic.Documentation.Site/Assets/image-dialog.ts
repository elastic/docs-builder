const triggers = new WeakMap<HTMLDialogElement, HTMLButtonElement>()
let delegatedListenersInitialized = false

function closeDialog(dialog: HTMLDialogElement) {
    if (dialog.open) dialog.close()
}

function initializeDelegatedListeners() {
    if (delegatedListenersInitialized) return
    delegatedListenersInitialized = true

    document.addEventListener('click', (event) => {
        if (!(event.target instanceof Element)) return

        const openButton = event.target.closest('[data-image-dialog-open]')
        if (openButton instanceof HTMLButtonElement) {
            const dialogId = openButton.getAttribute('aria-controls')
            if (!dialogId) return

            const dialog = document.getElementById(dialogId)
            if (!(dialog instanceof HTMLDialogElement)) return

            triggers.set(dialog, openButton)
            dialog.showModal()
            const closeButton = dialog.querySelector(
                '[data-image-dialog-close]'
            )
            if (closeButton instanceof HTMLButtonElement) closeButton.focus()
            return
        }

        const closeButton = event.target.closest('[data-image-dialog-close]')
        if (closeButton instanceof HTMLButtonElement) {
            const dialog = closeButton.closest('dialog')
            if (dialog instanceof HTMLDialogElement) closeDialog(dialog)
            return
        }

        if (
            event.target instanceof HTMLDialogElement &&
            event.target.matches('[data-image-dialog]')
        ) {
            closeDialog(event.target)
        }
    })
}

export function initImageDialogs() {
    initializeDelegatedListeners()

    document
        .querySelectorAll<HTMLDialogElement>('dialog[data-image-dialog]')
        .forEach((dialog) => {
            if (dialog.dataset.initialized === 'true') return
            dialog.dataset.initialized = 'true'
            dialog.addEventListener('close', () => {
                triggers.get(dialog)?.focus()
                triggers.delete(dialog)
            })
        })
}
