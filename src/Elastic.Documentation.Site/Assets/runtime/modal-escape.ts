/**
 * A modal `<dialog>` closes on Escape because the browser runs a default action after the keydown, and a handler that
 * calls `preventDefault()` cancels it. The elastic.co header does that for every Escape on assembled sites, so no modal
 * dialog on the site closed. This restores the default action where it was cancelled: when the key event is over, the
 * key was prevented, and a modal dialog is still open, the dialog gets the request the browser would have made. That is
 * a `cancel` event, then `close()`, so a dialog keeps the platform's way to stay open: cancel its `cancel` event.
 *
 * An open popover menu (floating.ts) loses its Escape the same way, so it gets `hidePopover()` where the key was prevented.
 * It takes the key before a dialog does, as the browser's own close requests go to the most recently opened first.
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

// The browser has no API for the order of the top layer, so the order in which dialogs open is recorded as they do.
const openedAt = new WeakMap<HTMLDialogElement, number>()
let opened = 0
let observer: MutationObserver | undefined

function noteOpening(dialog: HTMLDialogElement): void {
    if (dialog.open && !openedAt.has(dialog)) openedAt.set(dialog, ++opened)
}

// One record per change of the attribute, so a close and a reopen in the same task are told apart. The dialog's state
// then is only the end of the story: the old value says whether a change was an opening.
function noteOpenings(records: MutationRecord[]): void {
    const changed = new Set<HTMLDialogElement>()
    for (const { target, oldValue } of records) {
        if (!(target instanceof HTMLDialogElement)) continue
        changed.add(target)
        if (oldValue === null) openedAt.set(target, ++opened)
    }
    changed.forEach((dialog) => {
        if (!dialog.open) openedAt.delete(dialog)
    })
}

function lastOpened(dialogs: HTMLDialogElement[]): HTMLDialogElement | null {
    let last: HTMLDialogElement | null = null
    for (const dialog of dialogs) {
        if (!last || (openedAt.get(dialog) ?? 0) >= (openedAt.get(last) ?? 0))
            last = dialog
    }
    return last
}

/** The dialog the key is for: the one holding the target, else the modal dialog opened last. */
function targetDialog(target: EventTarget | null): HTMLDialogElement | null {
    // Openings the observer has not reported yet: a dialog can open in the same task as the key.
    if (observer) noteOpenings(observer.takeRecords())
    const open = Array.from(
        document.querySelectorAll<HTMLDialogElement>('dialog[open]')
    ).filter(isModal)
    const holding =
        target instanceof Element
            ? target.closest<HTMLDialogElement>('dialog')
            : null
    return holding && open.includes(holding) ? holding : lastOpened(open)
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

/**
 * The open popover the key is for: the one holding the target, else the last one open. A manual popover is left to its
 * owner, since the browser never closes it on Escape either.
 */
function targetPopover(target: EventTarget | null): HTMLElement | null {
    let open: HTMLElement[]
    try {
        open = Array.from(
            document.querySelectorAll<HTMLElement>(
                '[popover]:not([popover="manual"]):popover-open'
            )
        )
    } catch {
        // A browser without `:popover-open` has no popovers to close.
        return null
    }
    const holding =
        target instanceof Element
            ? open.find((popover) => popover.contains(target))
            : undefined
    return holding ?? open[open.length - 1] ?? null
}

function onKeydown(event: KeyboardEvent): void {
    // Escape also cancels an IME composition, and a held key repeats: it closes one dialog, not a stack of them.
    if (event.key !== 'Escape' || event.isComposing || event.repeat) return
    const popover = targetPopover(event.target)
    if (popover) {
        setTimeout(() => {
            if (event.defaultPrevented && popover.matches(':popover-open'))
                popover.hidePopover()
        })
        return
    }
    const dialog = targetDialog(event.target)
    if (!dialog) return
    // After the event, so that every handler has had its say. A key the browser acted on has already closed the dialog.
    setTimeout(() => {
        if (event.defaultPrevented && dialog.open) requestClose(dialog)
    })
}

export function bindModalEscape(): void {
    // Dialogs already open count in document order; later ones as they open.
    document
        .querySelectorAll<HTMLDialogElement>('dialog[open]')
        .forEach(noteOpening)
    observer = new MutationObserver(noteOpenings)
    observer.observe(document, {
        subtree: true,
        attributes: true,
        attributeOldValue: true,
        attributeFilter: ['open'],
    })
    // Capture on `window`: it runs first, so the dialog is found before any handler can change the page.
    window.addEventListener('keydown', onKeydown, true)
}
