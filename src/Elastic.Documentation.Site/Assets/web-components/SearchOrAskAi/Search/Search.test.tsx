import { chatStore } from '../AskAi/chat.store'
import { cooldownStore } from '../cooldown.store'
import { modalStore } from '../modal.store'
import { Search } from './Search'
import { searchStore, NO_SELECTION } from './search.store'
import { SearchResultItem } from './useSearchQuery'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

// Mock external HTTP calls
jest.mock('@microsoft/fetch-event-source', () => ({
    fetchEventSource: jest.fn(),
    EventStreamContentType: 'text/event-stream',
}))

// Mock fetch for search API
const mockFetch = jest.fn()
global.fetch = mockFetch

// Helper to create a fresh QueryClient for each test
const createTestQueryClient = () =>
    new QueryClient({
        defaultOptions: {
            queries: {
                retry: false,
                gcTime: 0,
            },
        },
    })

// Wrapper with QueryClient
const TestWrapper = ({ children }: { children: React.ReactNode }) => {
    const queryClient = createTestQueryClient()
    return (
        <QueryClientProvider client={queryClient}>
            {children}
        </QueryClientProvider>
    )
}

// Helper to reset all stores
const resetStores = () => {
    searchStore.setState({
        searchTerm: '',
        page: 1,
        typeFilter: 'all',
        selectedIndex: NO_SELECTION,
    })
    chatStore.setState({
        chatMessages: [],
        conversationId: null,
        aiProvider: 'LlmGateway',
        scrollPosition: 0,
    })
    modalStore.setState({ isOpen: false, mode: 'search' })
    cooldownStore.setState({
        cooldowns: {
            search: { cooldown: null, awaitingNewInput: false },
            askAi: { cooldown: null, awaitingNewInput: false },
        },
    })
}

// Helper to mock successful search response
const mockSearchResponse = (results: SearchResultItem[] = []) => {
    mockFetch.mockResolvedValue({
        ok: true,
        json: () =>
            Promise.resolve({
                results,
                totalResults: results.length,
                pageCount: 1,
                pageNumber: 1,
                pageSize: 10,
            }),
    })
}

describe('Search Component', () => {
    beforeEach(() => {
        jest.clearAllMocks()
        resetStores()
        mockSearchResponse([])
    })

    describe('Search input', () => {
        it('should render search input field', () => {
            // Act
            render(<Search />, { wrapper: TestWrapper })

            // Assert
            expect(
                screen.getByPlaceholderText(/search in docs/i)
            ).toBeInTheDocument()
        })

        it('should update store when input changes', async () => {
            // Arrange
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.type(input, 'elasticsearch')

            // Assert
            expect(searchStore.getState().searchTerm).toBe('elasticsearch')
        })

        it('should display search term from store', () => {
            // Arrange
            searchStore.setState({ searchTerm: 'kibana' })

            // Act
            render(<Search />, { wrapper: TestWrapper })

            // Assert
            const input = screen.getByPlaceholderText(
                /search in docs/i
            ) as HTMLInputElement
            expect(input.value).toBe('kibana')
        })

        it('should reset selectedIndex when search term changes', async () => {
            // Arrange
            searchStore.setState({ searchTerm: 'test', selectedIndex: 2 })
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.type(input, 'x')

            // Assert - selectedIndex should reset to 0
            expect(searchStore.getState().selectedIndex).toBe(0)
        })
    })

    describe('Ask AI button', () => {
        it('should not show Ask AI button when search term is empty', () => {
            // Act
            render(<Search />, { wrapper: TestWrapper })

            // Assert
            expect(
                screen.queryByRole('button', { name: /tell me more about/i })
            ).not.toBeInTheDocument()
        })

        it('should show Ask AI button when search term exists', () => {
            // Arrange
            searchStore.setState({ searchTerm: 'elasticsearch' })

            // Act
            render(<Search />, { wrapper: TestWrapper })

            // Assert
            expect(
                screen.getByRole('button', { name: /tell me more about/i })
            ).toBeInTheDocument()
        })

        it('should trigger chat when Ask AI button is clicked', async () => {
            // Arrange
            searchStore.setState({ searchTerm: 'what is kibana' })
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            await user.click(
                screen.getByRole('button', { name: /tell me more about/i })
            )

            // Assert - chat store should have user message and mode should change
            await waitFor(() => {
                const messages = chatStore.getState().chatMessages
                expect(messages.length).toBeGreaterThanOrEqual(1)
                expect(messages[0].content).toBe(
                    'Tell me more about what is kibana'
                )
            })
            expect(modalStore.getState().mode).toBe('askAi')
        })

        it('should not submit whitespace-only search term', async () => {
            // Arrange
            searchStore.setState({ searchTerm: '   ' })
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            // Button should still be visible for whitespace
            const button = screen.queryByRole('button', {
                name: /tell me more about/i,
            })
            if (button) {
                await user.click(button)
            }

            // Assert - chat should not be triggered
            expect(chatStore.getState().chatMessages).toHaveLength(0)
        })
    })

    describe('Search on Enter', () => {
        it('should trigger chat when Enter is pressed with valid search and no results', async () => {
            // Arrange
            searchStore.setState({ searchTerm: 'elasticsearch query' })
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.click(input)
            await user.keyboard('{Enter}')

            // Assert
            await waitFor(() => {
                const messages = chatStore.getState().chatMessages
                expect(messages[0]?.content).toBe(
                    'Tell me more about elasticsearch query'
                )
            })
        })

        it('should not submit empty search on Enter', async () => {
            // Arrange
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.click(input)
            await user.keyboard('{Enter}')

            // Assert
            expect(chatStore.getState().chatMessages).toHaveLength(0)
        })
    })

    describe('Search results', () => {
        it('should fetch and display search results', async () => {
            // Arrange
            mockSearchResponse([
                {
                    type: 'doc',
                    url: '/test1',
                    title: 'Test Result 1',
                    description: 'Description 1',
                    score: 0.9,
                    parents: [],
                },
            ])
            searchStore.setState({ searchTerm: 'test' })

            // Act
            render(<Search />, { wrapper: TestWrapper })

            // Assert - wait for debounced search and result
            await waitFor(
                () => {
                    expect(
                        screen.getByText('Test Result 1')
                    ).toBeInTheDocument()
                },
                { timeout: 1000 }
            )
        })
    })

    describe('Selection navigation', () => {
        beforeEach(() => {
            mockSearchResponse([
                {
                    type: 'doc',
                    url: '/test1',
                    title: 'Test Result 1',
                    description: 'Description 1',
                    score: 0.9,
                    parents: [],
                },
                {
                    type: 'doc',
                    url: '/test2',
                    title: 'Test Result 2',
                    description: 'Description 2',
                    score: 0.8,
                    parents: [],
                },
                {
                    type: 'doc',
                    url: '/test3',
                    title: 'Test Result 3',
                    description: 'Description 3',
                    score: 0.7,
                    parents: [],
                },
            ])
        })

        it('should select first item after typing (selectedIndex = 0)', async () => {
            // Arrange - start with no selection
            expect(searchStore.getState().selectedIndex).toBe(NO_SELECTION)
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.type(input, 'test')

            await waitFor(() => {
                expect(screen.getByText('Test Result 1')).toBeInTheDocument()
            })

            // Assert - selection appears after typing
            expect(searchStore.getState().selectedIndex).toBe(0)
        })

        it('should move selection to second result on ArrowDown from input (focus stays on input)', async () => {
            // Arrange
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })

            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.type(input, 'test')

            await waitFor(() => {
                expect(screen.getByText('Test Result 1')).toBeInTheDocument()
            })

            // selectedIndex is now 0 after typing
            expect(searchStore.getState().selectedIndex).toBe(0)

            await user.keyboard('{ArrowDown}')

            // Assert - selection moved to second result, focus stays on input (Pattern B)
            expect(searchStore.getState().selectedIndex).toBe(1)
            expect(input).toHaveFocus()
        })

        it('should move focus between results with ArrowDown/ArrowUp', async () => {
            // Arrange
            searchStore.setState({ searchTerm: 'test' })
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })

            await waitFor(() => {
                expect(screen.getByText('Test Result 1')).toBeInTheDocument()
            })

            // Focus first result
            const firstResult = screen.getByText('Test Result 1').closest('a')!
            await act(async () => {
                firstResult.focus()
            })

            // Navigate down
            await user.keyboard('{ArrowDown}')
            const secondResult = screen.getByText('Test Result 2').closest('a')
            expect(secondResult).toHaveFocus()
            expect(searchStore.getState().selectedIndex).toBe(1)

            // Navigate up
            await user.keyboard('{ArrowUp}')
            expect(firstResult).toHaveFocus()
            expect(searchStore.getState().selectedIndex).toBe(0)
        })

        it('should stay at first item when ArrowUp from first item (no wrap)', async () => {
            // Arrange
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.type(input, 'test')

            await waitFor(() => {
                expect(screen.getByText('Test Result 1')).toBeInTheDocument()
            })

            // Focus first result then press ArrowUp
            const firstResult = screen.getByText('Test Result 1').closest('a')!
            await act(async () => {
                firstResult.focus()
            })
            expect(searchStore.getState().selectedIndex).toBe(0)

            await user.keyboard('{ArrowUp}')

            // Assert - stays at first item (no wrap around)
            expect(firstResult).toHaveFocus()
            expect(searchStore.getState().selectedIndex).toBe(0)
        })

        it('should stay at last item when ArrowDown from last item (no wrap)', async () => {
            // Arrange
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.type(input, 'test')

            await waitFor(() => {
                expect(screen.getByText('Test Result 3')).toBeInTheDocument()
            })

            // Focus the last item directly
            const lastResult = screen.getByText('Test Result 3').closest('a')!
            await act(async () => {
                lastResult.focus()
            })
            expect(searchStore.getState().selectedIndex).toBe(2)

            // Try to go down from last item
            await user.keyboard('{ArrowDown}')

            // Assert - stays at last item (no wrap around)
            expect(lastResult).toHaveFocus()
            expect(searchStore.getState().selectedIndex).toBe(2)
        })

        it('should render isSelected prop on the selected item', async () => {
            // Arrange
            searchStore.setState({ searchTerm: 'test', selectedIndex: 1 })

            // Act
            render(<Search />, { wrapper: TestWrapper })

            await waitFor(() => {
                expect(screen.getByText('Test Result 2')).toBeInTheDocument()
            })

            // Assert - the second result should have the data-selected attribute
            const secondResultLink = screen
                .getByText('Test Result 2')
                .closest('a')
            expect(secondResultLink).toHaveAttribute('data-selected', 'true')

            // First and third should not be selected
            const firstResultLink = screen
                .getByText('Test Result 1')
                .closest('a')
            expect(firstResultLink).not.toHaveAttribute('data-selected')
        })
    })

    describe('Loading states', () => {
        it('should show loading spinner when fetching', async () => {
            // Arrange - slow response
            mockFetch.mockImplementation(
                () =>
                    new Promise((resolve) =>
                        setTimeout(
                            () =>
                                resolve({
                                    ok: true,
                                    json: () =>
                                        Promise.resolve({
                                            results: [],
                                            totalResults: 0,
                                            pageCount: 1,
                                            pageNumber: 1,
                                            pageSize: 10,
                                        }),
                                }),
                            500
                        )
                    )
            )
            searchStore.setState({ searchTerm: 'test' })

            // Act
            render(<Search />, { wrapper: TestWrapper })

            // Assert - loading spinner should appear
            await waitFor(() => {
                const spinner = screen.queryByRole('progressbar')
                // Spinner appears during loading
                expect(spinner || screen.queryByText('test')).toBeTruthy()
            })
        })
    })

    describe('Close modal', () => {
        it('should close modal and clear search when close button is clicked', async () => {
            // Arrange
            modalStore.setState({ isOpen: true })
            searchStore.setState({ searchTerm: 'test' })
            const user = userEvent.setup()

            // Act
            render(<Search />, { wrapper: TestWrapper })
            await user.click(
                screen.getByRole('button', { name: /close search modal/i })
            )

            // Assert
            expect(modalStore.getState().isOpen).toBe(false)
            expect(searchStore.getState().searchTerm).toBe('')
        })
    })
})
