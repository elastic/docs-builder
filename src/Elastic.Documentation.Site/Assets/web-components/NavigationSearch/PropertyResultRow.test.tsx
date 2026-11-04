import { renderWithProviders } from '../../test-utils/render'
import { PropertyResultRow } from './PropertyResultRow'
import { screen } from '@testing-library/react'

const renderRow = (searchTerm: string, path: string, showLocation = false) =>
    renderWithProviders(
        <PropertyResultRow
            property={{
                name: path.split('.').pop() ?? path,
                path,
                type: 'object',
                location: 'request',
                description: 'Property description',
                anchor: 'req-property',
                url: '/docs/api/search#req-property',
                children: [],
            }}
            searchTerm={searchTerm}
            showLocation={showLocation}
            index={1}
            isSelected={false}
            isKeyboardNavigating={false}
            onMouseEnter={vi.fn()}
            onMouseMove={vi.fn()}
            onClick={vi.fn()}
        />
    )

it.each([
    ['search knn', 'retriever.knn', ['knn']],
    ['_search KNN', 'retriever.knn', ['knn']],
    ['search track_total', 'track_total_hits', ['track_total']],
    [
        'elasticsearch.remote_cluster',
        '[].elasticsearch.remote_cluster',
        ['elasticsearch', 'remote_cluster'],
    ],
    ['_search knn.field', 'knn.field', ['knn', 'field']],
    ['knn search', 'retriever.knn', ['knn']],
    ['knn /_search', 'retriever.knn', ['knn']],
    ['knn GET /_search', 'knn', ['knn']],
])('highlights the property match for %s', (searchTerm, path, matches) => {
    renderRow(searchTerm, path)
    const row = screen.getByRole('link')
    expect(
        Array.from(row.querySelectorAll('mark'), (mark) => mark.textContent)
    ).toEqual(matches)
    expect(row.querySelector('code')).toHaveTextContent(path)
})

it("uses the page's tooltip for the path and type", () => {
    renderRow('knn', 'retriever.knn')
    const row = screen.getByRole('link')
    const [path, type] = row.querySelectorAll('code')
    expect(path).toHaveAttribute('data-tippy-content', 'retriever.knn')
    expect(path).toHaveAttribute('data-tippy-theme', 'mono')
    expect(type).toHaveAttribute('data-tippy-content', 'object')
    expect(type).toHaveAttribute('data-tippy-theme', 'mono')
    expect(row).not.toHaveAttribute('title')
})

it('puts the path and the description on separate lines', () => {
    renderRow('knn', 'retriever.knn')
    const row = screen.getByRole('link')
    expect(
        row.querySelector('.property-path-line > code.property-path')
    ).toHaveTextContent('retriever.knn')
    const detail = row.querySelector('.property-detail-line')
    expect(
        Array.from(detail?.children ?? [], (child) => [
            child.tagName,
            child.textContent,
        ])
    ).toEqual([
        ['CODE', 'object'],
        ['SPAN', 'Property description'],
    ])
})

it('keeps the leaf of a long path visible', () => {
    renderRow('replicas', 'settings.index.number_of_replicas')
    const path = screen.getByRole('link').querySelector('.property-path')
    expect(path?.querySelector('strong')?.textContent).toBe(
        'number_of_replicas'
    )
    expect(path?.querySelector('bdi')?.textContent).toBe(
        'settings.index.number_of_replicas'
    )
    expect(path?.querySelector('bdi mark')?.textContent).toBe('replicas')
})

it('shows the location chip only when the same path appears twice', () => {
    const { unmount } = renderRow('knn', 'retriever.knn', true)
    const location = (row: HTMLElement) =>
        row.querySelector('.property-path-line > span')
    const chip = location(screen.getByRole('link'))
    expect(chip).toHaveClass('property-location')
    expect(chip).toHaveTextContent('request')
    unmount()
    renderRow('knn', 'retriever.knn')
    expect(location(screen.getByRole('link'))).toHaveClass('sr-only')
})

it('offers "Go to property" as the Enter action', () => {
    renderRow('knn', 'retriever.knn')
    expect(screen.getByText('Go to property')).toBeInTheDocument()
})

const renderParent = (children: string[]) =>
    renderWithProviders(
        <PropertyResultRow
            property={{
                name: 'standard',
                path: 'retriever.standard',
                type: 'object',
                location: 'request',
                description: 'Standard retriever',
                anchor: 'req-retriever-standard',
                url: '/docs/api/search#req-retriever-standard',
                children,
            }}
            searchTerm="retriever.standard"
            showLocation={false}
            index={1}
            isSelected={false}
            isKeyboardNavigating={false}
            onMouseEnter={vi.fn()}
            onMouseMove={vi.fn()}
            onClick={vi.fn()}
        />
    )

it("lists the field's children under its path", () => {
    renderParent([
        '.filter',
        '.min_score',
        '._name',
        '.query',
        '.search_after',
        '.terminate_after',
        '.sort',
        '.collapse',
    ])
    const row = screen.getByRole('link')
    expect(row.querySelector('.property-children')?.textContent).toBe(
        'filter min_score _name query search_after terminate_after sort collapse'
    )
    const children = row.querySelector('.property-children')
    expect(children).toHaveAttribute(
        'data-tippy-content',
        'filter, min_score, _name, query, search_after, terminate_after, sort, collapse'
    )
    expect(children).toHaveAttribute('data-tippy-theme', 'mono')
    expect(row).toHaveAttribute(
        'aria-label',
        expect.stringContaining(
            'Fields: filter, min_score, _name, query, search_after, terminate_after, sort, collapse'
        )
    )
})

it('renders no children line for a leaf', () => {
    renderParent([])
    expect(
        screen.getByRole('link').querySelector('.property-children')
    ).toBeNull()
})
