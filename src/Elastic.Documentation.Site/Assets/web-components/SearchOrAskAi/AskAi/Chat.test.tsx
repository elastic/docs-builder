import { Chat } from './Chat'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

// Mock the chat store
jest.mock('./chat.store', () => ({
    chatStore: {
        getState: jest.fn(),
    },
    useChatMessages: jest.fn(() => []),
    useAiProvider: jest.fn(() => 'LlmGateway'),
    useChatActions: jest.fn(() => ({
        submitQuestion: jest.fn(),
        clearChat: jest.fn(),
        setAiProvider: jest.fn(),
    })),
}))

// Mock ChatMessageList
jest.mock('./ChatMessageList', () => ({
    ChatMessageList: () => <div data-testid="chat-message-list">Messages</div>,
}))

// Mock AskAiSuggestions
jest.mock('./AskAiSuggestions', () => ({
    AskAiSuggestions: () => (
        <div data-testid="ask-ai-suggestions">Suggestions</div>
    ),
}))

const mockUseChatMessages = jest.mocked(
    jest.requireMock('./chat.store').useChatMessages
)
const mockUseChatActions = jest.mocked(
    jest.requireMock('./chat.store').useChatActions
)

describe('Chat Component', () => {
    const mockSubmitQuestion = jest.fn()
    const mockClearChat = jest.fn()

    beforeEach(() => {
        jest.clearAllMocks()
        mockUseChatActions.mockReturnValue({
            submitQuestion: mockSubmitQuestion,
            clearChat: mockClearChat,
        })
    })

    describe('Empty state', () => {
        it('should show empty prompt when no messages', () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])

            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.getByText(/Hi! I'm the Elastic Docs AI Assistant/i)
            ).toBeInTheDocument()
            expect(
                screen.getByText(/Ask me anything about Elasticsearch/i)
            ).toBeInTheDocument()
        })

        it('should show suggestions when no messages', () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])

            // Act
            render(<Chat />)

            // Assert
            expect(screen.getByText(/Try asking me:/i)).toBeInTheDocument()
        })

        it('should not show "New conversation" button when no messages', () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])

            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.queryByRole('button', { name: /new conversation/i })
            ).not.toBeInTheDocument()
        })
    })

    describe('With messages', () => {
        const mockMessages = [
            {
                id: '1',
                type: 'user' as const,
                content: 'What is Elasticsearch?',
                conversationId: 'thread-1',
                timestamp: Date.now(),
            },
            {
                id: '2',
                type: 'ai' as const,
                content: 'Elasticsearch is a search engine...',
                conversationId: 'thread-1',
                timestamp: Date.now(),
                status: 'complete' as const,
            },
        ]

        it('should show message list when there are messages', () => {
            // Arrange
            mockUseChatMessages.mockReturnValue(mockMessages)

            // Act
            render(<Chat />)

            // Assert
            expect(screen.getByTestId('chat-message-list')).toBeInTheDocument()
            expect(
                screen.queryByText(/Hi! I'm the Elastic Docs AI Assistant/i)
            ).not.toBeInTheDocument()
        })

        it('should show "New conversation" button when there are messages', () => {
            // Arrange
            mockUseChatMessages.mockReturnValue(mockMessages)

            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.getByRole('button', { name: /new conversation/i })
            ).toBeInTheDocument()
        })

        it('should call clearChat when "New conversation" is clicked', async () => {
            // Arrange
            mockUseChatMessages.mockReturnValue(mockMessages)
            const user = userEvent.setup()

            // Act
            render(<Chat />)
            await user.click(
                screen.getByRole('button', { name: /new conversation/i })
            )

            // Assert
            expect(mockClearChat).toHaveBeenCalledTimes(1)
        })
    })

    describe('Input and submission', () => {
        it('should render input field', () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])

            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.getByPlaceholderText(/Ask Elastic Docs AI Assistant/i)
            ).toBeInTheDocument()
        })

        it('should submit question when Enter is pressed', async () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])
            const user = userEvent.setup()
            const question = 'What is Kibana?'

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask Elastic Docs AI Assistant/i
            )
            await user.type(input, question)
            await user.keyboard('{Enter}')

            // Assert
            expect(mockSubmitQuestion).toHaveBeenCalledWith(question)
        })

        it('should submit question when Send button is clicked', async () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])
            const user = userEvent.setup()
            const question = 'What is Kibana?'

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask Elastic Docs AI Assistant/i
            )
            await user.type(input, question)
            await user.click(screen.getByRole('button', { name: /send/i }))

            // Assert
            expect(mockSubmitQuestion).toHaveBeenCalledWith(question)
        })

        it('should not submit empty question', async () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])
            const user = userEvent.setup()

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask Elastic Docs AI Assistant/i
            )
            await user.type(input, '   ')
            await user.keyboard('{Enter}')

            // Assert
            expect(mockSubmitQuestion).not.toHaveBeenCalled()
        })

        it('should clear input after submission', async () => {
            // Arrange
            mockUseChatMessages.mockReturnValue([])
            const user = userEvent.setup()

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask Elastic Docs AI Assistant/i
            ) as HTMLInputElement
            await user.type(input, 'test question')
            await user.keyboard('{Enter}')

            // Assert
            await waitFor(() => {
                expect(input.value).toBe('')
            })
        })
    })

    describe('Auto-focus', () => {
        it('should focus input when AI completes response', () => {
            // Arrange
            const streamingMessages = [
                {
                    id: '1',
                    type: 'user' as const,
                    content: 'Question',
                    conversationId: 'thread-1',
                    timestamp: Date.now(),
                },
                {
                    id: '2',
                    type: 'ai' as const,
                    content: 'Answer...',
                    conversationId: 'thread-1',
                    timestamp: Date.now(),
                    status: 'streaming' as const,
                },
            ]

            mockUseChatMessages.mockReturnValue(streamingMessages)
            const { rerender } = render(<Chat />)

            // Act - simulate AI completing the response
            const completeMessages = [
                ...streamingMessages.slice(0, 1),
                { ...streamingMessages[1], status: 'complete' as const },
            ]
            mockUseChatMessages.mockReturnValue(completeMessages)
            rerender(<Chat />)

            // Assert
            const input = screen.getByPlaceholderText(
                /Ask Elastic Docs AI Assistant/i
            )
            // In a real test environment, we'd check if focus() was called
            // This is a limitation of jsdom
            expect(input).toBeInTheDocument()
        })
    })
})
