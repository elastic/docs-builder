// Small behaviours for the overlay components. Each piece works as plain HTML first; this only adds what a page cannot do alone.
// Hooks are data attributes, never styling classes.

// A toast has a dismiss button that is hidden until script can remove the toast.
export function initToasts(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>('[data-toast]:not([data-toast-bound])')
        .forEach((toast) => {
            toast.dataset.toastBound = ''
            const dismiss = toast.querySelector<HTMLButtonElement>(
                '[data-toast-dismiss]'
            )
            if (!dismiss) return
            dismiss.hidden = false
            dismiss.addEventListener('click', () => toast.remove())
        })
}

// A flyout is a modal dialog; its trigger is hidden until script can open it, and a click on the backdrop closes it.
export function initFlyouts(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-flyout-open]:not([data-flyout-bound])'
        )
        .forEach((trigger) => {
            const dialog = document.getElementById(
                trigger.dataset.flyoutOpen ?? ''
            )
            if (!(dialog instanceof HTMLDialogElement)) return
            if (typeof dialog.showModal !== 'function') return
            trigger.dataset.flyoutBound = ''
            trigger.hidden = false
            trigger.addEventListener('click', () => dialog.showModal())
            dialog.addEventListener('click', (event) => {
                if (event.target === dialog) dialog.close()
            })
        })
}
