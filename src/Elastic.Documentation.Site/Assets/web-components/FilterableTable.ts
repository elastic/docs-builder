// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Progressive-enhancement custom element for the `{table}` directive's
// `:filterable:` option. The server renders a normal, fully-usable table; when
// this module loads, it wraps that table with a search box and per-column
// dropdown filters (auto-generated for low-cardinality columns) and shows/hides
// rows client-side. No framework is required, so there is no hydration cost and
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
        // A heading fills only its first cell; every other cell is empty.
        return (
            this.cellText(cells[0]) !== '' &&
            cells.slice(1).every((c) => this.cellText(c) === '')
        )
    }

    /**
     * A cell's text with footnote markers removed. A `[^1]` reference renders
     * as `<a class="footnote-ref">`, whose digit is part of `textContent` but
     * not part of the value - without this a footnoted "Core" reads as
     * "Core 1" and splits off into its own dropdown option.
     */
    private cellText(cell: Element | undefined): string {
        if (!cell) return ''
        let text = ''
        for (const node of Array.from(cell.childNodes)) {
            if (
                node.nodeType === Node.ELEMENT_NODE &&
                (node as Element).classList.contains('footnote-ref')
            )
                continue
            text += node.textContent ?? ''
        }
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

    /** The dropdowns the current table content warrants, in column order. */
    private facetSpecs(): FacetSpec[] {
        const specs: FacetSpec[] = []
        this.headerLabels().forEach((label, colIndex) => {
            // A blank header leaves the dropdown with no label and no
            // accessible name, so skip the column: a nameless filter is worse
            // than none.
            if (!label) return
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
            // Only offer a dropdown for columns that partition the data
            // meaningfully: at least two values, few enough to scan, and not a
            // key column.
            if (
                values.length < 2 ||
                values.length > FACET_MAX_DISTINCT ||
                isKeyColumn
            )
                return
            specs.push({ colIndex, label, values })
        })
        return specs
    }

    private buildControls(): void {
        const controls = document.createElement('div')
        controls.className = 'filterable-table-controls'

        const search = document.createElement('input')
        search.type = 'search'
        search.placeholder = 'Filter table…'
        search.className = 'filterable-table-search'
        search.setAttribute('aria-label', 'Filter table')
        search.addEventListener('input', () => this.applyFilters())
        controls.appendChild(search)
        this.searchInput = search

        const status = document.createElement('span')
        status.className = 'filterable-table-status'
        status.setAttribute('role', 'status')
        status.setAttribute('aria-live', 'polite')
        controls.appendChild(status)
        this.status = status

        this.controls = controls
        this.insertBefore(controls, this.firstChild)
        this.renderFacets()
    }

    /**
     * Builds the dropdowns, or rebuilds them when late-rendering content has
     * changed what the columns contain. A no-op when nothing changed, so
     * incidental DOM churn costs nothing.
     */
    private renderFacets(): void {
        const controls = this.controls
        const status = this.status
        if (!controls || !status) return

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
            wrapper.append(`${spec.label}: `)

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
            // Keep the count last in the control row.
            controls.insertBefore(wrapper, status)
            this.facets.push({ colIndex: spec.colIndex, select })
        }
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
