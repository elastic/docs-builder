/**
 * A row of chips that wraps over at most two lines. The chips that do not fit move into a "+N more" popover menu,
 * which can carry a fuzzy filter. Used by the examples rail and by long lists of union variants; each passes the
 * selectors of its own markup, and picks what a chip does on click itself.
 */
import Fuse from 'fuse.js'
import type { Instance } from 'tippy.js'

/** Selectors for the parts of one kind of chip row; `idAttribute` names the attribute a chip and its menu item share. */
export interface ChipRowParts {
    row: string
    chip: string
    chipTitle: string
    more: string
    moreLabel: string
    menu: string
    item: string
    itemTitle: string
    filter: string
    idAttribute: string
}

const maxChipRows = 2

function chipId(element: HTMLElement, parts: ChipRowParts): string | null {
    return element.getAttribute(parts.idAttribute)
}

/** Lines a run of items wraps into, the way flex-wrap lays them out. */
function lineCount(widths: number[], rowWidth: number, gap: number): number {
    let lines = 1
    let x = 0
    for (const width of widths.map((w) => Math.min(w, rowWidth))) {
        if (x > 0 && x + gap + width > rowWidth) {
            lines++
            x = width
        } else x = x > 0 ? x + gap + width : width
    }
    return lines
}

/**
 * Which chips stay in the row: all of them when they fit in `maxRows` lines, otherwise as many from the start as
 * fit next to the "more" button. The active chip always stays, taking the place of the last one that fits.
 */
export function packChips(
    widths: number[],
    rowWidth: number,
    gap: number,
    moreWidth: number,
    maxRows: number,
    activeIndex: number
): number[] {
    const all = widths.map((_, i) => i)
    if (lineCount(widths, rowWidth, gap) <= maxRows) return all

    const fits = (indexes: number[]) =>
        lineCount(
            [...indexes.map((i) => widths[i]), moreWidth],
            rowWidth,
            gap
        ) <= maxRows
    for (let keep = widths.length - 1; keep > 0; keep--) {
        const candidate = all.slice(0, keep)
        if (activeIndex >= keep) candidate[keep - 1] = activeIndex
        if (fits(candidate)) return candidate
    }
    return activeIndex >= 0 ? [activeIndex] : []
}

function chipMenuSupported(menu: HTMLElement | null): menu is HTMLElement {
    return !!menu && typeof menu.hidePopover === 'function'
}

/** Removes the tooltip text and the tippy instance already made from it, which keeps the old text. */
function clearChipTooltip(chip: HTMLElement) {
    delete chip.dataset.tippyContent
    ;(chip as HTMLElement & { _tippy?: Instance })._tippy?.destroy()
}

function setMoreLabel(more: HTMLElement, parts: ChipRowParts, count: number) {
    more.querySelector(parts.moreLabel)!.textContent = `+${count} more`
}

/** Moves the chips that do not fit in two lines into the "more" menu. Without popover support every chip stays. */
export function fitChipRow(row: HTMLElement, parts: ChipRowParts): void {
    // Until the first fit the row is clipped to two lines (api-docs.css), so the page does not jump when chips move into the menu.
    row.classList.add('is-fitted')
    const more = row.querySelector<HTMLElement>(parts.more)
    const menu = row.querySelector<HTMLElement>(parts.menu)
    if (!more || !chipMenuSupported(menu)) return

    const chips = Array.from(row.querySelectorAll<HTMLElement>(parts.chip))
    chips.forEach((chip) => (chip.hidden = false))
    more.hidden = false
    // Measured with the longest label it can get, so the room kept for it is enough.
    setMoreLabel(more, parts, chips.length)
    const rowWidth = row.clientWidth
    // Unmeasurable (display: none, or no layout): leave every chip in place.
    if (rowWidth === 0) {
        more.hidden = true
        return
    }

    // Everything is measured before anything is written, so the fit costs one layout pass.
    const gap = parseFloat(getComputedStyle(row).columnGap) || 6
    // +1 because offsetWidth rounds down, and a chip a fraction too wide would wrap onto a third line.
    const widths = chips.map((chip) => chip.offsetWidth + 1)
    const titles = chips.map((chip) =>
        chip.querySelector<HTMLElement>(parts.chipTitle)
    )
    const truncated = titles.map((t) => !!t && t.scrollWidth > t.clientWidth)
    const visible = new Set(
        packChips(
            widths,
            rowWidth,
            gap,
            more.offsetWidth + 1,
            maxChipRows,
            chips.findIndex((chip) => chip.classList.contains('is-active'))
        )
    )

    const hiddenCount = chips.length - visible.size
    const overflow = new Set<string | null>()
    chips.forEach((chip, i) => {
        chip.hidden = !visible.has(i)
        if (chip.hidden) overflow.add(chipId(chip, parts))
        // A title cut off by the chip's width gets a tooltip with the whole of it; one that fits again loses it.
        else if (truncated[i])
            chip.dataset.tippyContent = titles[i]?.textContent?.trim() ?? ''
        else clearChipTooltip(chip)
    })
    menu.querySelectorAll<HTMLElement>(parts.item).forEach((item) => {
        item.hidden = !overflow.has(chipId(item, parts))
    })
    more.hidden = hiddenCount === 0
    setMoreLabel(more, parts, hiddenCount)
    if (hiddenCount === 0) menu.hidePopover()
}

/** Refits when the row changes width, since chip titles wrap differently. */
export function observeChipRow(row: HTMLElement, parts: ChipRowParts): void {
    if (row.dataset.chipsObserved || typeof ResizeObserver === 'undefined')
        return
    row.dataset.chipsObserved = 'true'
    let width = row.clientWidth
    new ResizeObserver(() => {
        // Only a change of width: refitting changes the row's height, which must not trigger another round.
        if (row.clientWidth === width) return
        width = row.clientWidth
        fitChipRow(row, parts)
    }).observe(row)
}

const registered: ChipRowParts[] = []
let openMenu: { menu: HTMLElement; more: HTMLElement } | null = null
let followQueued = false

function partsOf(element: Element): ChipRowParts | undefined {
    return registered.find((parts) => element.matches(parts.menu))
}

/** Keeps the open menu with its button as the page scrolls or resizes, at most once a frame. */
function followOpenMenu() {
    if (!openMenu || followQueued) return
    followQueued = true
    requestAnimationFrame(() => {
        followQueued = false
        if (openMenu) positionMenu(openMenu.menu, openMenu.more)
    })
}

/** Under the "more" button, or above it when there is no room below. Run again on scroll, so the menu stays with its button. */
function positionMenu(menu: HTMLElement, more: HTMLElement) {
    const anchor = more.getBoundingClientRect()
    const size = menu.getBoundingClientRect()
    const margin = 8
    const left = Math.max(
        margin,
        Math.min(anchor.left, window.innerWidth - size.width - margin)
    )
    const below = anchor.bottom + 4
    const top =
        below + size.height > window.innerHeight - margin &&
        anchor.top - 4 - size.height > margin
            ? anchor.top - 4 - size.height
            : below
    menu.style.left = `${left}px`
    menu.style.top = `${top}px`
}

/** Opens under the "more" button, or above it when there is no room below, and keeps the button's state in step. */
function onMenuToggle(event: Event) {
    const menu = event.target as HTMLElement | null
    const parts = menu && partsOf(menu)
    if (!menu || !parts) return
    const more = menu
        .closest<HTMLElement>(parts.row)
        ?.querySelector<HTMLElement>(parts.more)
    const open = (event as ToggleEvent).newState === 'open'
    more?.setAttribute('aria-expanded', open ? 'true' : 'false')
    const filter = menu.querySelector<HTMLInputElement>(parts.filter)
    openMenu = open && more ? { menu, more } : null
    if (!open && filter?.value) {
        filter.value = ''
        filterMenu(filter, parts)
    }
    if (!open || !more) return

    positionMenu(menu, more)
    if (filter) filter.focus()
    else menuItems(menu)[0]?.focus()
}

function menuItems(menu: HTMLElement): HTMLElement[] {
    return Array.from(
        menu.querySelectorAll<HTMLElement>('[role="menuitem"]:not([hidden])')
    )
}

interface MenuSearch {
    fuse: Fuse<{ item: HTMLElement; title: string }>
    all: HTMLElement[]
}

const menuSearches = new WeakMap<HTMLElement, MenuSearch>()

function menuSearch(menu: HTMLElement, parts: ChipRowParts): MenuSearch {
    let search = menuSearches.get(menu)
    if (!search) {
        const all = Array.from(
            menu.querySelectorAll<HTMLElement>('[role="menuitem"]')
        )
        const entries = all.map((item) => ({
            item,
            title:
                item.querySelector(parts.itemTitle)?.textContent?.trim() ??
                item.textContent?.trim() ??
                '',
        }))
        // Titles are long sentences: a match anywhere in them counts, and a typo or two is forgiven.
        search = {
            fuse: new Fuse(entries, {
                keys: ['title'],
                ignoreLocation: true,
                threshold: 0.35,
            }),
            all,
        }
        menuSearches.set(menu, search)
    }
    return search
}

/** Every word must match (fuzzily) somewhere in the title; the best combined score ranks first. */
function searchWords(fuse: MenuSearch['fuse'], query: string): HTMLElement[] {
    const scores = new Map<HTMLElement, number>()
    query
        .split(/\s+/)
        .filter(Boolean)
        .forEach((word, i) => {
            const found = new Map(
                fuse.search(word).map((r) => [r.item.item, r.score ?? 0])
            )
            if (i === 0) found.forEach((score, item) => scores.set(item, score))
            else
                scores.forEach((score, item) => {
                    const next = found.get(item)
                    if (next === undefined) scores.delete(item)
                    else scores.set(item, score + next)
                })
        })
    return [...scores.entries()]
        .sort((x, y) => x[1] - y[1])
        .map(([item]) => item)
}

/** Fuzzy filter: shows the menu items that are not in the row and match, best match first. */
function filterMenu(input: HTMLInputElement, parts: ChipRowParts) {
    const menu = input.closest<HTMLElement>(parts.menu)
    const row = input.closest<HTMLElement>(parts.row)
    if (!menu || !row) return
    const inRow = new Set(
        Array.from(row.querySelectorAll<HTMLElement>(parts.chip))
            .filter((chip) => !chip.hidden)
            .map((chip) => chipId(chip, parts))
    )
    const { fuse, all } = menuSearch(menu, parts)
    const query = input.value.trim()
    const matches = query ? searchWords(fuse, query) : all
    const shown = matches.filter((item) => !inRow.has(chipId(item, parts)))
    const shownSet = new Set(shown)
    const parent = shown[0]?.parentElement ?? all[0]?.parentElement
    // Matches first, in rank order; the rest follow hidden. Without a query, the original order comes back.
    const order = [...shown, ...all.filter((item) => !shownSet.has(item))]
    order.forEach((item) => {
        item.hidden = !shownSet.has(item)
        parent?.appendChild(item)
    })
}

/** Arrow keys, Home and End move focus through the visible menu items. Returns whether the key was handled. */
function moveInMenu(menu: HTMLElement, from: HTMLElement, key: string) {
    const items = menuItems(menu)
    const index = items.indexOf(from)
    const next: Record<string, number> = {
        ArrowDown: Math.min(items.length - 1, index + 1),
        ArrowUp: index <= 0 ? 0 : index - 1,
        Home: 0,
        End: items.length - 1,
    }
    if (!(key in next) || items.length === 0) return false
    // In the filter field, Home and End move the caret.
    if (index < 0 && (key === 'Home' || key === 'End')) return false
    items[next[key]].focus()
    return true
}

function onKeydown(event: KeyboardEvent) {
    const target = event.target as HTMLElement | null
    if (!target || event.metaKey || event.ctrlKey || event.altKey) return
    for (const parts of registered) {
        const menu = target.closest<HTMLElement>(parts.menu)
        if (menu && moveInMenu(menu, target, event.key)) {
            event.preventDefault()
            return
        }
    }
}

function onInput(event: Event) {
    const input = event.target as HTMLElement | null
    if (!input) return
    const parts = registered.find((p) => input.matches(p.filter))
    if (!parts) return
    filterMenu(input as HTMLInputElement, parts)
    // A menu that opened above its button shrinks as it filters; place it again so it stays against the button.
    if (openMenu) positionMenu(openMenu.menu, openMenu.more)
}

/**
 * Wires the "more" menus of one kind of chip row: placement, keyboard moves and the filter. Listeners are delegated
 * from the document and added once, so it is safe to call again after an HTMX swap.
 */
export function registerChipRows(parts: ChipRowParts): void {
    if (registered.includes(parts)) return
    if (registered.length === 0) {
        // toggle does not bubble, so it is caught on the way down.
        document.addEventListener('toggle', onMenuToggle, true)
        document.addEventListener('keydown', onKeydown)
        document.addEventListener('input', onInput)
        window.addEventListener('scroll', followOpenMenu, {
            capture: true,
            passive: true,
        })
        window.addEventListener('resize', followOpenMenu)
    }
    registered.push(parts)
}
