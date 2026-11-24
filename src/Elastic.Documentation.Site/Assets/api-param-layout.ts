/**
 * Lets the reader lay out the flat parameter sections (path, query, header) as a list or a table.
 * The server renders the list. The choice is one attribute on each section, and it is remembered
 * across operation pages, so a reader who prefers the table is not asked again on every page.
 *
 * In the table a long description is clamped to a few lines with a Show more control on its row.
 * Only a description that overflows the clamp gets the control; the server renders none of it.
 */

export type ParamLayout = 'list' | 'table'

export const paramLayoutStorageKey = 'api-param-layout'

const sectionSelector = '[data-param-section]'
const switchSelector = '[data-param-layout]'
const optionSelector = `${switchSelector} [data-layout]`
const rowSelector =
    ':scope > .api-param-section-body > .property-list > .property-item'
// The description is the row's only dd without a class; values, examples and links each carry one.
const descriptionSelector = ':scope > dd:not([class])'
const clampToggleClass = 'api-param-clamp-toggle'

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

/** Whether a clamped description has more lines than the clamp shows. */
export type Overflows = (description: HTMLElement) => boolean

// Clamping removes the hidden lines from layout, so scrollHeight no longer tells; the heights with and without the clamp do.
const measuredOverflow: Overflows = (description) => {
    const row = description.closest<HTMLElement>('.property-item')
    if (!row) return false
    const previous = row.dataset.clamp
    delete row.dataset.clamp
    const full = description.offsetHeight
    row.dataset.clamp = 'clamped'
    const clamped = description.offsetHeight
    if (previous) row.dataset.clamp = previous
    else delete row.dataset.clamp
    return full > clamped + 1
}

function setClampToggle(toggle: HTMLButtonElement, expanded: boolean): void {
    toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false')
    toggle.textContent = expanded ? 'Show less' : 'Show more'
}

/**
 * Clamps every description in the section's rows and gives the rows that overflow a Show more control.
 * A row the reader expanded keeps its state; a row that no longer overflows (the table got wider) loses the control.
 */
export function clampDescriptions(
    section: HTMLElement,
    overflows: Overflows = measuredOverflow
): void {
    for (const row of section.querySelectorAll<HTMLElement>(rowSelector)) {
        const description = row.querySelector<HTMLElement>(descriptionSelector)
        let toggle = row.querySelector<HTMLButtonElement>(
            `:scope > .${clampToggleClass}`
        )
        if (!description) continue
        if (row.dataset.clamp !== 'expanded') row.dataset.clamp = 'clamped'
        if (row.dataset.clamp === 'clamped' && !overflows(description)) {
            delete row.dataset.clamp
            toggle?.remove()
            continue
        }
        if (toggle) continue
        const dt = row.querySelector<HTMLElement>(':scope > dt')
        if (dt?.id && !description.id) description.id = `${dt.id}-description`
        toggle = document.createElement('button')
        toggle.type = 'button'
        toggle.className = clampToggleClass
        if (description.id) toggle.setAttribute('aria-controls', description.id)
        setClampToggle(toggle, false)
        description.after(toggle)
    }
}

function unclampDescriptions(section: HTMLElement): void {
    for (const row of section.querySelectorAll<HTMLElement>(rowSelector)) {
        delete row.dataset.clamp
        row.querySelector(`:scope > .${clampToggleClass}`)?.remove()
    }
}

function toggleClamp(toggle: HTMLButtonElement): void {
    const row = toggle.closest<HTMLElement>('.property-item')
    if (!row) return
    const expanded = row.dataset.clamp !== 'expanded'
    row.dataset.clamp = expanded ? 'expanded' : 'clamped'
    setClampToggle(toggle, expanded)
}

/** Every section with a switch takes the layout, so one click lays out path, query and header parameters alike. */
export function applyParamLayout(
    layout: ParamLayout,
    root: ParentNode = document,
    overflows: Overflows = measuredOverflow
): void {
    for (const control of root.querySelectorAll<HTMLElement>(switchSelector)) {
        // The server renders the control hidden: without this script the Table button would do nothing.
        control.hidden = false
        const section = control.closest<HTMLElement>(sectionSelector)
        if (section) {
            section.dataset.layout = layout
            if (layout === 'table') clampDescriptions(section, overflows)
            else unclampDescriptions(section)
        }
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
        const target = e.target as Element | null
        const clamp = target?.closest<HTMLButtonElement>(`.${clampToggleClass}`)
        if (clamp) {
            toggleClamp(clamp)
            return
        }
        const option = target?.closest<HTMLElement>(optionSelector)
        if (!option) return
        const layout = option.dataset.layout
        if (!isParamLayout(layout)) return
        rememberParamLayout(layout)
        applyParamLayout(layout)
    })
}
