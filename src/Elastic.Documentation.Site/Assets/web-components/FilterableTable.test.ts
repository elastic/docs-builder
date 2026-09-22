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

    it('matches search case-insensitively', () => {
        mount()
        const search = document.querySelector<HTMLInputElement>(
            '.filterable-table-search'
        )!
        search.value = 'APACHE'
        search.dispatchEvent(new Event('input'))
        expect(visibleRows()).toHaveLength(1)
        expect(visibleRows()[0].textContent).toContain('apachereceiver')
    })

    it('restores every row when a facet returns to All', () => {
        mount()
        const select = document.querySelector<HTMLSelectElement>(
            '.filterable-table-facet select'
        )!
        select.value = 'Core'
        select.dispatchEvent(new Event('change'))
        expect(visibleRows()).toHaveLength(3)

        select.value = ''
        select.dispatchEvent(new Event('change'))
        expect(visibleRows()).toHaveLength(4)
        expect(
            document.querySelector('.filterable-table-status')?.textContent
        ).toBe('Showing 4 of 4')
    })

    it('flags the host only while a filter hides rows', () => {
        mount()
        const host = document.querySelector<HTMLElement>('filterable-table')!
        expect(host.dataset.filtered).toBe('false')

        const search = document.querySelector<HTMLInputElement>(
            '.filterable-table-search'
        )!
        search.value = 'apache'
        search.dispatchEvent(new Event('input'))
        expect(host.dataset.filtered).toBe('true')

        search.value = ''
        search.dispatchEvent(new Event('input'))
        expect(host.dataset.filtered).toBe('false')
    })

    it('leaves the live region untouched when the count is unchanged', () => {
        mount()
        const status = document.querySelector('.filterable-table-status')!
        const search = document.querySelector<HTMLInputElement>(
            '.filterable-table-search'
        )!
        // The three Core rows; the Extended row has no "cor" anywhere.
        search.value = 'cor'
        search.dispatchEvent(new Event('input'))
        expect(status.textContent).toBe('Showing 3 of 4')

        const writes: string[] = []
        new MutationObserver(() =>
            writes.push(status.textContent ?? '')
        ).observe(status, {
            childList: true,
            characterData: true,
            subtree: true,
        })

        // Still the same three matches, so the announcement must not requeue.
        search.value = 'core'
        search.dispatchEvent(new Event('input'))
        expect(status.textContent).toBe('Showing 3 of 4')
        expect(writes).toHaveLength(0)
    })

    it('exposes the count as a status region', () => {
        mount()
        const status = document.querySelector('.filterable-table-status')!
        expect(status.getAttribute('role')).toBe('status')
        expect(status.getAttribute('aria-live')).toBe('polite')
    })

    it('enhances a table with no thead, without offering dropdowns', () => {
        document.body.innerHTML = `
          <filterable-table>
            <table><tbody>
              <tr><td>a</td><td>Core</td></tr>
              <tr><td>b</td><td>Extended</td></tr>
              <tr><td>c</td><td>Core</td></tr>
            </tbody></table>
          </filterable-table>`
        customElements.upgrade(document.body)
        expect(
            document.querySelector('.filterable-table-search')
        ).not.toBeNull()
        expect(
            document.querySelectorAll('.filterable-table-facet')
        ).toHaveLength(0)
        expect(
            document.querySelector('.filterable-table-status')?.textContent
        ).toBe('Showing 3 of 3')
    })

    it('leaves a table with no body rows alone', () => {
        document.body.innerHTML = `
          <filterable-table>
            <table><thead><tr><th>A</th><th>B</th></tr></thead></table>
          </filterable-table>`
        customElements.upgrade(document.body)
        expect(document.querySelector('.filterable-table-controls')).toBeNull()
        expect(
            document.querySelector<HTMLElement>('filterable-table')!.dataset
                .enhanced
        ).toBeUndefined()
    })

    it('does not re-initialize an already enhanced host', () => {
        mount()
        const host = document.querySelector<HTMLElement>('filterable-table')!
        host.remove()
        document.body.appendChild(host)
        expect(
            document.querySelectorAll('.filterable-table-controls')
        ).toHaveLength(1)
    })

    it('omits a dropdown when a column has one distinct value', () => {
        document.body.innerHTML = `
          <filterable-table>
            <table>
              <thead><tr><th>Name</th><th>Type</th></tr></thead>
              <tbody>
                <tr><td>a</td><td>Receiver</td></tr>
                <tr><td>b</td><td>Receiver</td></tr>
                <tr><td>c</td><td>Receiver</td></tr>
              </tbody>
            </table>
          </filterable-table>`
        customElements.upgrade(document.body)
        expect(
            document.querySelectorAll('.filterable-table-facet')
        ).toHaveLength(0)
    })

    it('omits a dropdown when a column has one distinct value per row', () => {
        // Two rows, two distinct types: a key column, not a category.
        document.body.innerHTML = `
          <filterable-table>
            <table>
              <thead><tr><th>Name</th><th>Type</th></tr></thead>
              <tbody>
                <tr><td>a</td><td>Receiver</td></tr>
                <tr><td>b</td><td>Exporter</td></tr>
              </tbody>
            </table>
          </filterable-table>`
        customElements.upgrade(document.body)
        expect(
            document.querySelectorAll('.filterable-table-facet')
        ).toHaveLength(0)
    })

    it('adds a dropdown once a lazily rendered column fills in', async () => {
        // Reproduces the applies_to badge case: the cell is empty markup when
        // this element upgrades, and another component renders into it later.
        document.body.innerHTML = `
          <filterable-table>
            <table>
              <thead><tr><th>Name</th><th>Availability</th></tr></thead>
              <tbody>
                <tr><td>a</td><td><span class="badge"></span></td></tr>
                <tr><td>b</td><td><span class="badge"></span></td></tr>
                <tr><td>c</td><td><span class="badge"></span></td></tr>
              </tbody>
            </table>
          </filterable-table>`
        customElements.upgrade(document.body)
        // Nothing to facet yet: every value is blank.
        expect(
            document.querySelectorAll('.filterable-table-facet')
        ).toHaveLength(0)

        const badges = document.querySelectorAll<HTMLElement>('.badge')
        badges[0].textContent = 'GA'
        badges[1].textContent = 'Beta'
        badges[2].textContent = 'GA'

        await new Promise((resolve) => setTimeout(resolve, 250))

        const facets = document.querySelectorAll('.filterable-table-facet')
        expect(facets).toHaveLength(1)
        expect(facets[0].textContent).toContain('Availability')
        expect(
            Array.from(facets[0].querySelectorAll('option')).map(
                (o) => o.textContent
            )
        ).toEqual(['All', 'Beta', 'GA'])
    })

    it('keeps the reader selection when the dropdowns are rebuilt', async () => {
        document.body.innerHTML = `
          <filterable-table>
            <table>
              <thead><tr><th>Name</th><th>Type</th><th>Availability</th></tr></thead>
              <tbody>
                <tr><td>a</td><td>Receiver</td><td><span class="badge"></span></td></tr>
                <tr><td>b</td><td>Exporter</td><td><span class="badge"></span></td></tr>
                <tr><td>c</td><td>Receiver</td><td><span class="badge"></span></td></tr>
              </tbody>
            </table>
          </filterable-table>`
        customElements.upgrade(document.body)
        const typeSelect = document.querySelector<HTMLSelectElement>(
            '.filterable-table-facet select'
        )!
        typeSelect.value = 'Receiver'
        typeSelect.dispatchEvent(new Event('change'))
        expect(visibleRows()).toHaveLength(2)

        document.querySelectorAll<HTMLElement>('.badge').forEach((b, i) => {
            b.textContent = i === 1 ? 'Beta' : 'GA'
        })
        await new Promise((resolve) => setTimeout(resolve, 250))

        expect(
            document.querySelectorAll('.filterable-table-facet')
        ).toHaveLength(2)
        const rebuilt = document.querySelector<HTMLSelectElement>(
            '.filterable-table-facet select'
        )!
        expect(rebuilt.value).toBe('Receiver')
        expect(visibleRows()).toHaveLength(2)
    })
})
