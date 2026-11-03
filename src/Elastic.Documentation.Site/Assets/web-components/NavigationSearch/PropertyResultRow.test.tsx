import { PropertyResultRow } from './PropertyResultRow'
import { EuiProvider } from '@elastic/eui'
import { render, screen } from '@testing-library/react'

const renderRow = (searchTerm: string, path: string) =>
    render(
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <PropertyResultRow
                property={{
                    name: path.split('.').pop() ?? path,
                    path,
                    type: 'object',
                    location: 'request',
                    description: 'Property description',
                    anchor: 'req-property',
                    url: '/docs/api/search#req-property',
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
        </EuiProvider>
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
