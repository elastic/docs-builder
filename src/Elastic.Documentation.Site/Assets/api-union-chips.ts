/**
 * Union variant lists with three or more variants show one variant at a time, picked from a row of chips that looks
 * and packs like the examples rail (chip-row.ts). The other variants stay in the page with hidden="until-found", so
 * find-in-page and links to their fields reach them and switch the chip.
 */
import {
    ChipRowParts,
    fitChipRow,
    observeChipRow,
    registerChipRows,
} from './chip-row'

const variantChips: ChipRowParts = {
    row: '[data-chip-row]',
    chip: '.api-example-chip[data-chip]',
    chipTitle: '[data-chip-title]',
    more: '[data-chip-more]',
    moreLabel: '[data-chip-more-label]',
    menu: '[data-chip-menu]',
    item: '[data-chip]',
    itemTitle: '[data-chip-item-title]',
    filter: '[data-chip-filter]',
    idAttribute: 'data-chip',
}

/** A union list that shows its variants behind chips, and one of its variants (never a variant of a plain list). */
const chipList = '.union-variant-chips'
const chipPanel = `${chipList} > .union-variants > .union-variant-item`

let delegated = false

function ownRow(container: HTMLElement): HTMLElement | null {
    return container.querySelector<HTMLElement>(`:scope > ${variantChips.row}`)
}

function ownPanels(container: HTMLElement): HTMLElement[] {
    return Array.from(
        container.querySelectorAll<HTMLElement>(
            ':scope > .union-variants > .union-variant-item'
        )
    )
}

/** Shows one variant of a chip list and marks its chip; the others stay findable through hidden="until-found". */
export function showVariant(container: HTMLElement, variantId: string): void {
    const panels = ownPanels(container)
    if (!panels.some((panel) => panel.id === variantId)) return
    const scrolled = window.scrollY
    panels.forEach((panel) => {
        if (panel.id === variantId) panel.removeAttribute('hidden')
        else panel.setAttribute('hidden', 'until-found')
    })
    keepScrollPosition(container, scrolled)
    const row = ownRow(container)
    if (!row) return
    row.querySelectorAll<HTMLElement>(variantChips.chip).forEach((chip) => {
        const match = chip.dataset.chip === variantId
        chip.classList.toggle('is-active', match)
        chip.setAttribute('aria-selected', match ? 'true' : 'false')
    })
    fitChipRow(row, variantChips)
}

/**
 * Near the bottom of the page, a shorter variant shortens the page below the current scroll position; the browser then
 * clamps the scroll and the chips jump under the pointer. The variant list keeps the height that holds the position,
 * and only for as long as it does: every switch starts from the list's own height.
 */
function keepScrollPosition(container: HTMLElement, scrolled: number) {
    const list = container.querySelector<HTMLElement>(
        ':scope > .union-variants'
    )
    if (!list) return
    list.style.minHeight = ''
    const maxScroll = document.documentElement.scrollHeight - window.innerHeight
    const shortfall = scrolled - maxScroll
    if (shortfall <= 0) return
    list.style.minHeight = `${list.offsetHeight + shortfall}px`
    window.scrollTo({ top: scrolled, behavior: 'instant' })
}

/** Whether a variant is the one its list shows: not hidden, and its chip the active one. */
function isShown(panel: HTMLElement, container: HTMLElement): boolean {
    if (panel.hasAttribute('hidden')) return false
    const chip = ownRow(container)?.querySelector<HTMLElement>(
        `${variantChips.chip}.is-active`
    )
    return chip?.dataset.chip === panel.id
}

/**
 * Shows every variant around `target` that its list does not show, outermost first, and returns whether there was
 * one. A link to a field can reach a hidden variant that the browser already revealed by itself while scrolling to
 * the fragment, before this script ran, so the active chip is checked too.
 */
export function revealVariantsAround(target: HTMLElement): boolean {
    const pending: [HTMLElement, HTMLElement][] = []
    for (
        let panel = target.closest<HTMLElement>(chipPanel);
        panel;
        panel = panel.parentElement?.closest<HTMLElement>(chipPanel) ?? null
    ) {
        const container = panel.closest<HTMLElement>(chipList)
        if (container && !isShown(panel, container))
            pending.unshift([container, panel])
    }
    pending.forEach(([container, panel]) => showVariant(container, panel.id))
    return pending.length > 0
}

function onClick(event: MouseEvent) {
    const target = event.target as HTMLElement | null
    const pick = target?.closest<HTMLElement>(
        `${variantChips.chip}, [data-chip-menu] [data-chip]`
    )
    const row = pick?.closest<HTMLElement>(variantChips.row)
    const container = row?.closest<HTMLElement>(chipList)
    if (!pick?.dataset.chip || !row || !container) return

    showVariant(container, pick.dataset.chip)
    const menu = pick.closest<HTMLElement>(variantChips.menu)
    if (!menu) return
    menu.hidePopover()
    row.querySelector<HTMLElement>(`${variantChips.chip}.is-active`)?.focus()
}

/** Find-in-page matched text inside a hidden variant: switch to it before the browser reveals it. */
function onBeforeMatch(event: Event) {
    const panel = (event.target as HTMLElement | null)?.closest<HTMLElement>(
        chipPanel
    )
    if (panel) revealVariantsAround(panel)
}

/** Fits every variant chip row in `root`. Safe to call again after an HTMX swap. */
export function initUnionChips(root: ParentNode = document): void {
    if (!delegated) {
        delegated = true
        document.addEventListener('click', onClick)
        document.addEventListener('beforematch', onBeforeMatch)
    }
    registerChipRows(variantChips)

    root.querySelectorAll<HTMLElement>(chipList).forEach((container) => {
        const row = ownRow(container)
        if (!row) return
        fitChipRow(row, variantChips)
        observeChipRow(row, variantChips)
        // Web fonts change the chips' widths once they load.
        if (document.fonts && document.fonts.status !== 'loaded')
            void document.fonts.ready.then(() => fitChipRow(row, variantChips))
    })
}
