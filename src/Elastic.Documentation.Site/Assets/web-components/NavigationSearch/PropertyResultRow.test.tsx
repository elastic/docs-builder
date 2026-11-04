import { renderWithProviders } from '../../test-utils/render'
import { PropertyResultRow } from './PropertyResultRow'
import { screen } from '@testing-library/react'

const renderRow = (searchTerm: string, path: string) =>
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
            showLocation={false}
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
    expect(row).toHaveAttribute(
        'title',
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
