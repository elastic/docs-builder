import { cooldownStore } from '../shared/cooldown.store'
import { ApiError } from '../shared/errorHandling'
import { ChatMessage } from './ChatMessage'
import { ChatMessage as ChatMessageType } from './chat.store'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import * as React from 'react'

// Create a fresh QueryClient for each test
const createTestQueryClient = () =>
    new QueryClient({
        defaultOptions: {
            queries: { retry: false },
            mutations: { retry: false },
        },
    })

// Wrapper component for tests that need React Query
const renderWithQueryClient = (ui: React.ReactElement) => {
    const testQueryClient = createTestQueryClient()
    return render(
        <QueryClientProvider client={testQueryClient}>{ui}</QueryClientProvider>
    )
}

// Reset cooldown store between tests
const resetStores = () => {
    cooldownStore.setState({
        cooldowns: {
            search: { cooldown: null, awaitingNewInput: false },
            askAi: { cooldown: null, awaitingNewInput: false },
        },
    })
}

describe('ChatMessage Component', () => {
    beforeEach(() => {
        resetStores()
    })

    describe('User messages', () => {
        const userMessage: ChatMessageType = {
            id: '1',
            type: 'user',
            content: 'What is Elasticsearch?',
            conversationId: 'thread-1',
            timestamp: Date.now(),
        }

        it('should render user message with correct content', () => {
            // Act
            render(<ChatMessage message={userMessage} />)

            // Assert
            expect(
                screen.getByText('What is Elasticsearch?')
            ).toBeInTheDocument()
        })

        it('should mark message as user type', () => {
            // Act
            render(<ChatMessage message={userMessage} />)

            // Assert
            const messageElement = screen
                .getByText('What is Elasticsearch?')
                .closest('[data-message-type="user"]')
            expect(messageElement).toBeInTheDocument()
        })
    })

    describe('AI messages - complete', () => {
        const aiMessage: ChatMessageType = {
            id: '2',
            type: 'ai',
            content: 'Elasticsearch is a distributed search engine...',
            conversationId: 'thread-1',
            timestamp: Date.now(),
            status: 'complete',
        }

        it('should render AI message with correct content', () => {
            // Act
            renderWithQueryClient(<ChatMessage message={aiMessage} />)

            // Assert
            expect(
                screen.getByText(
                    /Elasticsearch is a distributed search engine/i
                )
            ).toBeInTheDocument()
        })

        it('should show feedback buttons when complete', () => {
            // Act
            renderWithQueryClient(<ChatMessage message={aiMessage} />)

            // Assert
            expect(
                screen.getByRole('button', {
                    name: /^This answer was helpful$/i,
                })
            ).toBeInTheDocument()
            expect(
                screen.getByRole('button', {
                    name: /^This answer was not helpful$/i,
                })
            ).toBeInTheDocument()
        })

        it('should have correct data attributes', () => {
            // Act
            renderWithQueryClient(<ChatMessage message={aiMessage} />)

            // Assert
            const messageElement = screen
                .getAllByText(
                    /Elasticsearch is a distributed search engine/i
                )[0]
                .closest('[data-message-type="ai"]')
            expect(messageElement).toBeInTheDocument()
            expect(messageElement).toHaveAttribute('data-message-id', '2')
        })
    })

    describe('AI messages - streaming', () => {
        const streamingMessage: ChatMessageType = {
            id: '3',
            type: 'ai',
            content: 'Elasticsearch is...',
            conversationId: 'thread-1',
            timestamp: Date.now(),
            status: 'streaming',
        }

        it('should render streaming content', () => {
            // Act
            renderWithQueryClient(<ChatMessage message={streamingMessage} />)

            // Assert
            const messageElement = screen
                .getByText(/Elasticsearch is\.\.\./i)
                .closest('[data-message-type="ai"]')
            expect(messageElement).toBeInTheDocument()
        })

        it('should not show feedback buttons when streaming', () => {
            // Act
            renderWithQueryClient(<ChatMessage message={streamingMessage} />)

            // Assert
            expect(
                screen.queryByRole('button', {
                    name: /^This answer was helpful$/i,
                })
            ).not.toBeInTheDocument()
        })
    })

    describe('AI messages - error', () => {
        const createErrorMessage = (): ChatMessageType => {
            const testError = new Error('Server error') as ApiError
            testError.name = 'ApiError'
            testError.statusCode = 500

            return {
                id: '4',
                type: 'ai',
                content: '',
                conversationId: 'thread-1',
                timestamp: Date.now(),
                status: 'error',
                error: testError,
            }
        }

        it('should show error callout when status is error', () => {
            // Act
            renderWithQueryClient(
                <ChatMessage message={createErrorMessage()} />
            )

            // Assert - error callout should be visible with title
            expect(
                screen.getByText(/sorry, there was an error/i)
            ).toBeInTheDocument()
        })

        it('should display error message for 5xx errors', () => {
            // Act
            renderWithQueryClient(
                <ChatMessage message={createErrorMessage()} />
            )

            // Assert - error guidance should be displayed
            expect(
                screen.getByText(/We are unable to process your request/i)
            ).toBeInTheDocument()
        })
    })

    describe('Markdown rendering', () => {
        const messageWithMarkdown: ChatMessageType = {
            id: '5',
            type: 'ai',
            content: '# Heading\n\n**Bold text** and *italic*',
            conversationId: 'thread-1',
            timestamp: Date.now(),
            status: 'complete',
        }

        it('should render markdown as HTML', () => {
            // Act
            renderWithQueryClient(<ChatMessage message={messageWithMarkdown} />)

            // Assert - Bold text should be rendered
            expect(screen.getByText(/Bold text/)).toBeInTheDocument()
        })
    })
})
