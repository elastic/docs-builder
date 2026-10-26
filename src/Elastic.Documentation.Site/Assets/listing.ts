// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

/**
 * Client-side filter for `.listing-root` pages.
 *
 * Supports:
 * - Text input: hides cards whose `data-listing-title` does not include the query.
 * - Group chips: a click selects one group. Cmd or Ctrl click adds or
 *   removes groups. A card is visible when any of its groups is in the active
 *   set (or when no groups are selected = show all). "All" clears the set.
 * - One card may belong to several groups via space-separated
 *   `data-listing-groups` or a single `data-listing-group`.
 * - Hides group headings when all their cards are hidden.
 * - Shows a "No pages match" notice when no card is visible.
 */

export interface ListingFilterState {
    query: string
    activeGroups: Set<string> // empty = all groups shown
}

function parseListingGroups(raw: string): string[] {
    return raw.split(/\s+/).filter(Boolean)
}

/** Returns true when a card should be visible given the current filter state. */
export function cardIsVisible(
    cardTitle: string,
    cardGroup: string,
    state: ListingFilterState
): boolean {
    if (state.activeGroups.size > 0) {
        const groups = parseListingGroups(cardGroup)
        if (!groups.some((group) => state.activeGroups.has(group))) return false
    }
    if (state.query !== '' && !cardTitle.includes(state.query.toLowerCase()))
        return false
    return true
}

function cardGroupAttribute(card: HTMLElement): string {
    return card.dataset.listingGroups ?? card.dataset.listingGroup ?? ''
}

function applyFilter(root: HTMLElement, state: ListingFilterState) {
    const cards = root.querySelectorAll<HTMLElement>('[data-listing-title]')
    let visibleCount = 0

    cards.forEach((card) => {
        const title = card.dataset.listingTitle ?? ''
        const group = cardGroupAttribute(card)
        const visible = cardIsVisible(title, group, state)
        card.style.display = visible ? '' : 'none'
        if (visible) visibleCount++
    })

    // Show / hide group sections based on whether any card in them is visible
    root.querySelectorAll<HTMLElement>('[data-group-key]').forEach(
        (groupEl) => {
            const visibleInGroup = Array.from(
                groupEl.querySelectorAll<HTMLElement>('[data-listing-title]')
            ).some((c) => c.style.display !== 'none')
            groupEl.style.display = visibleInGroup ? '' : 'none'
        }
    )

    const noResults = root.querySelector<HTMLElement>('.listing-no-results')
    if (noResults) {
        noResults.classList.toggle('hidden', visibleCount > 0)
    }
}

function updateChipStyles(
    allChip: HTMLButtonElement | null,
    groupChips: NodeListOf<HTMLButtonElement>,
    state: ListingFilterState
) {
    const noneSelected = state.activeGroups.size === 0

    if (allChip) {
        allChip.classList.toggle('listing-chip-active', noneSelected)
        allChip.classList.toggle('border-blue-elastic', noneSelected)
        allChip.classList.toggle('text-blue-elastic', noneSelected)
        allChip.classList.toggle('border-grey-20', !noneSelected)
        allChip.classList.toggle('text-ink-light', !noneSelected)
        allChip.setAttribute('aria-pressed', noneSelected ? 'true' : 'false')
    }

    groupChips.forEach((chip) => {
        const active = state.activeGroups.has(chip.dataset.group ?? '')
        chip.classList.toggle('listing-chip-active', active)
        chip.classList.toggle('border-blue-elastic', active)
        chip.classList.toggle('text-blue-elastic', active)
        chip.classList.toggle('border-grey-20', !active)
        chip.classList.toggle('text-ink-light', !active)
        chip.setAttribute('aria-pressed', active ? 'true' : 'false')
    })
}

function initListingRoot(root: HTMLElement) {
    const input = root.querySelector<HTMLInputElement>('.listing-filter-input')
    const allChip = root.querySelector<HTMLButtonElement>('.listing-chip-all')
    const groupChips = root.querySelectorAll<HTMLButtonElement>(
        '.listing-chip:not(.listing-chip-all)'
    )

    const state: ListingFilterState = { query: '', activeGroups: new Set() }

    input?.addEventListener('input', () => {
        state.query = input.value
        applyFilter(root, state)
    })

    allChip?.addEventListener('click', () => {
        state.activeGroups.clear()
        updateChipStyles(allChip, groupChips, state)
        applyFilter(root, state)
    })

    groupChips.forEach((chip) => {
        chip.addEventListener('click', (event) => {
            const group = chip.dataset.group ?? ''
            selectGroup(state, group, isMultiSelectModifier(event))
            updateChipStyles(allChip, groupChips, state)
            applyFilter(root, state)
        })
    })
}

function isMultiSelectModifier(event: MouseEvent): boolean {
    return event.metaKey || event.ctrlKey
}

function selectGroup(
    state: ListingFilterState,
    group: string,
    additive: boolean
) {
    if (additive) {
        if (state.activeGroups.has(group)) state.activeGroups.delete(group)
        else state.activeGroups.add(group)
        return
    }

    if (state.activeGroups.size === 1 && state.activeGroups.has(group)) {
        state.activeGroups.clear()
        return
    }

    state.activeGroups.clear()
    state.activeGroups.add(group)
}

export function initListing() {
    document
        .querySelectorAll<HTMLElement>('.listing-root')
        .forEach(initListingRoot)
}
