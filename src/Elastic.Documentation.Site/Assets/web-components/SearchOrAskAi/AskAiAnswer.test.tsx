import { AskAiAnswer } from './AskAiAnswer'
import { render, screen, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import * as React from 'react'

// Mock the dependencies
const mockSendQuestion = jest.fn(() => Promise.resolve())
const mockRetry = jest.fn()

jest.mock('./search.store', () => ({
    useAskAiTerm: jest.fn(() => 'What is Elasticsearch?'),
}))

jest.mock('./useLlmGateway', () => ({
    useLlmGateway: jest.fn(() => ({
        messages: [],
        error: null,
        retry: mockRetry,
        sendQuestion: mockSendQuestion,
    })),
}))

// Mock uuid
jest.mock('uuid', () => ({
    v4: jest.fn(() => 'mock-uuid-123'),
}))

describe('AskAiAnswer Component', () => {
    const {
        useLlmGateway,
    } = require('./useLlmGateway')

    beforeEach(() => {
        jest.clearAllMocks()
    })

    describe('Initial Loading State', () => {
        test('should show loading spinner when no messages are present', () => {
            // Arrange
            useLlmGateway.mockReturnValue({
                messages: [],
                error: null,
                retry: mockRetry,
                sendQuestion: mockSendQuestion,
            })

            // Act
            render(<AskAiAnswer />)

            // Assert
            const loadingSpinner = screen.getByRole('progressbar')
            expect(loadingSpinner).toBeInTheDocument()
            expect(screen.getByText('Generating...')).toBeInTheDocument()
        })
    })

    describe('Message Display', () => {
        test('should display AI message content correctly', () => {
            // Arrange
            const mockMessages = [
                {
                    type: 'ai_message',
                    data: {
                        content:
                            'Elasticsearch is a distributed search engine...',
                    },
                },
                {
                    type: 'ai_message_chunk',
                    data: {
                        content: ' It provides real-time search capabilities.',
                    },
                },
            ]

            useLlmGateway.mockReturnValue({
                messages: mockMessages,
                error: null,
                retry: mockRetry,
                sendQuestion: mockSendQuestion,
            })

            // Act
            render(<AskAiAnswer />)

            // Assert
            const expectedContent =
                'Elasticsearch is a distributed search engine... It provides real-time search capabilities.'
            expect(screen.getByText(expectedContent)).toBeInTheDocument()
        })
    })

    describe('Error State', () => {
        test('should display error message when there is an error', () => {
            // Arrange
            useLlmGateway.mockReturnValue({
                messages: [],
                error: new Error('Network error'),
                retry: mockRetry,
                sendQuestion: mockSendQuestion,
            })

            // Act
            render(<AskAiAnswer />)

            // Assert
            expect(
                screen.getByText('Sorry, there was an error')
            ).toBeInTheDocument()
            expect(
                screen.getByText(
                    'The Elastic Docs AI Assistant encountered an error. Please try again.'
                )
            ).toBeInTheDocument()
        })
    })

    describe('Finished State with Feedback Buttons', () => {
        test('should show feedback buttons when answer is finished', () => {
            // Arrange
            let onMessageCallback: (message: any) => void = () => {}

            const mockMessages = [
                {
                    type: 'ai_message',
                    data: {
                        content: 'Here is your answer about Elasticsearch.',
                    },
                },
            ]

            useLlmGateway.mockImplementation(({ onMessage }) => {
                onMessageCallback = onMessage
                return {
                    messages: mockMessages,
                    error: null,
                    retry: mockRetry,
                    sendQuestion: mockSendQuestion,
                }
            })

            // Act
            render(<AskAiAnswer />)

            // Simulate the component receiving an 'agent_end' message to finish loading
            act(() => {
                onMessageCallback({ type: 'agent_end' })
            })

            // Assert
            expect(
                screen.getByLabelText('This answer was helpful')
            ).toBeInTheDocument()
            expect(
                screen.getByLabelText('This answer was not helpful')
            ).toBeInTheDocument()
            expect(
                screen.getByLabelText('Request a new answer')
            ).toBeInTheDocument()
        })

        test('should call retry function when refresh button is clicked', async () => {
            // Arrange
            const user = userEvent.setup()
            let onMessageCallback: (message: any) => void = () => {}

            const mockMessages = [
                {
                    type: 'ai_message',
                    data: { content: 'Here is your answer.' },
                },
            ]

            useLlmGateway.mockImplementation(({ onMessage }) => {
                onMessageCallback = onMessage
                return {
                    messages: mockMessages,
                    error: null,
                    retry: mockRetry,
                    sendQuestion: mockSendQuestion,
                }
            })

            render(<AskAiAnswer />)

            // Simulate finished state
            act(() => {
                onMessageCallback({ type: 'agent_end' })
            })

            // Act
            const refreshButton = screen.getByLabelText('Request a new answer')

            await act(async () => {
                await user.click(refreshButton)
            })

            // Assert
            expect(mockRetry).toHaveBeenCalledTimes(1)
        })
    })

    describe('Question Sending', () => {
        test('should send question on component mount', () => {
            // Arrange
            useLlmGateway.mockReturnValue({
                messages: [],
                error: null,
                retry: mockRetry,
                sendQuestion: mockSendQuestion,
            })

            // Act
            render(<AskAiAnswer />)

            // Assert
            expect(mockSendQuestion).toHaveBeenCalledWith(
                'What is Elasticsearch?'
            )
        })
    })
})
