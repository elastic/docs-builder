/**
 * A modal `<dialog>` closes on Escape because the browser runs a default action after the keydown, and a handler that
 * calls `preventDefault()` cancels it. The elastic.co header does that for every Escape on assembled sites, so no modal
 * dialog on the site closed. This restores the default action where it was cancelled: when the key event is over, the
 * key was prevented, and a modal dialog is still open, the dialog gets the request the browser would have made. That is
 * a `cancel` event, then `close()`, so a dialog keeps the platform's way to stay open: cancel its `cancel` event.
 *
 * Nothing is stopped or changed while the event runs. Handlers see the key as before; only its lost default is restored.
 */
type CloseRequestable = HTMLDialogElement & { requestClose?: () => void }

function isModal(dialog: HTMLDialogElement): boolean {
    try {
        return dialog.matches(':modal')
    } catch {
        // A browser without `:modal` also has nothing else to tell modal and non-modal dialogs apart with.
        return true
    }
}

/** The dialog the key is for: the one holding the target, else the last modal dialog open. */
function targetDialog(target: EventTarget | null): HTMLDialogElement | null {
    const open = Array.from(
        document.querySelectorAll<HTMLDialogElement>('dialog[open]')
    ).filter(isModal)
    const holding =
        target instanceof Element
            ? target.closest<HTMLDialogElement>('dialog')
            : null
    return holding && open.includes(holding)
        ? holding
        : (open[open.length - 1] ?? null)
}

function requestClose(dialog: CloseRequestable): void {
    if (typeof dialog.requestClose === 'function') {
        dialog.requestClose()
        return
    }
    // What the browser does on Escape: ask first, so a dialog can cancel the request.
    if (dialog.dispatchEvent(new Event('cancel', { cancelable: true }))) {
        dialog.close()
    }
}

function onKeydown(event: KeyboardEvent): void {
    // Escape also cancels an IME composition, and a held key repeats: it closes one dialog, not a stack of them.
    if (event.key !== 'Escape' || event.isComposing || event.repeat) return
    const dialog = targetDialog(event.target)
    if (!dialog) return
    // After the event, so that every handler has had its say. A key the browser acted on has already closed the dialog.
    setTimeout(() => {
        if (event.defaultPrevented && dialog.open) requestClose(dialog)
    })
}

export function bindModalEscape(): void {
    // Capture on `window`: it runs first, so the dialog is found before any handler can change the page.
    window.addEventListener('keydown', onKeydown, true)
}
