import { cardIsVisible, initListing, ListingFilterState } from './listing'

// ---------------------------------------------------------------------------
// Pure predicate tests — no DOM needed
// ---------------------------------------------------------------------------

describe('cardIsVisible predicate', () => {
    const allGroups: ListingFilterState = { query: '', activeGroups: new Set() }

    it('shows every card when state is empty', () => {
        expect(cardIsVisible('rfc-0001 title', 'accepted', allGroups)).toBe(
            true
        )
    })

    it('matches query case-insensitively', () => {
        const state: ListingFilterState = {
            query: 'RFC',
            activeGroups: new Set(),
        }
        expect(cardIsVisible('rfc-0001 title', 'accepted', state)).toBe(true)
        expect(cardIsVisible('unrelated page', 'accepted', state)).toBe(false)
    })

    it('filters by a single active group', () => {
        const state: ListingFilterState = {
            query: '',
            activeGroups: new Set(['accepted']),
        }
        expect(cardIsVisible('any title', 'accepted', state)).toBe(true)
        expect(cardIsVisible('any title', 'draft', state)).toBe(false)
    })

    it('shows cards matching any of multiple active groups', () => {
        const state: ListingFilterState = {
            query: '',
            activeGroups: new Set(['accepted', 'draft']),
        }
        expect(cardIsVisible('any title', 'accepted', state)).toBe(true)
        expect(cardIsVisible('any title', 'draft', state)).toBe(true)
        expect(cardIsVisible('any title', 'deprecated', state)).toBe(false)
    })

    it('applies both query and group filter simultaneously', () => {
        const state: ListingFilterState = {
            query: 'search',
            activeGroups: new Set(['accepted']),
        }
        expect(cardIsVisible('search-something', 'accepted', state)).toBe(true)
        expect(cardIsVisible('unrelated', 'accepted', state)).toBe(false)
        expect(cardIsVisible('search-something', 'draft', state)).toBe(false)
    })

    it('shows ungrouped cards (empty group) when no groups are active', () => {
        expect(cardIsVisible('title', '', allGroups)).toBe(true)
    })

    it('hides ungrouped cards when specific groups are active', () => {
        const state: ListingFilterState = {
            query: '',
            activeGroups: new Set(['accepted']),
        }
        expect(cardIsVisible('title', '', state)).toBe(false)
    })
})

// ---------------------------------------------------------------------------
// DOM-based integration tests
// ---------------------------------------------------------------------------

function buildListingHtml() {
    return `
        <div class="listing-root" data-listing-total="4">
          <div class="listing-filter-bar">
            <input class="listing-filter-input" type="search" />
            <div class="listing-group-chips">
              <button class="listing-chip listing-chip-all listing-chip-active" data-group="">All</button>
              <span>|</span>
              <button class="listing-chip" data-group="accepted">Accepted</button>
              <button class="listing-chip" data-group="draft">Draft</button>
            </div>
          </div>
          <div class="listing-group" data-group-key="accepted">
            <h2 class="listing-group-heading" id="accepted">Accepted</h2>
            <a class="page-card" href="/rfcs/001" data-listing-group="accepted" data-listing-title="first rfc about search">First RFC about Search</a>
            <a class="page-card" href="/rfcs/002" data-listing-group="accepted" data-listing-title="second rfc about indexing">Second RFC about Indexing</a>
          </div>
          <div class="listing-group" data-group-key="draft">
            <h2 class="listing-group-heading" id="draft">Draft</h2>
            <a class="page-card" href="/rfcs/003" data-listing-group="draft" data-listing-title="draft rfc about storage">Draft RFC about Storage</a>
          </div>
          <div class="listing-group" data-group-key="">
            <a class="page-card" href="/rfcs/004" data-listing-group="" data-listing-title="ungrouped rfc">Ungrouped RFC</a>
          </div>
          <p class="listing-no-results hidden">No pages match your filter.</p>
        </div>
    `
}

function cards(doc = document) {
    return Array.from(
        doc.querySelectorAll<HTMLAnchorElement>('a[data-listing-title]')
    )
}

function visibleCards(doc = document) {
    return cards(doc).filter((c) => c.style.display !== 'none')
}

describe('initListing DOM behaviour', () => {
    beforeEach(() => {
        document.body.innerHTML = buildListingHtml()
        initListing()
    })

    it('shows all cards on init', () => {
        expect(visibleCards()).toHaveLength(4)
    })

    it('filters cards by text input', () => {
        const input = document.querySelector<HTMLInputElement>(
            '.listing-filter-input'
        )!
        input.value = 'search'
        input.dispatchEvent(new Event('input'))

        const visible = visibleCards()
        expect(visible).toHaveLength(1)
        expect(visible[0].dataset.listingTitle).toContain('search')
    })

    it('hides group section when all its cards are filtered out by text', () => {
        const input = document.querySelector<HTMLInputElement>(
            '.listing-filter-input'
        )!
        input.value = 'storage'
        input.dispatchEvent(new Event('input'))

        const acceptedGroup = document.querySelector<HTMLElement>(
            '[data-group-key="accepted"]'
        )!
        expect(acceptedGroup.style.display).toBe('none')
    })

    it('toggles a single group chip on click', () => {
        const draftChip = document.querySelector<HTMLButtonElement>(
            '[data-group="draft"]'
        )!
        draftChip.click()

        const visible = visibleCards()
        expect(visible).toHaveLength(1)
        expect(visible[0].dataset.listingGroup).toBe('draft')
    })

    it('allows selecting multiple group chips independently', () => {
        const acceptedChip = document.querySelector<HTMLButtonElement>(
            '[data-group="accepted"]'
        )!
        const draftChip = document.querySelector<HTMLButtonElement>(
            '[data-group="draft"]'
        )!
        acceptedChip.click()
        draftChip.click()

        // Both groups visible, ungrouped hidden
        const visible = visibleCards()
        expect(visible).toHaveLength(3)
        expect(visible.every((c) => c.dataset.listingGroup !== '')).toBe(true)
    })

    it('deselects a chip by clicking it again', () => {
        const draftChip = document.querySelector<HTMLButtonElement>(
            '[data-group="draft"]'
        )!
        draftChip.click() // select
        draftChip.click() // deselect → back to all

        expect(visibleCards()).toHaveLength(4)
    })

    it('All chip clears all active groups', () => {
        const draftChip = document.querySelector<HTMLButtonElement>(
            '[data-group="draft"]'
        )!
        draftChip.click()

        const allChip =
            document.querySelector<HTMLButtonElement>('.listing-chip-all')!
        allChip.click()

        expect(visibleCards()).toHaveLength(4)
    })

    it('All chip becomes active when no groups are selected', () => {
        const draftChip = document.querySelector<HTMLButtonElement>(
            '[data-group="draft"]'
        )!
        draftChip.click()

        const allChip =
            document.querySelector<HTMLButtonElement>('.listing-chip-all')!
        allChip.click()

        expect(allChip.classList.contains('listing-chip-active')).toBe(true)
    })

    it('All chip loses active state when a group chip is selected', () => {
        const draftChip = document.querySelector<HTMLButtonElement>(
            '[data-group="draft"]'
        )!
        draftChip.click()

        const allChip =
            document.querySelector<HTMLButtonElement>('.listing-chip-all')!
        expect(allChip.classList.contains('listing-chip-active')).toBe(false)
    })

    it('shows no-results paragraph when nothing matches', () => {
        const input = document.querySelector<HTMLInputElement>(
            '.listing-filter-input'
        )!
        input.value = 'zzznomatch'
        input.dispatchEvent(new Event('input'))

        const noResults = document.querySelector<HTMLElement>(
            '.listing-no-results'
        )!
        expect(noResults.classList.contains('hidden')).toBe(false)
    })

    it('hides no-results paragraph when something matches', () => {
        const input = document.querySelector<HTMLInputElement>(
            '.listing-filter-input'
        )!
        input.value = 'rfc'
        input.dispatchEvent(new Event('input'))

        const noResults = document.querySelector<HTMLElement>(
            '.listing-no-results'
        )!
        expect(noResults.classList.contains('hidden')).toBe(true)
    })
})
