/**
 * Lets the reader lay out the flat parameter sections (path, query, header) as a list or a table.
 * The server renders the list. The choice is one attribute on each section, and it is remembered
 * across operation pages, so a reader who prefers the table is not asked again on every page.
 */

export type ParamLayout = 'list' | 'table'

export const paramLayoutStorageKey = 'api-param-layout'

const sectionSelector = '[data-param-section]'
const switchSelector = '[data-param-layout]'
const optionSelector = `${switchSelector} [data-layout]`

function isParamLayout(value: unknown): value is ParamLayout {
    return value === 'list' || value === 'table'
}

// Storage can be blocked, and a throw here would stop the initializers that run after this one. The page then opens as a list.
export function storedParamLayout(): ParamLayout {
    try {
        const value = localStorage.getItem(paramLayoutStorageKey)
        return isParamLayout(value) ? value : 'list'
    } catch {
        return 'list'
    }
}

function rememberParamLayout(layout: ParamLayout): void {
    try {
        if (layout === 'list') localStorage.removeItem(paramLayoutStorageKey)
        else localStorage.setItem(paramLayoutStorageKey, layout)
    } catch {
        // The switch still works for this page.
    }
}

/** Every section with a switch takes the layout, so one click lays out path, query and header parameters alike. */
export function applyParamLayout(
    layout: ParamLayout,
    root: ParentNode = document
): void {
    for (const control of root.querySelectorAll<HTMLElement>(switchSelector)) {
        const section = control.closest<HTMLElement>(sectionSelector)
        if (section) section.dataset.layout = layout
        for (const option of control.querySelectorAll<HTMLElement>(
            '[data-layout]'
        ))
            option.setAttribute(
                'aria-pressed',
                option.dataset.layout === layout ? 'true' : 'false'
            )
    }
}

let clickHandlerBound = false

/** Applies the remembered layout to the sections on the page and binds the switch once, for every page swap. */
export function initParamLayout(): void {
    applyParamLayout(storedParamLayout())
    if (clickHandlerBound) return
    clickHandlerBound = true
    document.addEventListener('click', (e) => {
        const option = (e.target as Element | null)?.closest<HTMLElement>(
            optionSelector
        )
        if (!option) return
        const layout = option.dataset.layout
        if (!isParamLayout(layout)) return
        rememberParamLayout(layout)
        applyParamLayout(layout)
    })
}
