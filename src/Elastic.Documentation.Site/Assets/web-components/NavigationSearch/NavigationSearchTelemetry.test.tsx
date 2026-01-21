import * as logging from '../../telemetry/logging'
import { cooldownStore } from '../shared/cooldown.store'
import { NavigationSearch } from './NavigationSearch'
import { SearchResultsList } from './SearchResultsList'
import { navigationSearchStore } from './navigationSearch.store'
import * as queryHook from './useNavigationSearchQuery'
import { EuiProvider } from '@elastic/eui'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

// Mock htmx - it uses XPath which jsdom doesn't support properly
jest.mock('htmx.org', () => ({
    on: jest.fn(),
    off: jest.fn(),
    process: jest.fn(),
    ajax: jest.fn(),
}))

// Mock the telemetry logging functions
jest.mock('../../telemetry/logging', () => ({
    logInfo: jest.fn(),
    logWarn: jest.fn(),
}))

// Mock search results for result click tests
const mockSearchResults = {
    results: [
        {
            url: '/docs/elasticsearch/guide',
            title: 'Elasticsearch Guide',
            description: 'Learn about Elasticsearch',
            score: 0.95,
            type: 'doc' as const,
            parents: [{ title: 'Docs' }, { title: 'Elasticsearch' }],
        },
        {
            url: '/docs/kibana/dashboard',
            title: 'Kibana Dashboard',
            description: 'Create dashboards',
            score: 0.85,
            type: 'doc' as const,
            parents: [{ title: 'Docs' }, { title: 'Kibana' }],
        },
    ],
    totalResults: 2,
    pageCount: 1,
}

// Create a fresh QueryClient for each test
const createTestQueryClient = () =>
    new QueryClient({
        defaultOptions: {
            queries: { retry: false },
            mutations: { retry: false },
        },
    })

// Wrapper component for tests
const renderWithProviders = (ui: React.ReactElement) => {
    const testQueryClient = createTestQueryClient()
    return render(
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <QueryClientProvider client={testQueryClient}>
                {ui}
            </QueryClientProvider>
        </EuiProvider>
    )
}

// Helper to reset all stores
const resetStores = () => {
    navigationSearchStore.getState().actions.clearSearchTerm()
    cooldownStore.setState({
        cooldowns: {
            search: { cooldown: null, awaitingNewInput: false },
            askAi: { cooldown: null, awaitingNewInput: false },
        },
    })
}

describe('Navigation Search Telemetry Integration', () => {
    beforeEach(() => {
        jest.clearAllMocks()
        resetStores()
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

    describe('Opening Navigation Search', () => {
        it('should track navigation_search_opened when input is focused', async () => {
            // Arrange
            renderWithProviders(<NavigationSearch />)
            const input = screen.getByPlaceholderText(/jump to page/i)

            // Act
            await userEvent.click(input)

            // Assert
            expect(logging.logInfo).toHaveBeenCalledWith(
                'navigation_search_opened',
                {
                    'navigation_search.trigger': 'focus',
                }
            )
        })

        it('should track keyboard_shortcut trigger when opened via Cmd+K', async () => {
            // Arrange
            renderWithProviders(<NavigationSearch />)

            // Act - simulate Cmd+K
            await userEvent.keyboard('{Meta>}k{/Meta}')

            // Assert
            expect(logging.logInfo).toHaveBeenCalledWith(
                'navigation_search_opened',
                {
                    'navigation_search.trigger': 'keyboard_shortcut',
                }
            )
        })
    })

    describe('Closing Navigation Search', () => {
        it('should track navigation_search_closed with escape reason when pressing Escape', async () => {
            // Arrange
            renderWithProviders(<NavigationSearch />)
            const input = screen.getByPlaceholderText(/jump to page/i)

            // Act - focus and type, then escape
            await userEvent.click(input)
            await userEvent.type(input, 'elasticsearch')
            jest.clearAllMocks() // Clear the opened event

            await userEvent.keyboard('{Escape}')

            // Assert
            expect(logging.logInfo).toHaveBeenCalledWith(
                'navigation_search_closed',
                expect.objectContaining({
                    'navigation_search.close_reason': 'escape',
                    'navigation_search.query': 'elasticsearch',
                })
            )
        })

        it('should track navigation_search_closed with blur reason when clicking outside', async () => {
            // Arrange
            renderWithProviders(
                <div>
                    <NavigationSearch />
                    <button data-testid="outside">Outside</button>
                </div>
            )
            const input = screen.getByPlaceholderText(/jump to page/i)

            // Act - focus, type, then click outside
            await userEvent.click(input)
            await userEvent.type(input, 'test')
            jest.clearAllMocks()

            await userEvent.click(screen.getByTestId('outside'))

            // Assert
            expect(logging.logInfo).toHaveBeenCalledWith(
                'navigation_search_closed',
                expect.objectContaining({
                    'navigation_search.close_reason': 'blur',
                })
            )
        })

        it('should include hadResults and hadSelection in close event', async () => {
            // Arrange
            renderWithProviders(<NavigationSearch />)
            const input = screen.getByPlaceholderText(/jump to page/i)

            // Act - focus, type, then escape without results
            await userEvent.click(input)
            await userEvent.type(input, 'test')
            jest.clearAllMocks()

            await userEvent.keyboard('{Escape}')

            // Assert - should have hadResults and hadSelection fields
            expect(logging.logInfo).toHaveBeenCalledWith(
                'navigation_search_closed',
                expect.objectContaining({
                    'navigation_search.had_results': expect.any(Boolean),
                    'navigation_search.had_selection': expect.any(Boolean),
                })
            )
        })
    })

    describe('Error Tracking', () => {
        it('should track navigation_search_rate_limited on 429 response', async () => {
            // Arrange - mock 429 response
            global.fetch = jest.fn().mockResolvedValue({
                ok: false,
                status: 429,
                headers: {
                    get: (name: string) =>
                        name === 'Retry-After' ? '30' : null,
                },
                json: () => Promise.resolve({ error: 'Rate limited' }),
            })

            renderWithProviders(<NavigationSearch />)
            const input = screen.getByPlaceholderText(/jump to page/i)

            // Act
            await userEvent.click(input)
            await userEvent.type(input, 'test query')

            // Assert - wait for rate limit warning
            await waitFor(() => {
                expect(logging.logWarn).toHaveBeenCalledWith(
                    'navigation_search_rate_limited',
                    expect.objectContaining({
                        'navigation_search.query': expect.any(String),
                    })
                )
            })
        })
    })
})

describe('Navigation Search Result Click Tracking', () => {
    // Shared props for SearchResultsList - reduces duplication
    const createResultsListProps = () => ({
        isKeyboardNavigating: { current: false },
        onMouseMove: jest.fn(),
        onResultClick: jest.fn(),
    })

    beforeEach(() => {
        jest.clearAllMocks()
        resetStores()
        // Set up the store with a search term
        navigationSearchStore.getState().actions.setSearchTerm('elasticsearch')

        // Mock the query hook to return results
        jest.spyOn(queryHook, 'useNavigationSearchQuery').mockReturnValue({
            isLoading: false,
            isFetching: false,
            data: mockSearchResults,
            error: null,
        } as ReturnType<typeof queryHook.useNavigationSearchQuery>)
    })

    afterEach(() => {
        jest.restoreAllMocks()
    })

    it('should track result click with query, position, url, and score', async () => {
        // Arrange
        const props = createResultsListProps()
        renderWithProviders(<SearchResultsList {...props} />)

        // Act
        await userEvent.click(screen.getByText('Elasticsearch Guide'))

        // Assert - verify all required telemetry fields
        expect(logging.logInfo).toHaveBeenCalledWith(
            'navigation_search_result_clicked',
            {
                'navigation_search.query': 'elasticsearch',
                'navigation_search.result.position': 0,
                'navigation_search.result.url': '/docs/elasticsearch/guide',
                'navigation_search.result.score': 0.95,
            }
        )
        expect(props.onResultClick).toHaveBeenCalledTimes(1)
    })

    it('should track correct position for each result (0-indexed)', async () => {
        // Arrange
        const props = createResultsListProps()
        renderWithProviders(<SearchResultsList {...props} />)

        // Act - click second result
        await userEvent.click(screen.getByText('Kibana Dashboard'))

        // Assert
        expect(logging.logInfo).toHaveBeenCalledWith(
            'navigation_search_result_clicked',
            expect.objectContaining({
                'navigation_search.result.position': 1,
            })
        )
    })

    it('should use current search term from store in telemetry', async () => {
        // Arrange - change the search term after initial setup
        navigationSearchStore.getState().actions.setSearchTerm('updated query')
        const props = createResultsListProps()
        renderWithProviders(<SearchResultsList {...props} />)

        // Act
        await userEvent.click(screen.getByText('Elasticsearch Guide'))

        // Assert - query should reflect the updated store value
        expect(logging.logInfo).toHaveBeenCalledWith(
            'navigation_search_result_clicked',
            expect.objectContaining({
                'navigation_search.query': 'updated query',
            })
        )
    })
})
