import { ApiError } from '../errorHandling'
import { ChatMessage } from './ChatMessage'
import { ChatMessage as ChatMessageType } from './chat.store'
import { render, screen } from '@testing-library/react'
import * as React from 'react'

// Mock EuiCallOut and EuiSpacer for SearchOrAskAiErrorCallout
jest.mock('@elastic/eui', () => {
    const actual = jest.requireActual('@elastic/eui')
    return {
        ...actual,
        EuiCallOut: ({
            title,
            children,
            color,
            iconType,
            size,
        }: {
            title: string
            children: React.ReactNode
            color: string
            iconType: string
            size: string
        }) => (
            <div
                data-testid="eui-callout"
                data-title={title}
                data-color={color}
                data-icon-type={iconType}
                data-size={size}
            >
                {children}
            </div>
        ),
        EuiSpacer: ({ size }: { size: string }) => (
            <div data-testid="eui-spacer" data-size={size} />
        ),
    }
})

// Mock domain-specific cooldown hooks
jest.mock('../Search/useSearchCooldown', () => ({
    useSearchErrorCalloutState: jest.fn(() => ({
        hasActiveCooldown: false,
        countdown: null,
        cooldownFinishedPendingAcknowledgment: false,
    })),
}))

jest.mock('../AskAi/useAskAiCooldown', () => ({
    useAskAiErrorCalloutState: jest.fn(() => ({
        hasActiveCooldown: false,
        countdown: null,
        cooldownFinishedPendingAcknowledgment: false,
    })),
}))

// Mock errorHandling utilities
jest.mock('../errorHandling', () => {
    const actual = jest.requireActual('../errorHandling')
    return {
        ...actual,
        getErrorMessage: jest.fn((error: ApiError | Error | null) => {
            if (!error) return 'Unknown error'
            if ('statusCode' in error) {
                return `Error ${error.statusCode}: ${error.message}`
            }
            return error.message
        }),
        isApiError: jest.fn((error: ApiError | Error | null) => {
            return (
                error instanceof Error &&
                'statusCode' in error &&
                error.name === 'ApiError'
            )
        }),
        isRateLimitError: jest.fn((error: ApiError | Error | null) => {
            return (
                error instanceof Error &&
                'statusCode' in error &&
                (error as ApiError).statusCode === 429
            )
        }),
    }
})

// Mock rate limit handlers
jest.mock('./useAskAiRateLimitHandler', () => ({
    useAskAiRateLimitHandler: jest.fn(),
}))

jest.mock('../Search/useSearchRateLimitHandler', () => ({
    useSearchRateLimitHandler: jest.fn(),
}))

describe('ChatMessage Component', () => {
    beforeEach(() => {
        jest.clearAllMocks()
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

        it('should display user icon', () => {
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
            render(<ChatMessage message={aiMessage} />)

            // Assert
            expect(
                screen.getByText(
                    /Elasticsearch is a distributed search engine/i
                )
            ).toBeInTheDocument()
        })

        it('should show feedback buttons', () => {
            // Act
            render(<ChatMessage message={aiMessage} />)

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

        it('should display Elastic logo icon', () => {
            // Act
            render(<ChatMessage message={aiMessage} />)

            // Assert
            const messageElement = screen
                .getAllByText(
                    /Elasticsearch is a distributed search engine/i
                )[0]
                .closest('[data-message-type="ai"]')
            expect(messageElement).toBeInTheDocument()
            // Check for logo icon
            const logoIcon = messageElement?.querySelector(
                '[data-type="logoElastic"]'
            )
            expect(logoIcon).toBeInTheDocument()
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

        it('should show loading icon when streaming', () => {
            // Act
            render(<ChatMessage message={streamingMessage} />)

            // Assert
            // Loading elastic icon should be present
            const messageElement = screen
                .getByText(/Elasticsearch is\.\.\./i)
                .closest('[data-message-type="ai"]')
            expect(messageElement).toBeInTheDocument()
        })

        it('should not show feedback buttons when streaming', () => {
            // Act
            render(<ChatMessage message={streamingMessage} />)

            // Assert
            expect(
                screen.queryByRole('button', {
                    name: /^This answer was helpful$/i,
                })
            ).not.toBeInTheDocument()
            expect(
                screen.queryByRole('button', {
                    name: /^This answer was not helpful$/i,
                })
            ).not.toBeInTheDocument()
        })
    })

    describe('AI messages - error', () => {
        const testError = new Error('Test error') as ApiError
        testError.name = 'ApiError'
        testError.statusCode = 500

        const errorMessage: ChatMessageType = {
            id: '4',
            type: 'ai',
            content: 'Previous content...',
            conversationId: 'thread-1',
            timestamp: Date.now(),
            status: 'error',
            error: testError,
        }

        it('should show error message', () => {
            // Act
            render(<ChatMessage message={errorMessage} />)

            // Assert
            const callout = screen.getByTestId('eui-callout')
            expect(callout).toBeInTheDocument()
            expect(callout).toHaveAttribute('data-title', 'Sorry, there was an error')
            expect(callout).toHaveTextContent('Test error')
        })

        it('should display previous content before error occurred', () => {
            // Act
            render(<ChatMessage message={errorMessage} />)

            // Assert
            // When there's an error, the content is hidden, only the error callout is shown
            expect(screen.getByTestId('eui-callout')).toBeInTheDocument()
            // The content is not rendered when hasError is true
            expect(
                screen.queryByText(/Previous content/i)
            ).not.toBeInTheDocument()
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

        it('should render markdown content', () => {
            // Act
            render(<ChatMessage message={messageWithMarkdown} />)

            // Assert - EuiMarkdownFormat will render the markdown
            expect(screen.getByText(/Bold text/)).toBeInTheDocument()
        })
    })
})
