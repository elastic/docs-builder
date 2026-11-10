import { renderWithProviders } from '../../test-utils/render'
import { ModalSearchResultsList } from './ModalSearchResultsList'
import { modalSearchStore } from './modalSearch.store'

vi.mock('./useModalSearchQuery', () => {
    const operation = {
        type: 'api',
        url: '/docs/api/doc/elasticsearch/operation/operation-rollup-put-job',
        title: 'Create a rollup job',
        description: 'Creates a rollup job',
        score: 1,
        parents: [{ url: '/docs/api', title: 'API' }],
    }
    return {
        useModalSearchQuery: () => ({
            isLoading: false,
            isFetching: false,
            data: {
                results: [
                    { ...operation, deprecated: true },
                    {
                        ...operation,
                        url: '/docs/api/doc/elasticsearch/operation/operation-search',
                        title: 'Search',
                    },
                ],
            },
            error: null,
        }),
    }
})

vi.mock('./useModalSearchTelemetry', () => ({
    useModalSearchTelemetry: () => ({ trackResultClicked: vi.fn() }),
}))

describe('ModalSearchResultsList', () => {
    beforeEach(() => {
        modalSearchStore.getState().actions.setSearchTerm('rollup')
    })

    it('marks a deprecated operation with the page badge and leaves a live one plain', () => {
        const { container } = renderWithProviders(
            <ModalSearchResultsList
                isKeyboardNavigating={{ current: false }}
                onMouseMove={vi.fn()}
                onResultClick={vi.fn()}
            />
        )

        const rows = container.querySelectorAll('a[data-search-result-index]')
        expect(rows).toHaveLength(2)
        expect(
            rows[0].querySelector('.title-text .api-badge--danger')
        ).toHaveTextContent('Deprecated')
        expect(rows[1].querySelector('.api-badge--danger')).toBeNull()
    })
})
