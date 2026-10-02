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

// Wrappers currently being watched. htmx swaps detach old table-wrappers
// from the DOM without ever removing them from the observer, so this lets
// initTable() drop exactly the ones that left instead of disconnecting and
// re-observing everything - which would re-trigger ResizeObserver's initial
// callback for every table still on the page, not just the ones that changed.
const observedWrappers = new Set<HTMLElement>()

/**
 * The fullscreen copy of a `:filterable:` table has to bring its own controls.
 * Cloning just the .table-wrapper would carry the `hidden` rows of an active
 * filter into the dialog with no search box, dropdowns or count - a subset of
 * the table with no way to see or clear the filter. So clone the host element
 * instead, strip the enhancement so the custom element rebuilds live controls
 * (listeners don't survive cloneNode), and copy the reader's filter across.
 */
function cloneTableForModal(wrapper: HTMLElement): {
    node: HTMLElement
    source: Element | null
} {
    const host = wrapper.closest('filterable-table')
    if (!host)
        return { node: wrapper.cloneNode(true) as HTMLElement, source: null }

    const clone = host.cloneNode(true) as HTMLElement
    clone.querySelector('.filterable-table-controls')?.remove()
    delete clone.dataset.enhanced
    delete clone.dataset.filtered
    for (const row of clone.querySelectorAll<HTMLTableRowElement>('tbody tr'))
        row.hidden = false

    // Drop the .table-container this table is already wrapped in: it carries
    // its own expand button, which inside the dialog would open a second one.
    // Promoting .table-wrapper back to a direct child also restores the
    // structure dialog.table-modal's own styles are written against.
    const container = clone.querySelector('.table-container')
    const inner = container?.querySelector('.table-wrapper')
    if (container && inner) container.replaceWith(inner)

    return { node: clone, source: host }
}

function copyFilterState(source: Element, clone: Element): void {
    const from = source.querySelector<HTMLInputElement>(
        '.filterable-table-search'
    )
    const to = clone.querySelector<HTMLInputElement>('.filterable-table-search')
    if (from && to && from.value) {
        to.value = from.value
        to.dispatchEvent(new Event('input'))
    }

    // Same table, so the dropdowns are generated in the same column order.
    const fromSelects = source.querySelectorAll<HTMLSelectElement>(
        '.filterable-table-facet select'
    )
    const toSelects = clone.querySelectorAll<HTMLSelectElement>(
        '.filterable-table-facet select'
    )
    fromSelects.forEach((select, i) => {
        const target = toSelects[i]
        if (!target || !select.value) return
        target.value = select.value
        target.dispatchEvent(new Event('change'))
    })
}

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

    const { node, source } = cloneTableForModal(wrapper)
    content.append(closeBtn, node)
    dialog.appendChild(content)

    // Native <dialog> handles Escape and focus trapping; backdrop click is ours.
    dialog.addEventListener('click', (e) => {
        if (e.target === dialog) dialog.close()
    })
    dialog.addEventListener('close', () => dialog.remove())

    document.body.appendChild(dialog)
    dialog.showModal()

    // Appending upgrades the cloned custom element and builds its controls,
    // so the reader's filter can only be replayed onto them afterwards.
    if (source) copyFilterState(source, node)
}

/**
 * Adds a fullscreen button to markdown tables that overflow the content column.
 */
export function initTable(): void {
    for (const wrapper of observedWrappers) {
        if (wrapper.isConnected) continue
        overflowObserver.unobserve(wrapper)
        observedWrappers.delete(wrapper)
    }

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
        overflowObserver.observe(wrapper)
        observedWrappers.add(wrapper)
    })
}
