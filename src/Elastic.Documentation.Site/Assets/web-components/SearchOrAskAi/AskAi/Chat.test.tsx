import { cooldownStore } from '../cooldown.store'
import { modalStore } from '../modal.store'
import { Chat } from './Chat'
import { chatStore } from './chat.store'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

// Mock only external HTTP calls - fetchEventSource makes the actual API request
jest.mock('@microsoft/fetch-event-source', () => ({
    fetchEventSource: jest.fn(),
    EventStreamContentType: 'text/event-stream',
}))

// Helper to reset all stores to initial state
const resetStores = () => {
    chatStore.setState({
        chatMessages: [],
        conversationId: null,
        aiProvider: 'LlmGateway',
        scrollPosition: 0,
    })
    modalStore.setState({
        isOpen: false,
        mode: 'search',
    })
    cooldownStore.setState({
        cooldowns: {
            search: { cooldown: null, awaitingNewInput: false },
            askAi: { cooldown: null, awaitingNewInput: false },
        },
    })
}

describe('Chat Component', () => {
    beforeEach(() => {
        jest.clearAllMocks()
        resetStores()
    })

    describe('Empty state', () => {
        it('should show empty prompt when no messages', () => {
            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.getByText(/Hi! I'm the Elastic Docs AI Assistant/i)
            ).toBeInTheDocument()
            expect(
                screen.getByText(
                    /I'm here to help you find answers about Elastic/i
                )
            ).toBeInTheDocument()
        })

        it('should show example questions when no messages', () => {
            // Act
            render(<Chat />)

            // Assert
            expect(screen.getByText(/Example questions/i)).toBeInTheDocument()
        })

        it('should not show "Clear conversation" button when no messages', () => {
            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.queryByRole('button', { name: /clear conversation/i })
            ).not.toBeInTheDocument()
        })
    })

    describe('With messages', () => {
        const setupMessages = () => {
            chatStore.setState({
                chatMessages: [
                    {
                        id: '1',
                        type: 'user',
                        content: 'What is Elasticsearch?',
                        conversationId: 'thread-1',
                        timestamp: Date.now(),
                    },
                    {
                        id: '2',
                        type: 'ai',
                        content:
                            'Elasticsearch is a distributed search engine...',
                        conversationId: 'thread-1',
                        timestamp: Date.now(),
                        status: 'complete',
                    },
                ],
                conversationId: 'thread-1',
            })
        }

        it('should show messages when there are messages', () => {
            // Arrange
            setupMessages()

            // Act
            render(<Chat />)

            // Assert - real messages should be rendered
            expect(
                screen.getByText('What is Elasticsearch?')
            ).toBeInTheDocument()
            expect(
                screen.getByText(
                    /Elasticsearch is a distributed search engine/i
                )
            ).toBeInTheDocument()
            expect(
                screen.queryByText(/Hi! I'm the Elastic Docs AI Assistant/i)
            ).not.toBeInTheDocument()
        })

        it('should show "Clear conversation" button when there are messages', () => {
            // Arrange
            setupMessages()

            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.getByRole('button', { name: /clear conversation/i })
            ).toBeInTheDocument()
        })

        it('should clear messages when "Clear conversation" is clicked', async () => {
            // Arrange
            setupMessages()
            const user = userEvent.setup()

            // Act
            render(<Chat />)
            await user.click(
                screen.getByRole('button', { name: /clear conversation/i })
            )

            // Assert - messages should be cleared
            await waitFor(() => {
                expect(chatStore.getState().chatMessages).toHaveLength(0)
            })
        })
    })

    describe('Input and submission', () => {
        it('should render input field', () => {
            // Act
            render(<Chat />)

            // Assert
            expect(
                screen.getByPlaceholderText(
                    /Ask the Elastic Docs AI Assistant/i
                )
            ).toBeInTheDocument()
        })

        it('should add user message to store when question is submitted', async () => {
            // Arrange
            const user = userEvent.setup()
            const question = 'What is Kibana?'

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask the Elastic Docs AI Assistant/i
            )
            await user.type(input, question)
            await user.keyboard('{Enter}')

            // Assert - user message should be in the store
            await waitFor(() => {
                const messages = chatStore.getState().chatMessages
                expect(messages.length).toBeGreaterThanOrEqual(1)
                expect(messages[0].type).toBe('user')
                expect(messages[0].content).toBe(question)
            })
        })

        it('should add user message when Send button is clicked', async () => {
            // Arrange
            const user = userEvent.setup()
            const question = 'What is Kibana?'

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask the Elastic Docs AI Assistant/i
            )
            await user.type(input, question)
            await user.click(
                screen.getByRole('button', { name: /send message/i })
            )

            // Assert
            await waitFor(() => {
                const messages = chatStore.getState().chatMessages
                expect(messages[0].content).toBe(question)
            })
        })

        it('should not submit empty question', async () => {
            // Arrange
            const user = userEvent.setup()

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask the Elastic Docs AI Assistant/i
            )
            await user.type(input, '   ')
            await user.keyboard('{Enter}')

            // Assert - no messages should be added
            expect(chatStore.getState().chatMessages).toHaveLength(0)
        })

        it('should clear input after submission', async () => {
            // Arrange
            const user = userEvent.setup()

            // Act
            render(<Chat />)
            const input = screen.getByPlaceholderText(
                /Ask the Elastic Docs AI Assistant/i
            ) as HTMLTextAreaElement
            await user.type(input, 'test question')
            await user.keyboard('{Enter}')

            // Assert
            await waitFor(() => {
                expect(input.value).toBe('')
            })
        })
    })

    describe('Close modal', () => {
        it('should close modal when close button is clicked', async () => {
            // Arrange
            modalStore.setState({ isOpen: true })
            const user = userEvent.setup()

            // Act
            render(<Chat />)
            await user.click(
                screen.getByRole('button', { name: /close ask ai modal/i })
            )

            // Assert
            expect(modalStore.getState().isOpen).toBe(false)
        })
    })
})
