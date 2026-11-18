// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Progressive-enhancement custom element for the `{table}` directive's
// `:filterable:` option. The server renders a normal, fully-usable table; when
// this module loads, it wraps that table with a search box and per-column
// dropdown filters and shows/hides rows client-side. The dropdowns are
// auto-generated for low-cardinality columns unless the author chose them:
// `:filterable: search` turns them off and `:filter-columns:` names them. No framework is required, so there is no hydration cost and
// the table degrades gracefully when JavaScript is unavailable.

// Columns with at most this many distinct values become dropdown filters.
const FACET_MAX_DISTINCT = 12

// Sibling web components render their own text lazily - an `applies_to` badge
// column is empty markup until AppliesToPopover loads - so the values this
// element reads on upgrade can still be blank. Re-derive the dropdowns once the
// table has stopped changing for this long.
const SETTLE_MS = 150

// ...and stop watching after this, so later reader interaction (badge popovers,
// htmx swaps) can never trigger a rebuild while someone is using the filters.
const WATCH_CEILING_MS = 5000

// Gives each dropdown panel a unique id for its button's aria-controls.
let facetListIds = 0

// The Filters button: a funnel icon, the label, a count of the filters that
// are on (empty when none), and a chevron that flips when the panel opens.
// Static markup, with no reader or author text in it.
const TOGGLE_CONTENT = `<svg class="filterable-table-toggle-icon" aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path stroke-linecap="round" stroke-linejoin="round" d="M12 3c2.755 0 5.455.232 8.083.678.533.09.917.556.917 1.096v1.044a2.25 2.25 0 0 1-.659 1.591l-5.432 5.432a2.25 2.25 0 0 0-.659 1.591v2.927a2.25 2.25 0 0 1-1.244 2.013L9.75 21v-6.568a2.25 2.25 0 0 0-.659-1.591L3.659 7.409A2.25 2.25 0 0 1 3 5.818V4.774c0-.54.384-1.006.917-1.096A48.32 48.32 0 0 1 12 3Z"/></svg><span>Filters</span><span class="filterable-table-toggle-count"></span><svg class="filterable-table-toggle-chevron" aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5"/></svg>`

type Facet = { colIndex: number; select: HTMLSelectElement }
type FacetSpec = { colIndex: number; label: string; values: string[] }

class FilterableTableElement extends HTMLElement {
    private table: HTMLTableElement | null = null
    private rows: HTMLTableRowElement[] = []
    private allRows: HTMLTableRowElement[] = []
    private sectionRows: Set<HTMLTableRowElement> = new Set()
    private searchInput: HTMLInputElement | null = null
    private facets: Facet[] = []
    private status: HTMLElement | null = null
    private controls: HTMLElement | null = null
    private facetList: HTMLElement | null = null
    private clearButton: HTMLButtonElement | null = null
    private emptyMessage: HTMLElement | null = null
    private toggleButton: HTMLButtonElement | null = null
    private facetSignature = ''
    private observer: MutationObserver | null = null
    private settleTimer = 0

    connectedCallback(): void {
        // Guard against double-initialization (e.g. htmx re-scans).
        if (this.dataset.enhanced === 'true') return
        this.table = this.querySelector('table')
        const tbody = this.table?.tBodies[0]
        if (!this.table || !tbody) return
        this.readRows(tbody)
        // An empty body has nothing to filter; leave the server markup as-is
        // rather than show controls that can do nothing.
        if (this.rows.length === 0) return
        this.buildControls()
        this.applyFilters()
        this.watchLateContent(tbody)
        this.dataset.enhanced = 'true'
    }

    disconnectedCallback(): void {
        window.clearTimeout(this.settleTimer)
        this.stopWatching()
    }

    private headerLabels(): string[] {
        const headRow = this.table?.tHead?.rows[0]
        return headRow
            ? Array.from(headRow.cells).map((c) => c.textContent?.trim() ?? '')
            : []
    }

    /**
     * Splits the body into data rows and section-header rows. A grouped
     * reference table (for example the EDOT components list) uses a row whose
     * first cell holds a group name and whose other cells are empty as a
     * heading. Those rows are structure, not data: they must not inflate the
     * count or be matched by a filter.
     */
    private readRows(tbody: HTMLTableSectionElement): void {
        this.allRows = Array.from(tbody.rows)
        const candidates = this.allRows.filter((r) => this.isSectionRow(r))
        // If every row looks like a heading, none of them is: the data columns
        // are probably still empty because sibling components (for example
        // `applies_to` badges) render them late. The observer re-reads once
        // they fill, and the real headings separate out then.
        this.sectionRows =
            candidates.length < this.allRows.length
                ? new Set(candidates)
                : new Set()
        this.rows = this.allRows.filter((r) => !this.sectionRows.has(r))
    }

    private isSectionRow(row: HTMLTableRowElement): boolean {
        const cells = Array.from(row.cells)
        if (cells.length < 2) return false
        const [first, ...rest] = cells
        const heading = this.cellText(first)
        // A heading fills only its first cell; every other cell is empty.
        if (heading === '' || rest.some((c) => this.cellText(c) !== ''))
            return false
        // The heading must be emphasized, the convention for a section row in a
        // Markdown table (`|***Receivers***|||||`). A plain data row whose
        // trailing cells happen to be blank is then never mistaken for a
        // heading. Erring toward "data" is the safe direction: mislabeling a
        // heading as data only miscounts by one, while the reverse would drop a
        // real row from the count and hide it under a filter.
        const emphasis = first.querySelector('strong, em, b, i')
        return emphasis?.textContent?.trim() === heading
    }

    /**
     * A cell's text with footnote markers removed. A `[^1]` reference renders
     * as `<a class="footnote-ref">`, whose digit is part of `textContent` but
     * not part of the value - without this a footnoted "Core" reads as
     * "Core 1" and splits off into its own dropdown option. A footnote nested
     * inside other markup (`**Core[^1]**`) is skipped at any depth.
     */
    private cellText(cell: Element | undefined): string {
        if (!cell) return ''
        let text = ''
        const collect = (node: Node): void => {
            if (node.nodeType === Node.TEXT_NODE) {
                text += node.textContent ?? ''
                return
            }
            if (
                node.nodeType === Node.ELEMENT_NODE &&
                (node as Element).classList.contains('footnote-ref')
            )
                return
            for (const child of Array.from(node.childNodes)) collect(child)
        }
        collect(cell)
        return text.trim()
    }

    /**
     * The values a cell contributes to its column's filter. A cell that lists
     * several comma-separated values ("Logs, Metrics") contributes each one, so
     * the row matches a filter for any of them rather than only the exact text.
     */
    private cellValues(cell: Element | undefined): string[] {
        const text = this.cellText(cell)
        if (!text) return []
        return text
            .split(',')
            .map((v) => v.trim())
            .filter(Boolean)
    }

    private columnValues(colIndex: number): string[][] {
        return this.rows.map((row) => this.cellValues(row.cells[colIndex]))
    }

    /**
     * The zero-based positions of the columns the author chose for dropdowns,
     * or null when the choice is automatic. `:filterable: search` renders
     * `data-facets="none"` (an empty choice). `:filter-columns:` names columns
     * by header text, and the build matches those names and renders the
     * positions in `data-facet-columns`, so header text is never compared here.
     */
    private chosenColumns(): Set<number> | null {
        if (this.dataset.facets === 'none') return new Set()
        const listed = this.dataset.facetColumns
        if (listed === undefined) return null
        return new Set(
            listed
                .split(',')
                .map((position) => Number.parseInt(position.trim(), 10))
                .filter((position) => Number.isInteger(position))
        )
    }

    /** The dropdowns the current table content warrants, in column order. */
    private facetSpecs(): FacetSpec[] {
        const specs: FacetSpec[] = []
        const chosen = this.chosenColumns()
        this.headerLabels().forEach((label, colIndex) => {
            // A blank header leaves the dropdown with no label and no
            // accessible name, so skip the column: a nameless filter is worse
            // than none.
            if (!label) return
            // The author's choice replaces the heuristic below, so a column of
            // scores never gets a dropdown just because it has few values.
            if (chosen && !chosen.has(colIndex)) return
            const perRow = this.columnValues(colIndex)
            const values = Array.from(new Set(perRow.flat())).sort((a, b) =>
                a.localeCompare(b)
            )
            // A key column holds one unique value per row, so a dropdown of it
            // would never group anything. A multi-value column is never a key
            // column, even when its token count reaches the row count, because
            // its values are shared across rows.
            const isKeyColumn =
                perRow.every((v) => v.length === 1) &&
                values.length === this.rows.length
            // A column with one value can't narrow anything, chosen or not.
            if (values.length < 2) return
            // Left to the heuristic, only offer a dropdown for columns that
            // partition the data meaningfully: few enough values to scan, and
            // not a key column. A column the author chose skips both checks.
            if (!chosen && (values.length > FACET_MAX_DISTINCT || isKeyColumn))
                return
            specs.push({ colIndex, label, values })
        })
        return specs
    }

    /**
     * Lays the controls out in rows: the search box with a "Filters" button,
     * the dropdowns in a grid with their labels above them (collapsed until
     * the button opens them), and the count with a "Clear" button that shows
     * only while something is filtered. A message below the table says so
     * when nothing matches.
     */
    private buildControls(): void {
        const controls = document.createElement('div')
        controls.className = 'filterable-table-controls'

        const search = document.createElement('input')
        search.type = 'search'
        search.placeholder = 'Filter table…'
        search.className = 'filterable-table-search'
        search.setAttribute('aria-label', 'Filter table')
        search.addEventListener('input', () => this.applyFilters())
        this.searchInput = search

        // The dropdowns start collapsed behind a "Filters" button beside the
        // search box. The button carries a count of the filters that are on,
        // so readers can tell the table is narrowed even with the panel shut.
        const facetList = document.createElement('div')
        facetList.className = 'filterable-table-facets'
        facetList.id = `filterable-table-facets-${++facetListIds}`
        facetList.hidden = true
        this.facetList = facetList

        const toggle = document.createElement('button')
        toggle.type = 'button'
        toggle.className = 'filterable-table-toggle'
        toggle.setAttribute('aria-expanded', 'false')
        toggle.setAttribute('aria-controls', facetList.id)
        toggle.innerHTML = TOGGLE_CONTENT
        toggle.addEventListener('click', () =>
            this.showFilters(facetList.hidden)
        )
        this.toggleButton = toggle

        const searchRow = document.createElement('div')
        searchRow.className = 'filterable-table-search-row'
        searchRow.append(search, toggle)
        controls.append(searchRow, facetList)

        const summary = document.createElement('div')
        summary.className = 'filterable-table-summary'

        const status = document.createElement('span')
        status.className = 'filterable-table-status'
        status.setAttribute('role', 'status')
        status.setAttribute('aria-live', 'polite')
        summary.appendChild(status)
        this.status = status

        const clear = document.createElement('button')
        clear.type = 'button'
        clear.className = 'filterable-table-clear'
        clear.textContent = 'Clear'
        clear.hidden = true
        clear.addEventListener('click', () => this.clearFilters())
        summary.appendChild(clear)
        this.clearButton = clear

        controls.appendChild(summary)
        this.controls = controls
        this.insertBefore(controls, this.firstChild)

        // Shown under the header row when nothing matches, so an empty table
        // doesn't look broken. It isn't a live region: the count above it
        // already announces "Showing 0 of N". A fullscreen copy is cloned
        // from an enhanced host, so drop any message the clone carried over.
        this.querySelector(':scope > .filterable-table-empty')?.remove()
        // A div, not a p: the page's paragraph styles would add a top margin.
        const empty = document.createElement('div')
        empty.className = 'filterable-table-empty'
        empty.hidden = true
        this.appendChild(empty)
        this.emptyMessage = empty

        this.renderFacets()
    }

    /** Opens or closes the dropdown panel. */
    private showFilters(open: boolean): void {
        if (!this.facetList || !this.toggleButton) return
        this.facetList.hidden = !open
        this.toggleButton.setAttribute('aria-expanded', String(open))
    }

    /** Empties the search box, sets every dropdown back to All, and returns focus to search. */
    private clearFilters(): void {
        if (this.searchInput) this.searchInput.value = ''
        for (const facet of this.facets) facet.select.value = ''
        this.applyFilters()
        this.searchInput?.focus()
    }

    /**
     * Builds the dropdowns, or rebuilds them when late-rendering content has
     * changed what the columns contain. A no-op when nothing changed, so
     * incidental DOM churn costs nothing.
     */
    private renderFacets(): void {
        const facetList = this.facetList
        if (!facetList) return

        const specs = this.facetSpecs()
        const signature = JSON.stringify(specs)
        if (signature === this.facetSignature) return
        this.facetSignature = signature

        // Carry over what the reader had chosen, where it still exists.
        const selected = new Map(
            this.facets.map((f) => [f.colIndex, f.select.value])
        )
        for (const facet of this.facets) facet.select.closest('label')?.remove()
        this.facets = []

        for (const spec of specs) {
            const wrapper = document.createElement('label')
            wrapper.className = 'filterable-table-facet'
            const name = document.createElement('span')
            name.className = 'filterable-table-facet-label'
            name.textContent = spec.label
            wrapper.appendChild(name)

            const select = document.createElement('select')
            select.setAttribute('aria-label', `Filter by ${spec.label}`)
            select.append(new Option('All', ''))
            for (const value of spec.values)
                select.append(new Option(value, value))

            const previous = selected.get(spec.colIndex)
            if (previous && spec.values.includes(previous))
                select.value = previous
            select.addEventListener('change', () => this.applyFilters())

            wrapper.appendChild(select)
            facetList.appendChild(wrapper)
            this.facets.push({ colIndex: spec.colIndex, select })
        }

        // No dropdowns, no button: a search-only table shows just the search box.
        if (this.toggleButton)
            this.toggleButton.hidden = this.facets.length === 0
        if (this.facets.length === 0) this.showFilters(false)
    }

    private watchLateContent(tbody: HTMLTableSectionElement): void {
        if (typeof MutationObserver === 'undefined') return
        this.observer = new MutationObserver(() => {
            window.clearTimeout(this.settleTimer)
            this.settleTimer = window.setTimeout(() => {
                const body = this.table?.tBodies[0]
                if (body) this.readRows(body)
                this.renderFacets()
                this.applyFilters()
            }, SETTLE_MS)
        })
        // Attributes are deliberately not observed: applyFilters() toggles
        // `hidden` on rows, which would otherwise re-enter this callback.
        this.observer.observe(tbody, {
            childList: true,
            characterData: true,
            subtree: true,
        })
        window.setTimeout(() => this.stopWatching(), WATCH_CEILING_MS)
    }

    private stopWatching(): void {
        this.observer?.disconnect()
        this.observer = null
    }

    private applyFilters(): void {
        const query = this.searchInput?.value.trim().toLowerCase() ?? ''
        const activeFacets = this.facets
            .filter((f) => f.select.value !== '')
            .map((f) => ({ colIndex: f.colIndex, value: f.select.value }))

        let visible = 0
        // A section header is shown only while its group has a visible row, so
        // a filter never strands a heading above nothing.
        let section: HTMLTableRowElement | null = null
        let sectionHasVisible = false
        const closeSection = () => {
            if (section) section.hidden = !sectionHasVisible
        }

        for (const row of this.allRows) {
            if (this.sectionRows.has(row)) {
                closeSection()
                section = row
                sectionHasVisible = false
                continue
            }
            const matchesQuery =
                query === '' ||
                (row.textContent?.toLowerCase().includes(query) ?? false)
            const matchesFacets = activeFacets.every((f) =>
                this.cellValues(row.cells[f.colIndex]).includes(f.value)
            )
            const show = matchesQuery && matchesFacets
            row.hidden = !show
            if (show) {
                visible++
                sectionHasVisible = true
            }
        }
        closeSection()

        // Drives the print rule: a printed copy of a filtered table keeps its
        // count, so it can't be mistaken for the complete table.
        this.dataset.filtered = String(visible < this.rows.length)

        // Mark the dropdowns that narrow the table, and offer a way to undo
        // everything only while something is set.
        for (const facet of this.facets)
            facet.select
                .closest('label')
                ?.toggleAttribute('data-active', facet.select.value !== '')
        if (this.clearButton)
            this.clearButton.hidden = query === '' && activeFacets.length === 0

        // Count the filters that are on in the Filters button, visibly and in
        // its accessible name.
        if (this.toggleButton) {
            const count = this.toggleButton.querySelector(
                '.filterable-table-toggle-count'
            )
            if (count)
                count.textContent =
                    activeFacets.length > 0 ? String(activeFacets.length) : ''
            this.toggleButton.setAttribute(
                'aria-label',
                activeFacets.length > 0
                    ? `Filters, ${activeFacets.length} on`
                    : 'Filters'
            )
        }

        // Say why the table is empty, naming what's narrowing it.
        const none = visible === 0
        this.toggleAttribute('data-empty', none)
        if (this.emptyMessage) {
            this.emptyMessage.hidden = !none
            if (none)
                this.emptyMessage.textContent =
                    query !== '' && activeFacets.length > 0
                        ? 'No rows match your search and filters.'
                        : query !== ''
                          ? 'No rows match your search.'
                          : 'No rows match your filters.'
        }

        const text = `Showing ${visible} of ${this.rows.length}`
        // Only touch the live region when the count really changed, so typing
        // doesn't queue one announcement per keystroke.
        if (this.status && this.status.textContent !== text)
            this.status.textContent = text
    }
}

if (!customElements.get('filterable-table'))
    customElements.define('filterable-table', FilterableTableElement)

export { FilterableTableElement }
