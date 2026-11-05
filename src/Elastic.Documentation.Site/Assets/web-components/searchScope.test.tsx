import { modalSearchStore } from './ModalSearch/modalSearch.store'
import { useModalSearchQuery } from './ModalSearch/useModalSearchQuery'
import { navigationSearchStore } from './NavigationSearch/navigationSearch.store'
import { useNavigationSearchQuery } from './NavigationSearch/useNavigationSearchQuery'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'

vi.mock('../config', () => ({
    config: {
        buildType: 'assembler',
        apiBasePath: '/docs/_api',
        rootPath: '/docs',
    },
}))

const empty = {
    results: [],
    totalResults: 0,
    pageCount: 0,
    pageNumber: 1,
    pageSize: 20,
}

const v8 = '/docs/api/doc/elasticsearch/v8/operation/x'
const unversioned = '/docs/api/doc/elasticsearch/operation/x'

describe('navigation search scope', () => {
    let fetchMock: ReturnType<typeof vi.fn>
    let client: QueryClient

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    )

    beforeEach(() => {
        client = new QueryClient({
            defaultOptions: { queries: { retry: false } },
        })
        fetchMock = vi.fn(async () => ({
            ok: true,
            json: async () => empty,
        }))
        global.fetch = fetchMock as unknown as typeof fetch
        navigationSearchStore.getState().actions.setSearchTerm('bulk')
        modalSearchStore.getState().actions.setSearchTerm('bulk')
    })

    const lastUrl = () =>
        String(fetchMock.mock.calls[fetchMock.mock.calls.length - 1][0])

    it('modal search sends the version scope on a v8 path', async () => {
        history.pushState(null, '', v8)
        renderHook(() => useModalSearchQuery(), { wrapper })

        await waitFor(() => expect(fetchMock).toHaveBeenCalled())
        expect(lastUrl()).toBe(
            '/docs/_api/v1/navigation-search?q=bulk&page=1&api_key=elasticsearch&api_tree=8'
        )
    })

    it('modal search sends the api key alone on the unversioned path', async () => {
        history.pushState(null, '', unversioned)
        renderHook(() => useModalSearchQuery(), { wrapper })

        await waitFor(() => expect(fetchMock).toHaveBeenCalled())
        expect(lastUrl()).toBe(
            '/docs/_api/v1/navigation-search?q=bulk&page=1&api_key=elasticsearch'
        )
    })

    it('navigation search keys and requests follow the page after a swap', async () => {
        history.pushState(null, '', unversioned)
        const { rerender } = renderHook(() => useNavigationSearchQuery('all'), {
            wrapper,
        })
        await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1))
        expect(lastUrl()).toBe(
            '/docs/_api/v1/navigation-search?q=bulk&page=1&api_key=elasticsearch'
        )

        act(() => history.pushState(null, '', v8))
        rerender()

        await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))
        expect(lastUrl()).toBe(
            '/docs/_api/v1/navigation-search?q=bulk&page=1&api_key=elasticsearch&api_tree=8'
        )
        const keys = client
            .getQueryCache()
            .getAll()
            .map((query) => JSON.stringify(query.queryKey))
        expect(new Set(keys).size).toBe(2)
    })
})
