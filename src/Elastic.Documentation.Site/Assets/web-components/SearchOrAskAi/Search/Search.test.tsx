import { Search } from './Search'
import { render, screen } from '@testing-library/react'
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
    useSearchActions: jest.fn(() => ({
        setSearchTerm: jest.fn(),
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
    })),
    useIsSearchCooldownActive: jest.fn(() => false),
    useIsAskAiCooldownActive: jest.fn(() => false),
}))

jest.mock('./SearchResults', () => ({
    SearchResults: () => <div data-testid="search-results">Search Results</div>,
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
                screen.getByPlaceholderText(/search the docs as you type/i)
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
                /search the docs as you type/i
            ) as HTMLInputElement
            expect(input.value).toBe(searchTerm)
        })

        it('should call setSearchTerm when input changes', async () => {
            // Arrange
            mockUseSearchTerm.mockReturnValue('')
            const user = userEvent.setup()

            // Act
            render(<Search />)
            const input = screen.getByPlaceholderText(
                /search the docs as you type/i
            )
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
                screen.getByRole('button', { name: /ask ai about/i })
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
                screen.getByRole('button', { name: /ask ai about/i })
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
                screen.getByRole('button', { name: /ask ai about/i })
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
            const input = screen.getByPlaceholderText(
                /search the docs as you type/i
            )
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
            const input = screen.getByPlaceholderText(
                /search the docs as you type/i
            )
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

            // Act
            render(<Search />)

            // Assert
            expect(screen.getByTestId('search-results')).toBeInTheDocument()
        })
    })
})
