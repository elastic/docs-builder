// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Progressive-enhancement custom element for the `{table}` directive's
// `:filterable:` option. The server renders a normal, fully-usable table; when
// this module loads, it wraps that table with a search box and per-column facet
// dropdowns (auto-generated for low-cardinality columns) and shows/hides rows
// client-side. No framework is required, so there is no hydration cost and the
// table degrades gracefully when JavaScript is unavailable.

// Columns with at most this many distinct values become facet dropdowns.
const FACET_MAX_DISTINCT = 12

type Facet = { colIndex: number; select: HTMLSelectElement }

class FilterableTableElement extends HTMLElement {
    private table: HTMLTableElement | null = null
    private rows: HTMLTableRowElement[] = []
    private searchInput: HTMLInputElement | null = null
    private facets: Facet[] = []
    private status: HTMLElement | null = null

    connectedCallback(): void {
        // Guard against double-initialization (e.g. htmx re-scans).
        if (this.dataset.enhanced === 'true') return
        this.table = this.querySelector('table')
        const tbody = this.table?.tBodies[0]
        if (!this.table || !tbody) return
        this.rows = Array.from(tbody.rows)
        this.buildControls()
        this.applyFilters()
        this.dataset.enhanced = 'true'
    }

    private headerLabels(): string[] {
        const headRow = this.table?.tHead?.rows[0]
        return headRow
            ? Array.from(headRow.cells).map((c) => c.textContent?.trim() ?? '')
            : []
    }

    private distinctValues(colIndex: number): string[] {
        const values = new Set<string>()
        for (const row of this.rows) {
            const v = row.cells[colIndex]?.textContent?.trim() ?? ''
            if (v) values.add(v)
        }
        return Array.from(values).sort((a, b) => a.localeCompare(b))
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

        this.headerLabels().forEach((label, colIndex) => {
            const distinct = this.distinctValues(colIndex)
            // Only facet columns that partition the data meaningfully.
            if (
                distinct.length < 2 ||
                distinct.length > FACET_MAX_DISTINCT ||
                distinct.length >= this.rows.length
            )
                return

            const wrapper = document.createElement('label')
            wrapper.className = 'filterable-table-facet'
            wrapper.append(`${label}: `)

            const select = document.createElement('select')
            select.setAttribute('aria-label', `Filter by ${label}`)
            select.append(new Option('All', ''))
            for (const value of distinct)
                select.append(new Option(value, value))
            select.addEventListener('change', () => this.applyFilters())

            wrapper.appendChild(select)
            controls.appendChild(wrapper)
            this.facets.push({ colIndex, select })
        })

        const status = document.createElement('span')
        status.className = 'filterable-table-status'
        status.setAttribute('aria-live', 'polite')
        controls.appendChild(status)
        this.status = status

        this.insertBefore(controls, this.firstChild)
    }

    private applyFilters(): void {
        const query = this.searchInput?.value.trim().toLowerCase() ?? ''
        const activeFacets = this.facets
            .filter((f) => f.select.value !== '')
            .map((f) => ({ colIndex: f.colIndex, value: f.select.value }))

        let visible = 0
        for (const row of this.rows) {
            const matchesQuery =
                query === '' ||
                (row.textContent?.toLowerCase().includes(query) ?? false)
            const matchesFacets = activeFacets.every(
                (f) =>
                    (row.cells[f.colIndex]?.textContent?.trim() ?? '') ===
                    f.value
            )
            const show = matchesQuery && matchesFacets
            row.hidden = !show
            if (show) visible++
        }

        if (this.status)
            this.status.textContent = `Showing ${visible} of ${this.rows.length}`
    }
}

if (!customElements.get('filterable-table'))
    customElements.define('filterable-table', FilterableTableElement)

export { FilterableTableElement }
