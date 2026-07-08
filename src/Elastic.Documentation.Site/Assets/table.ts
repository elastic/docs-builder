import { fullscreenIcon } from './icons'

const closeIcon = `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor"><path d="M8 6.586 13.293 1.293l1.414 1.414L9.414 8l5.293 5.293-1.414 1.414L8 9.414l-5.293 5.293-1.414-1.414L6.586 8 1.293 2.707l1.414-1.414L8 6.586Z"/></svg>`

// Keeps data-overflowing on each .table-container in sync with whether its
// table actually overflows, so the expand button only shows when useful.
const overflowObserver = new ResizeObserver((entries) => {
    for (const entry of entries) {
        const wrapper = entry.target as HTMLElement
        wrapper.parentElement?.toggleAttribute(
            'data-overflowing',
            wrapper.scrollWidth > wrapper.clientWidth
        )
    }
})

function openTableModal(wrapper: HTMLElement): void {
    const dialog = document.createElement('dialog')
    dialog.className = 'table-modal'

    const content = document.createElement('div')
    content.className = 'table-modal-content'

    const closeBtn = document.createElement('button')
    closeBtn.type = 'button'
    closeBtn.className = 'table-modal-close'
    closeBtn.setAttribute('aria-label', 'Close')
    closeBtn.title = 'Close'
    closeBtn.innerHTML = closeIcon
    closeBtn.addEventListener('click', () => dialog.close())

    content.append(closeBtn, wrapper.cloneNode(true))
    dialog.appendChild(content)

    // Native <dialog> handles Escape and focus trapping; backdrop click is ours.
    dialog.addEventListener('click', (e) => {
        if (e.target === dialog) dialog.close()
    })
    dialog.addEventListener('close', () => dialog.remove())

    document.body.appendChild(dialog)
    dialog.showModal()
}

/**
 * Adds a fullscreen button to markdown tables that overflow the content column.
 */
export function initTable(): void {
    const newWrappers = document.querySelectorAll<HTMLElement>(
        '.markdown-content .table-wrapper:not([data-table-expand])'
    )
    newWrappers.forEach((wrapper) => {
        wrapper.setAttribute('data-table-expand', '')

        const container = document.createElement('div')
        container.className = 'table-container'
        wrapper.replaceWith(container)

        const expandBtn = document.createElement('button')
        expandBtn.type = 'button'
        expandBtn.className = 'table-expand-btn'
        expandBtn.setAttribute('aria-label', 'View table fullscreen')
        expandBtn.title = 'View table fullscreen'
        expandBtn.innerHTML = fullscreenIcon
        expandBtn.addEventListener('click', () => openTableModal(wrapper))

        container.append(expandBtn, wrapper)
    })

    // Re-sync to exactly the wrappers on the page now. htmx swaps detach old
    // table-wrappers from the DOM without ever removing them from this
    // observer, so without this it would keep a strong reference to every
    // table wrapper ever seen this session, across every past page.
    overflowObserver.disconnect()
    document
        .querySelectorAll<HTMLElement>('.markdown-content .table-wrapper')
        .forEach((wrapper) => overflowObserver.observe(wrapper))
}
