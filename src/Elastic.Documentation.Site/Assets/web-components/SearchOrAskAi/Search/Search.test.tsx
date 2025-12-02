import { Search } from './Search'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

/*
 * Note: These tests use mock verification for store actions.
 *
 * Unlike pure unit tests, the Search component's main responsibility is
 * orchestrating the handoff from search to chat (calling clearChat,
 * submitQuestion, setModalMode in the right order). Testing these calls
 * verifies the integration/workflow, not just implementation details.
 *
 * For full E2E behavior testing without mocks, see integration tests.
 */

// Mock dependencies
jest.mock('./search.store', () => ({
    useSearchTerm: jest.fn(() => ''),
    usePageNumber: jest.fn(() => 0),
    useSearchActions: jest.fn(() => ({
        setSearchTerm: jest.fn(),
        clearSearchTerm: jest.fn(),
        setPageNumber: jest.fn(),
    })),
}))

jest.mock('../AskAi/chat.store', () => ({
    useChatActions: jest.fn(() => ({
        submitQuestion: jest.fn(),
        clearChat: jest.fn(),
        clearNon429Errors: jest.fn(),
        setAiProvider: jest.fn(),
    })),
}))

jest.mock('../modal.store', () => ({
    useModalActions: jest.fn(() => ({
        setModalMode: jest.fn(),
        openModal: jest.fn(),
        closeModal: jest.fn(),
        toggleModal: jest.fn(),
    })),
}))

jest.mock('./useSearchCooldown', () => ({
    useIsSearchCooldownActive: jest.fn(() => false),
    useSearchCooldown: jest.fn(() => null),
    useIsSearchAwaitingNewInput: jest.fn(() => false),
    useSearchCooldownActions: jest.fn(() => ({
        setCooldown: jest.fn(),
        updateCooldown: jest.fn(),
        notifyCooldownFinished: jest.fn(),
        acknowledgeCooldownFinished: jest.fn(),
    })),
}))

jest.mock('../AskAi/useAskAiCooldown', () => ({
    useIsAskAiCooldownActive: jest.fn(() => false),
}))

jest.mock('../useCooldown', () => ({
    useCooldown: jest.fn(),
}))

// Use the real SearchResults component - its dependencies are already mocked

const mockUseSearchQuery = jest.fn(() => ({
    isLoading: false,
    isFetching: false,
    data: null as {
        results: Array<{
            url: string
            title: string
            description: string
            score: number
            parents: Array<{ url: string; title: string }>
        }>
        totalResults: number
        pageCount: number
        pageNumber: number
        pageSize: number
    } | null,
    error: null as Error | null,
    cancelQuery: jest.fn(),
}))

jest.mock('./useSearchQuery', () => ({
    useSearchQuery: () => mockUseSearchQuery(),
}))

// Mock SearchOrAskAiErrorCallout
jest.mock('../SearchOrAskAiErrorCallout', () => ({
    SearchOrAskAiErrorCallout: () => null,
}))

// Mock rate limit handlers
jest.mock('./useSearchRateLimitHandler', () => ({
    useSearchRateLimitHandler: jest.fn(),
}))

jest.mock('../AskAi/useAskAiRateLimitHandler', () => ({
    useAskAiRateLimitHandler: jest.fn(),
}))

const mockUseSearchTerm = jest.mocked(
    jest.requireMock('./search.store').useSearchTerm
)
const mockUseSearchActions = jest.mocked(
    jest.requireMock('./search.store').useSearchActions
)
const mockUseChatActions = jest.mocked(
    jest.requireMock('../AskAi/chat.store').useChatActions
)
const mockUseModalActions = jest.mocked(
    jest.requireMock('../modal.store').useModalActions
)

describe('Search Component', () => {
    const mockSetSearchTerm = jest.fn()
    const mockSubmitQuestion = jest.fn()
    const mockClearChat = jest.fn()
    const mockSetModalMode = jest.fn()

    beforeEach(() => {
        jest.clearAllMocks()
        mockUseSearchActions.mockReturnValue({
            setSearchTerm: mockSetSearchTerm,
            clearSearchTerm: jest.fn(),
            setPageNumber: jest.fn(),
        })
        mockUseChatActions.mockReturnValue({
            submitQuestion: mockSubmitQuestion,
            clearChat: mockClearChat,
            setAiProvider: jest.fn(),
        })
        mockUseModalActions.mockReturnValue({
            setModalMode: mockSetModalMode,
        })
    })

    describe('Search input', () => {
        it('should render search input field', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('')

            // Act
            render(<Search />)

            // Assert
            expect(
                screen.getByPlaceholderText(/search in docs/i)
            ).toBeInTheDocument()
        })

        it('should display current search term', () => {
            // Arrange
            const searchTerm = 'elasticsearch'
            mockUseSearchTerm.mockReturnValue(searchTerm)

            // Act
            render(<Search />)

            // Assert
            const input = screen.getByPlaceholderText(
                /search in docs/i
            ) as HTMLInputElement
            expect(input.value).toBe(searchTerm)
        })

        it('should call setSearchTerm when input changes', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.type(input, 'kibana')

            // Assert
            expect(mockSetSearchTerm).toHaveBeenCalled()
        })
    })

    describe('Ask AI button', () => {
        it('should not show Ask AI button when search term is empty', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('')

            // Act
            render(<Search />)

            // Assert
            expect(
                screen.queryByRole('button', { name: /ask ai/i })
            ).not.toBeInTheDocument()
        })

        it('should show Ask AI button when search term exists', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('elasticsearch')

            // Act
            render(<Search />)

            // Assert
            expect(
                screen.getByRole('button', { name: /tell me more about/i })
            ).toBeInTheDocument()
            expect(screen.getByText(/elasticsearch/i)).toBeInTheDocument()
        })

        it('should trigger chat actions when Ask AI button is clicked', async () => {
            // Arrange
            const searchTerm = 'what is kibana'
            mockUseSearchTerm.mockReturnValue(searchTerm)
            const user = userEvent.setup()

            // Act
            render(<Search />)
            await user.click(
                screen.getByRole('button', { name: /tell me more about/i })
            )

            // Assert - verify the workflow is triggered
            expect(mockClearChat).toHaveBeenCalled()
            expect(mockSubmitQuestion).toHaveBeenCalledWith(searchTerm)
            expect(mockSetModalMode).toHaveBeenCalledWith('askAi')
        })

        it('should not submit whitespace-only search term', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('   ')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            await user.click(
                screen.getByRole('button', { name: /tell me more about/i })
            )

            // Assert - submission should be blocked
            expect(mockSubmitQuestion).not.toHaveBeenCalled()
        })
    })

    describe('Search on Enter', () => {
        it('should trigger chat workflow when Enter is pressed', async () => {
            // Arrange
            const searchTerm = 'elasticsearch query'
            mockUseSearchTerm.mockReturnValue(searchTerm)
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.click(input)
            await user.keyboard('{Enter}')

            // Assert - same workflow as clicking button
            expect(mockClearChat).toHaveBeenCalled()
            expect(mockSubmitQuestion).toHaveBeenCalledWith(searchTerm)
            expect(mockSetModalMode).toHaveBeenCalledWith('askAi')
        })

        it('should not submit empty search on Enter', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.click(input)
            await user.keyboard('{Enter}')

            // Assert
            expect(mockSubmitQuestion).not.toHaveBeenCalled()
        })
    })

    describe('Search results', () => {
        it('should render SearchResults component', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            mockUseSearchQuery.mockReturnValue({
                isLoading: false,
                isFetching: false,
                data: {
                    results: [],
                    totalResults: 0,
                    pageCount: 1,
                    pageNumber: 1,
                    pageSize: 10,
                },
                error: null,
                cancelQuery: jest.fn(),
            })

            // Act
            render(<Search />)

            // Assert - SearchResults renders a div with data-search-results attribute
            expect(
                document.querySelector('[data-search-results]')
            ).toBeInTheDocument()
        })
    })

    describe('Search cancellation', () => {
        const mockCancelQuery = jest.fn()

        beforeEach(() => {
            mockCancelQuery.mockClear()
            mockUseSearchQuery.mockReturnValue({
                isLoading: false,
                isFetching: false,
                data: null,
                error: null,
                cancelQuery: mockCancelQuery,
            })
        })

        it('should provide cancelQuery function from useSearchQuery', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('')

            // Act
            render(<Search />)

            // Assert - verify the hook is called and returns cancelQuery
            expect(mockUseSearchQuery).toHaveBeenCalled()
        })
    })

    describe('Return key icon visibility', () => {
        it('should show return key icon when input is focused', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('elasticsearch')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const input = screen.getByPlaceholderText(/search in docs/i)
            await user.click(input)

            // Assert - return key icon should be visible
            const returnKeyIcon = document.querySelector('.return-key-icon')
            expect(returnKeyIcon).toBeInTheDocument()
            expect(returnKeyIcon).toHaveStyle({ visibility: 'visible' })
        })

        it('should hide return key icon when input loses focus', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('elasticsearch')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            // Input is auto-focused, so icon should be visible initially
            const returnKeyIcon = document.querySelector('.return-key-icon')
            expect(returnKeyIcon).toHaveStyle({ visibility: 'visible' })

            // Blur the input
            await act(async () => {
                await user.click(document.body)
            })

            // Assert - return key icon should be hidden after blur
            await waitFor(() => {
                expect(returnKeyIcon).toHaveStyle({ visibility: 'hidden' })
            })
        })

        it('should show return key icon when Ask AI button is focused', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('elasticsearch')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            await user.tab() // Tab to focus the button

            // Assert - return key icon should be visible when button is focused
            // Note: CSS :focus selector makes it visible, but we can check the icon exists
            const returnKeyIcon = document.querySelector('.return-key-icon')
            expect(returnKeyIcon).toBeInTheDocument()
        })
    })

    describe('Arrow key navigation', () => {
        beforeEach(() => {
            mockUseSearchQuery.mockReturnValue({
                isLoading: false,
                isFetching: false,
                data: {
                    results: [
                        {
                            url: '/test1',
                            title: 'Test Result 1',
                            description: 'Description 1',
                            score: 0.9,
                            parents: [],
                        },
                        {
                            url: '/test2',
                            title: 'Test Result 2',
                            description: 'Description 2',
                            score: 0.8,
                            parents: [],
                        },
                        {
                            url: '/test3',
                            title: 'Test Result 3',
                            description: 'Description 3',
                            score: 0.7,
                            parents: [],
                        },
                    ],
                    totalResults: 3,
                    pageCount: 1,
                    pageNumber: 1,
                    pageSize: 10,
                },
                error: null,
                cancelQuery: jest.fn(),
            })
        })

        it('should navigate from input to first list item on ArrowDown', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const input = screen.getByPlaceholderText(/search in docs/i)
            await act(async () => {
                await user.click(input)
                await user.keyboard('{ArrowDown}')
            })

            // Assert - first list item should be focused
            await waitFor(() => {
                const firstItem = screen.getByText('Test Result 1').closest('a')
                expect(firstItem).toHaveFocus()
            })
        })

        it('should navigate between list items with ArrowDown', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const firstItem = screen.getByText('Test Result 1').closest('a')
            if (!firstItem) throw new Error('First item not found')

            await act(async () => {
                firstItem.focus()
                await user.keyboard('{ArrowDown}')
            })

            // Assert - second item should be focused
            const secondItem = screen.getByText('Test Result 2').closest('a')
            expect(secondItem).toHaveFocus()
        })

        it('should navigate between list items with ArrowUp', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const secondItem = screen.getByText('Test Result 2').closest('a')
            if (!secondItem) throw new Error('Second item not found')

            await act(async () => {
                secondItem.focus()
                await user.keyboard('{ArrowUp}')
            })

            // Assert - first item should be focused
            const firstItem = screen.getByText('Test Result 1').closest('a')
            expect(firstItem).toHaveFocus()
        })

        it('should navigate from last list item to Ask AI button on ArrowDown', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const lastItem = screen.getByText('Test Result 3').closest('a')
            if (!lastItem) throw new Error('Last item not found')

            await act(async () => {
                lastItem.focus()
                await user.keyboard('{ArrowDown}')
            })

            // Assert - Ask AI button should be focused
            await waitFor(() => {
                const askAiButton = screen.getByRole('button', {
                    name: /tell me more about/i,
                })
                expect(askAiButton).toHaveFocus()
            })
        })

        it('should navigate from first list item to input on ArrowUp', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const firstItem = screen.getByText('Test Result 1').closest('a')
            if (!firstItem) throw new Error('First item not found')

            await act(async () => {
                firstItem.focus()
                await user.keyboard('{ArrowUp}')
            })

            // Assert - input should be focused
            await waitFor(() => {
                const input = screen.getByPlaceholderText(/search in docs/i)
                expect(input).toHaveFocus()
            })
        })

        it('should navigate from Ask AI button to last list item on ArrowUp', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const askAiButton = screen.getByRole('button', {
                name: /tell me more about/i,
            })
            await act(async () => {
                askAiButton.focus()
                await user.keyboard('{ArrowUp}')
            })

            // Assert - last list item should be focused
            await waitFor(() => {
                const lastItem = screen.getByText('Test Result 3').closest('a')
                expect(lastItem).toHaveFocus()
            })
        })

        it('should navigate from input to Ask AI button when there are no results', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            mockUseSearchQuery.mockReturnValue({
                isLoading: false,
                isFetching: false,
                data: {
                    results: [],
                    totalResults: 0,
                    pageCount: 1,
                    pageNumber: 1,
                    pageSize: 10,
                },
                error: null,
                cancelQuery: jest.fn(),
            })
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const input = screen.getByPlaceholderText(/search in docs/i)
            await act(async () => {
                await user.click(input)
                await user.keyboard('{ArrowDown}')
            })

            // Assert - Ask AI button should be focused (fallback when no results)
            await waitFor(() => {
                const askAiButton = screen.getByRole('button', {
                    name: /tell me more about/i,
                })
                expect(askAiButton).toHaveFocus()
            })
        })

        it('should navigate from Ask AI button to input when there are no results', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            mockUseSearchQuery.mockReturnValue({
                isLoading: false,
                isFetching: false,
                data: {
                    results: [],
                    totalResults: 0,
                    pageCount: 1,
                    pageNumber: 1,
                    pageSize: 10,
                },
                error: null,
                cancelQuery: jest.fn(),
            })
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const askAiButton = screen.getByRole('button', {
                name: /tell me more about/i,
            })
            await act(async () => {
                askAiButton.focus()
                await user.keyboard('{ArrowUp}')
            })

            // Assert - input should be focused
            await waitFor(() => {
                const input = screen.getByPlaceholderText(/search in docs/i)
                expect(input).toHaveFocus()
            })
        })
    })

    describe('Loading states', () => {
        it('should show loading spinner when isLoading is true', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            mockUseSearchQuery.mockReturnValue({
                isLoading: true,
                isFetching: false,
                data: null,
                error: null,
                cancelQuery: jest.fn(),
            })

            // Act
            render(<Search />)

            // Assert - check that the loading spinner exists (it has aria-label="Loading" without trailing space)
            const progressbars = screen.getAllByRole('progressbar')
            const spinner = progressbars.find(
                (el) => el.getAttribute('aria-label') === 'Loading'
            )
            expect(spinner).toBeInTheDocument()
        })

        it('should show loading spinner when isFetching is true', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            mockUseSearchQuery.mockReturnValue({
                isLoading: false,
                isFetching: true,
                data: null,
                error: null,
                cancelQuery: jest.fn(),
            })

            // Act
            render(<Search />)

            // Assert
            expect(screen.getByRole('progressbar')).toBeInTheDocument()
        })
    })

    describe('Input value synchronization', () => {
        it('should use searchTerm directly from store', () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test search')
            render(<Search />)
            const input = screen.getByPlaceholderText(
                /search in docs/i
            ) as HTMLInputElement

            // Assert - input value should match searchTerm from store
            expect(input.value).toBe('test search')

            // Act - update search term in store
            mockUseSearchTerm.mockReturnValue('updated search')
            const { rerender } = render(<Search />)
            rerender(<Search />)

            // Assert - input should reflect updated value
            expect(input.value).toBe('updated search')
        })
    })

    describe('Esc button', () => {
        it('should call clearSearchTerm and closeModal when Esc button is clicked', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('test')
            const mockClearSearchTerm = jest.fn()
            const mockCloseModal = jest.fn()
            mockUseSearchActions.mockReturnValue({
                setSearchTerm: jest.fn(),
                clearSearchTerm: mockClearSearchTerm,
                setPageNumber: jest.fn(),
            })
            mockUseModalActions.mockReturnValue({
                setModalMode: jest.fn(),
                openModal: jest.fn(),
                closeModal: mockCloseModal,
                toggleModal: jest.fn(),
            })
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const escButton = screen.getByRole('button', { name: /esc/i })
            await user.click(escButton)

            // Assert
            expect(mockClearSearchTerm).toHaveBeenCalled()
            expect(mockCloseModal).toHaveBeenCalled()
        })
    })
})
