import { renderWithProviders } from '../../test-utils/render'
import { PropertyResultRow } from './PropertyResultRow'
import type { ApiPropertyResult } from './apiPropertyResults'
import { screen } from '@testing-library/react'

const renderRow = (
    searchTerm: string,
    path: string,
    isSelected = false,
    location: ApiPropertyResult['location'] = 'request'
) =>
    renderWithProviders(
        <PropertyResultRow
            property={{
                name: path.split('.').pop() ?? path,
                path,
                type: 'object',
                location,
                description: 'Property description',
                anchor: 'req-property',
                url: '/docs/api/search#req-property',
                parent: '',
            }}
            searchTerm={searchTerm}
            index={1}
            isSelected={isSelected}
            isKeyboardNavigating={false}
            onMouseEnter={vi.fn()}
            onMouseMove={vi.fn()}
            onClick={vi.fn()}
        />
    )

it('underlines the path when the row is selected', () => {
    const path = (row: HTMLElement) =>
        row.querySelector('.property-path') as HTMLElement
    const { unmount } = renderRow('knn', 'retriever.knn', true)
    expect(screen.getByRole('link')).toHaveAttribute('data-selected', 'true')
    expect(
        getComputedStyle(path(screen.getByRole('link'))).textDecoration
    ).toContain('underline')
    unmount()
    renderRow('knn', 'retriever.knn', false)
    expect(
        getComputedStyle(path(screen.getByRole('link'))).textDecoration
    ).not.toContain('underline')
})

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
    expect(path).toHaveAttribute('data-tippy-delay', '[500, 0]')
    expect(type).toHaveAttribute('data-tippy-delay', '[500, 0]')
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

it.each([
    ['path', 'path'],
    ['query', 'query'],
    ['header', 'header'],
    ['request', 'request body'],
    ['response', 'response body'],
] as const)('shows where the property lives: %s', (location, label) => {
    renderRow('knn', 'retriever.knn', false, location)
    const chip = screen
        .getByRole('link')
        .querySelector('.property-path-line > .property-location')
    expect(chip).toHaveTextContent(label)
    expect(chip?.previousElementSibling).toHaveClass('property-path')
})

it('says nothing about child properties in its accessible name', () => {
    renderRow('knn', 'retriever.knn')
    expect(screen.getByRole('link')).toHaveAccessibleName(
        'retriever.knn object request Property description'
    )
    expect(screen.queryByText('Contains')).toBeNull()
})

it('offers "Go to property" as the Enter action', () => {
    renderRow('knn', 'retriever.knn')
    expect(screen.getByText('Go to property')).toBeInTheDocument()
})

it('renders the field icon in the gutter', () => {
    renderRow('knn', 'retriever.knn')
    const row = screen.getByRole('link')
    const icon = row.querySelector('.property-icon')
    expect(icon).toHaveAttribute('aria-hidden', 'true')
    expect(
        Array.from(row.children, (child) => child.className).slice(0, 2)
    ).toEqual(['property-icon', 'property-body'])
})
