import { cooldownStore } from '../shared/cooldown.store'
import { Chat } from './Chat'
import { chatStore } from './chat.store'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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

        // Mock scrollTo to prevent flaky test failures in CI
        // The handleSubmit function schedules a setTimeout that calls scrollTo
        // In test environments, scrollTo may not be available on DOM elements
        HTMLElement.prototype.scrollTo = jest.fn()
    })

    afterEach(() => {
        // Clean up the mock
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        delete (HTMLElement.prototype as any).scrollTo
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
            renderWithQueryClient(<Chat />)

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
            renderWithQueryClient(<Chat />)

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
            renderWithQueryClient(<Chat />)
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

        it('should scroll to user message when question is submitted', async () => {
            // Arrange
            jest.useFakeTimers()
            try {
                const user = userEvent.setup({ delay: null })
                const question = 'What is Kibana?'
                const scrollToSpy = jest.fn()

                // Set up existing messages so scroll container exists
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

                // Act
                const { container } = renderWithQueryClient(<Chat />)

                // Wait for component to render and find scroll container
                await waitFor(() => {
                    expect(
                        screen.getByText('What is Elasticsearch?')
                    ).toBeInTheDocument()
                })

                // Find the scroll container (the div that will have messages)
                const scrollContainer =
                    (container
                        .querySelector('[data-message-type]')
                        ?.closest('div[style*="overflow"]') as HTMLElement) ||
                    (Array.from(container.querySelectorAll('div')).find(
                        (el) => {
                            const style = window.getComputedStyle(el)
                            return style.overflowY === 'auto'
                        }
                    ) as HTMLElement)

                if (scrollContainer) {
                    scrollContainer.scrollTo = scrollToSpy
                    scrollContainer.scrollTop = 0
                    // Mock getBoundingClientRect for scroll calculations
                    scrollContainer.getBoundingClientRect = jest.fn(() => ({
                        top: 0,
                        left: 0,
                        right: 100,
                        bottom: 500,
                        width: 100,
                        height: 500,
                        x: 0,
                        y: 0,
                        toJSON: jest.fn(),
                    })) as jest.MockedFunction<() => DOMRect>
                }

                const input = screen.getByPlaceholderText(
                    /Ask the Elastic Docs AI Assistant/i
                )
                await user.type(input, question)
                await user.keyboard('{Enter}')

                // Wait for message to be added
                await waitFor(() => {
                    const messages = chatStore.getState().chatMessages
                    expect(messages.length).toBeGreaterThan(2)
                    expect(messages[messages.length - 2].type).toBe('user')
                    expect(messages[messages.length - 2].content).toBe(question)
                })

                // Wait for the new user message to be rendered in the DOM
                await waitFor(() => {
                    expect(screen.getByText(question)).toBeInTheDocument()
                })

                // Mock getBoundingClientRect for the new user message
                const newUserMessage = container.querySelector(
                    '[data-message-type="user"]:last-of-type'
                ) as HTMLElement
                if (newUserMessage) {
                    newUserMessage.getBoundingClientRect = jest.fn(() => ({
                        top: 100,
                        left: 0,
                        right: 100,
                        bottom: 200,
                        width: 100,
                        height: 100,
                        x: 0,
                        y: 100,
                        toJSON: jest.fn(),
                    })) as jest.MockedFunction<() => DOMRect>
                }

                // Advance timers to trigger requestAnimationFrame
                jest.advanceTimersByTime(100)
                await Promise.resolve() // Let React process the state update

                // Run all pending requestAnimationFrame callbacks
                jest.runAllTimers()

                // Verify scrollTo was called
                if (scrollContainer) {
                    expect(scrollToSpy).toHaveBeenCalledWith(
                        expect.objectContaining({
                            top: expect.any(Number),
                            behavior: 'smooth',
                        })
                    )
                }
            } finally {
                jest.useRealTimers()
            }
        })
    })
})
