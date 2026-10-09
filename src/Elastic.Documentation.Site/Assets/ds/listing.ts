// Filters a listing of page cards by text and by group chips. The filter bar is rendered hidden and revealed here, because the cards are
// ordinary links without it. A plain click selects one group; Cmd or Ctrl click adds or removes groups.
import { cardIsVisible } from '../listing'
import type { ListingFilterState } from '../listing'

export function initListings(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>(
            '[data-listing]:not([data-listing-bound])'
        )
        .forEach((root) => {
            root.dataset.listingBound = ''
            bindListing(root)
        })
}

function bindListing(root: HTMLElement) {
    const bar = root.querySelector<HTMLElement>('[data-listing-bar]')
    const input = root.querySelector<HTMLInputElement>('[data-listing-filter]')
    const chips = Array.from(
        root.querySelectorAll<HTMLButtonElement>('[data-listing-chip]')
    )
    const state: ListingFilterState = { query: '', activeGroups: new Set() }

    function render() {
        const cards = root.querySelectorAll<HTMLElement>('[data-listing-card]')
        let visible = 0
        cards.forEach((card) => {
            const show = cardIsVisible(
                card.dataset.listingTitle ?? '',
                card.dataset.listingGroup ?? '',
                state
            )
            card.hidden = !show
            if (show) visible++
        })
        root.querySelectorAll<HTMLElement>('[data-listing-section]').forEach(
            (section) => {
                section.hidden = !Array.from(
                    section.querySelectorAll<HTMLElement>('[data-listing-card]')
                ).some((card) => !card.hidden)
            }
        )
        const empty = root.querySelector<HTMLElement>('[data-listing-empty]')
        if (empty) empty.hidden = visible > 0

        chips.forEach((chip) => {
            const group = chip.dataset.listingChip ?? ''
            const pressed =
                group === ''
                    ? state.activeGroups.size === 0
                    : state.activeGroups.has(group)
            chip.setAttribute('aria-pressed', String(pressed))
        })
    }

    input?.addEventListener('input', () => {
        state.query = input.value
        render()
    })
    chips.forEach((chip) =>
        chip.addEventListener('click', (event) => {
            const group = chip.dataset.listingChip ?? ''
            if (group === '') state.activeGroups.clear()
            else if (event.metaKey || event.ctrlKey) {
                if (!state.activeGroups.delete(group))
                    state.activeGroups.add(group)
            } else if (
                state.activeGroups.size === 1 &&
                state.activeGroups.has(group)
            ) {
                state.activeGroups.clear()
            } else {
                state.activeGroups.clear()
                state.activeGroups.add(group)
            }
            render()
        })
    )

    if (bar) bar.hidden = false
    render()
}
