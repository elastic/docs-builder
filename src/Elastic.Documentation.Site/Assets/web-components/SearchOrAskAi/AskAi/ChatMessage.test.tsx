import { ChatMessage } from './ChatMessage'
import { ChatMessage as ChatMessageType } from './chat.store'
import { render, screen } from '@testing-library/react'
import * as React from 'react'

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
            render(<ChatMessage message={streamingMessage} llmMessages={[]} />)

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
        const errorMessage: ChatMessageType = {
            id: '4',
            type: 'ai',
            content: 'Previous content...',
            conversationId: 'thread-1',
            timestamp: Date.now(),
            status: 'error',
        }

        it('should show error message', () => {
            // Act
            render(<ChatMessage message={errorMessage} />)

            // Assert
            expect(
                screen.getByText(/Sorry, there was an error/i)
            ).toBeInTheDocument()
            expect(
                screen.getByText(
                    /The Elastic Docs AI Assistant encountered an error/i
                )
            ).toBeInTheDocument()
        })

        it('should display previous content before error occurred', () => {
            // Act
            render(<ChatMessage message={errorMessage} />)

            // Assert
            expect(screen.getByText(/Previous content/i)).toBeInTheDocument()
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
