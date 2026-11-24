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
// The control lives in a dd of its own: a dl group may hold only terms and descriptions.
const clampRowClass = 'api-param-clamp-row'
const clampToggleSelector = `:scope > .${clampRowClass} > .${clampToggleClass}`
// In the table a parameter's constraints (min, max, pattern) leave the Type cell for a row of their own in the
// description column, so the Type column holds only the primitive and the constraints have room to read.
const constraintRowClass = 'api-param-constraints'
const constraintGroupSelector =
    ':scope > dt > code.schema-type > .schema-type-group'
const constraintMarkers = new WeakMap<Element, Comment>()

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

/** Whether the stylesheet lays the section out as a table: the choice is remembered, but a narrow section keeps the list. */
export type TableApplied = (section: HTMLElement) => boolean

const measuredTableApplied: TableApplied = (section) => {
    const body = section.querySelector<HTMLElement>(
        ':scope > .api-param-section-body'
    )
    return body !== null && getComputedStyle(body).display === 'grid'
}

// The clamp swaps the cell's bottom padding for a margin, so only the content box compares across the two states.
function contentHeight(el: HTMLElement): number {
    const style = getComputedStyle(el)
    return (
        el.clientHeight -
        parseFloat(style.paddingTop) -
        parseFloat(style.paddingBottom)
    )
}

// Clamping removes the hidden lines from layout, so scrollHeight no longer tells; the heights with and without the clamp do.
const measuredOverflow: Overflows = (description) => {
    const row = description.closest<HTMLElement>('.property-item')
    if (!row) return false
    const previous = row.dataset.clamp
    delete row.dataset.clamp
    const full = contentHeight(description)
    row.dataset.clamp = 'clamped'
    const clamped = contentHeight(description)
    if (previous) row.dataset.clamp = previous
    else delete row.dataset.clamp
    return full > clamped + 1
}

function setClampToggle(toggle: HTMLButtonElement, expanded: boolean): void {
    toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false')
    toggle.textContent = expanded ? 'Show less' : 'Show more'
}

function setRowExpanded(row: HTMLElement, expanded: boolean): void {
    row.dataset.clamp = expanded ? 'expanded' : 'clamped'
    const toggle = row.querySelector<HTMLButtonElement>(clampToggleSelector)
    if (toggle) setClampToggle(toggle, expanded)
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
        const toggle = row.querySelector<HTMLButtonElement>(clampToggleSelector)
        if (!description) continue
        if (row.dataset.clamp !== 'expanded') row.dataset.clamp = 'clamped'
        if (row.dataset.clamp === 'clamped' && !overflows(description)) {
            delete row.dataset.clamp
            toggle?.parentElement?.remove()
            continue
        }
        if (toggle) continue
        const dt = row.querySelector<HTMLElement>(':scope > dt')
        if (dt?.id && !description.id) description.id = `${dt.id}-description`
        const control = document.createElement('button')
        control.type = 'button'
        control.className = clampToggleClass
        if (description.id)
            control.setAttribute('aria-controls', description.id)
        setClampToggle(control, false)
        const clampRow = document.createElement('dd')
        clampRow.className = clampRowClass
        clampRow.append(control)
        description.after(clampRow)
    }
}

function unclampDescriptions(section: HTMLElement): void {
    for (const row of section.querySelectorAll<HTMLElement>(rowSelector)) {
        delete row.dataset.clamp
        row.querySelector(`:scope > .${clampRowClass}`)?.remove()
    }
}

/**
 * Moves the row's constraints from the Type cell to a labelled row below the description, as the Values row is laid
 * out. Each group leaves a marker behind so the list gets it back in its place.
 */
function placeConstraints(row: HTMLElement): void {
    if (row.querySelector(`:scope > .${constraintRowClass}`)) return
    const groups = row.querySelectorAll<HTMLElement>(constraintGroupSelector)
    if (groups.length === 0) return
    const focused = focusedWithin(groups)
    const constraints = document.createElement('dd')
    constraints.className = constraintRowClass
    const label = document.createElement('span')
    label.className = 'values-label'
    label.textContent = 'Constraints:'
    constraints.append(label)
    for (const group of groups) {
        const marker = document.createComment('constraint')
        constraintMarkers.set(group, marker)
        group.replaceWith(marker)
        constraints.append(group)
    }
    const after =
        row.querySelector<HTMLElement>(`:scope > .${clampRowClass}`) ??
        row.querySelector<HTMLElement>(descriptionSelector) ??
        row.querySelector<HTMLElement>(':scope > dt')
    after?.after(constraints)
    focused?.focus({ preventScroll: true })
}

function restoreConstraints(row: HTMLElement): void {
    const constraints = row.querySelector<HTMLElement>(
        `:scope > .${constraintRowClass}`
    )
    if (!constraints) return
    const groups =
        constraints.querySelectorAll<HTMLElement>('.schema-type-group')
    const focused = focusedWithin(groups)
    for (const group of groups) {
        const marker = constraintMarkers.get(group)
        if (marker?.parentNode) marker.replaceWith(group)
        constraintMarkers.delete(group)
    }
    constraints.remove()
    focused?.focus({ preventScroll: true })
}

// A constraint with a tooltip is a tab stop. Moving it out of the document drops the focus, so the reader who was on it
// when the layout changed (a zoom can do that) gets it back in the new place.
function focusedWithin(groups: Iterable<HTMLElement>): HTMLElement | null {
    const active = document.activeElement
    if (!(active instanceof HTMLElement)) return null
    for (const group of groups) if (group.contains(active)) return active
    return null
}

/** Lays out the section's rows for the table: constraints move only while the stylesheet applies the table. */
function layoutTableRows(
    section: HTMLElement,
    overflows: Overflows,
    tableApplied: TableApplied
): void {
    const applied = tableApplied(section)
    for (const row of section.querySelectorAll<HTMLElement>(rowSelector)) {
        if (applied) placeConstraints(row)
        else restoreConstraints(row)
    }
    clampDescriptions(section, overflows)
}

function layoutListRows(section: HTMLElement): void {
    unclampDescriptions(section)
    for (const row of section.querySelectorAll<HTMLElement>(rowSelector))
        restoreConstraints(row)
}

const observedSections = new WeakSet<HTMLElement>()

/**
 * A section measured while collapsed, or while its column was too narrow for the table, has no clamps. When it opens
 * or its width changes, the rows are measured again. The browsers without ResizeObserver also lack subgrid, so they
 * never show the table.
 */
function observeSection(
    section: HTMLElement,
    overflows: Overflows,
    tableApplied: TableApplied
): void {
    if (observedSections.has(section) || typeof ResizeObserver === 'undefined')
        return
    observedSections.add(section)
    // Laying the rows out changes the section's own height, which the observer would report as an undelivered
    // notification if it happened inside the delivery; the next frame is outside it, and one frame serves a burst.
    let frame = 0
    new ResizeObserver(() => {
        if (frame) return
        frame = requestAnimationFrame(() => {
            frame = 0
            if (section.dataset.layout === 'table')
                layoutTableRows(section, overflows, tableApplied)
        })
    }).observe(section)
}

/** Every section with a switch takes the layout, so one click lays out path, query and header parameters alike. */
export function applyParamLayout(
    layout: ParamLayout,
    root: ParentNode = document,
    overflows: Overflows = measuredOverflow,
    tableApplied: TableApplied = measuredTableApplied
): void {
    for (const control of root.querySelectorAll<HTMLElement>(switchSelector)) {
        // The server renders the control hidden: without this script the Table button would do nothing.
        control.hidden = false
        const section = control.closest<HTMLElement>(sectionSelector)
        if (section) {
            section.dataset.layout = layout
            if (layout === 'table') {
                layoutTableRows(section, overflows, tableApplied)
                observeSection(section, overflows, tableApplied)
            } else layoutListRows(section)
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

let handlersBound = false

/** Applies the remembered layout to the sections on the page and binds the handlers once, for every page swap. */
export function initParamLayout(): void {
    applyParamLayout(storedParamLayout())
    if (handlersBound) return
    handlersBound = true
    document.addEventListener('click', (e) => {
        const target = e.target as Element | null
        const toggle = target?.closest<HTMLButtonElement>(
            `.${clampToggleClass}`
        )
        if (toggle) {
            const row = toggle.closest<HTMLElement>('.property-item')
            if (row) setRowExpanded(row, row.dataset.clamp !== 'expanded')
            return
        }
        const option = target?.closest<HTMLElement>(optionSelector)
        if (!option) return
        const layout = option.dataset.layout
        if (!isParamLayout(layout)) return
        rememberParamLayout(layout)
        applyParamLayout(layout)
    })
    // The clamp only clips the text, so a link below the third line is still a tab stop: focus inside shows the row.
    // A nested field's own description sits beside the row's, not inside it, so focus there leaves the row clamped.
    document.addEventListener('focusin', (e) => {
        const target = e.target as Element | null
        const row = target?.closest<HTMLElement>(
            '.property-item[data-clamp="clamped"]'
        )
        const description = row?.querySelector<HTMLElement>(descriptionSelector)
        if (row && target && description?.contains(target))
            setRowExpanded(row, true)
    })
}
