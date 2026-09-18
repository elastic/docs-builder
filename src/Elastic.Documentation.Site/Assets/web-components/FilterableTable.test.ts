import { FilterableTableElement } from './FilterableTable'

// The module registers <filterable-table> on import.
const TABLE_HTML = `
  <filterable-table>
    <div class="table-wrapper">
      <table>
        <thead><tr><th>Component</th><th>Support status</th></tr></thead>
        <tbody>
          <tr><td>filelogreceiver</td><td>Core</td></tr>
          <tr><td>apachereceiver</td><td>Extended</td></tr>
          <tr><td>k8sclusterreceiver</td><td>Core</td></tr>
          <tr><td>hostmetricsreceiver</td><td>Core</td></tr>
        </tbody>
      </table>
    </div>
  </filterable-table>`

function mount(): void {
    document.body.innerHTML = TABLE_HTML
    // Ensure the custom element is upgraded + connectedCallback ran.
    customElements.upgrade(document.body)
}

const dataRows = () =>
    Array.from(document.querySelectorAll<HTMLTableRowElement>('tbody tr'))
const visibleRows = () => dataRows().filter((r) => !r.hidden)

describe('filterable-table', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
    })

    it('is registered as a custom element', () => {
        expect(customElements.get('filterable-table')).toBe(
            FilterableTableElement
        )
    })

    it('injects a search box and a status region', () => {
        mount()
        expect(
            document.querySelector('.filterable-table-search')
        ).not.toBeNull()
        expect(
            document.querySelector('.filterable-table-status')?.textContent
        ).toBe('Showing 4 of 4')
    })

    it('auto-generates a facet only for the low-cardinality column', () => {
        mount()
        const facets = document.querySelectorAll('.filterable-table-facet')
        // "Support status" (Core/Extended) qualifies; "Component" (all unique) does not.
        expect(facets).toHaveLength(1)
        expect(facets[0].textContent).toContain('Support status')
    })

    it('filters rows by free-text search across all columns', () => {
        mount()
        const search = document.querySelector<HTMLInputElement>(
            '.filterable-table-search'
        )!
        search.value = 'apache'
        search.dispatchEvent(new Event('input'))
        expect(visibleRows()).toHaveLength(1)
        expect(visibleRows()[0].textContent).toContain('apachereceiver')
        expect(
            document.querySelector('.filterable-table-status')?.textContent
        ).toBe('Showing 1 of 4')
    })

    it('filters rows by facet selection', () => {
        mount()
        const select = document.querySelector<HTMLSelectElement>(
            '.filterable-table-facet select'
        )!
        select.value = 'Core'
        select.dispatchEvent(new Event('change'))
        expect(visibleRows()).toHaveLength(3)
        expect(
            visibleRows().every((r) => r.textContent?.includes('Core'))
        ).toBe(true)
    })

    it('combines search and facet (AND semantics)', () => {
        mount()
        const search = document.querySelector<HTMLInputElement>(
            '.filterable-table-search'
        )!
        const select = document.querySelector<HTMLSelectElement>(
            '.filterable-table-facet select'
        )!
        select.value = 'Core'
        select.dispatchEvent(new Event('change'))
        search.value = 'host'
        search.dispatchEvent(new Event('input'))
        expect(visibleRows()).toHaveLength(1)
        expect(visibleRows()[0].textContent).toContain('hostmetricsreceiver')
    })
})
