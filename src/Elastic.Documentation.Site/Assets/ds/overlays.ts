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

const flyoutWidthKey = 'docs-flyout-width'
const flyoutMinWidth = 320
const flyoutKeyStep = 24

function clampFlyoutWidth(width: number): number {
    return Math.min(Math.max(width, flyoutMinWidth), window.innerWidth)
}

// A resizable flyout grows from its left edge. The handle is hidden until script can move it; the arrow keys resize it too.
export function initFlyoutResize(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-flyout-resizable]:not([data-flyout-resize-bound])'
        )
        .forEach((dialog) => {
            const handle = dialog.querySelector<HTMLElement>(
                '[data-flyout-handle]'
            )
            if (!handle) return
            dialog.dataset.flyoutResizeBound = ''
            handle.hidden = false

            const apply = (width: number) => {
                const next = clampFlyoutWidth(width)
                dialog.style.setProperty('--flyout-width', `${next}px`)
                localStorage.setItem(flyoutWidthKey, String(next))
            }
            const saved = Number(localStorage.getItem(flyoutWidthKey))
            if (saved > 0) apply(saved)

            handle.addEventListener('pointerdown', (event) => {
                event.preventDefault()
                handle.setPointerCapture?.(event.pointerId)
                const move = (e: PointerEvent) =>
                    apply(window.innerWidth - e.clientX)
                const stop = () => {
                    handle.removeEventListener('pointermove', move)
                    handle.removeEventListener('pointerup', stop)
                }
                handle.addEventListener('pointermove', move)
                handle.addEventListener('pointerup', stop)
            })
            handle.addEventListener('keydown', (event) => {
                const grow =
                    event.key === 'ArrowLeft'
                        ? flyoutKeyStep
                        : event.key === 'ArrowRight'
                          ? -flyoutKeyStep
                          : 0
                if (grow === 0) return
                event.preventDefault()
                apply(dialog.getBoundingClientRect().width + grow)
            })
        })
}
