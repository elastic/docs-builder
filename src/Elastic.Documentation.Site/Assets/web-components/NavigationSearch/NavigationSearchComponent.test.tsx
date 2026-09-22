import { sharedQueryClient } from '../shared/queryClient'
import { NavigationSearchWrapper } from './NavigationSearchComponent'
import { navigationSearchStore } from './navigationSearch.store'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'

jest.mock('htmx.org', () => ({
    on: jest.fn(),
    off: jest.fn(),
    process: jest.fn(),
    ajax: jest.fn(),
}))

jest.mock('../../telemetry/logging', () => ({
    logInfo: jest.fn(),
    logWarn: jest.fn(),
    logError: jest.fn(),
}))

const emptySearch = {
    results: [],
    totalResults: 0,
    pageCount: 0,
    pageNumber: 1,
    pageSize: 20,
}

const searchRequestUrls = () =>
    jest
        .mocked(global.fetch)
        .mock.calls.map(([input]) => String(input))
        .filter((url) => url.includes('/v1/navigation-search'))

describe('NavigationSearchWrapper type attribute', () => {
    beforeEach(() => {
        sharedQueryClient.clear()
        navigationSearchStore.getState().actions.clearSearchTerm()
        global.fetch = jest.fn().mockImplementation((input: RequestInfo) => {
            const url = String(input)
            if (url.includes('/v1/navigation-search')) {
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(emptySearch),
                })
            }
            return Promise.resolve({ ok: true })
        })
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

    it('requests navigation-search with type=api when type is api', async () => {
        render(<NavigationSearchWrapper type="api" />)

        const input = await screen.findByPlaceholderText('Jump to API')
        await userEvent.type(input, '_bulk')

        await waitFor(() => {
            expect(
                searchRequestUrls().some((url) => url.includes('type=api'))
            ).toBe(true)
        })
    })

    it('filters the first query when health is cached and a search term is already set', async () => {
        sharedQueryClient.setQueryData(['api-health'], true)
        navigationSearchStore.getState().actions.setSearchTerm('_bulk')

        render(<NavigationSearchWrapper type="api" />)

        await waitFor(() => {
            const urls = searchRequestUrls()
            expect(urls.length).toBeGreaterThan(0)
            expect(urls.every((url) => url.includes('type=api'))).toBe(true)
        })
    })

    it('leaves the query unfiltered when type is omitted', async () => {
        render(<NavigationSearchWrapper />)

        const input = await screen.findByPlaceholderText('Jump to page')
        await userEvent.type(input, '_bulk')

        await waitFor(() => {
            const urls = searchRequestUrls()
            expect(urls.length).toBeGreaterThan(0)
            expect(urls.every((url) => !url.includes('type='))).toBe(true)
        })
    })
})
