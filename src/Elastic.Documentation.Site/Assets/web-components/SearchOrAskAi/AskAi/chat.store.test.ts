import { chatStore } from './chat.store'
import { act } from 'react'
import { v4 as uuidv4 } from 'uuid'

// Mock uuid
jest.mock('uuid', () => ({
    v4: jest.fn(),
}))

const mockUuidv4 = uuidv4 as jest.MockedFunction<typeof uuidv4>

describe('chat.store', () => {
    beforeEach(() => {
        // Setup UUID mock to return unique IDs
        let counter = 0
        mockUuidv4.mockImplementation(() => `mock-uuid-${++counter}` as string)

        // Reset store state before each test
        act(() => {
            chatStore.getState().actions.clearChat()
        })
    })

    it('should support a complete chat conversation flow', () => {
        // Submit first question
        act(() => {
            chatStore
                .getState()
                .actions.submitQuestion('What is Elasticsearch?')
        })

        let messages = chatStore.getState().chatMessages
        expect(messages).toHaveLength(2) // user + AI (streaming)
        expect(messages[0].type).toBe('user')
        expect(messages[0].content).toBe('What is Elasticsearch?')
        expect(messages[1].type).toBe('ai')
        expect(messages[1].status).toBe('streaming')

        // AI completes response
        const firstAiMessage = messages[1]
        act(() => {
            chatStore
                .getState()
                .actions.updateAiMessage(
                    firstAiMessage.id,
                    'Elasticsearch is a distributed search engine...',
                    'complete'
                )
        })

        messages = chatStore.getState().chatMessages
        expect(messages[1].content).toBe(
            'Elasticsearch is a distributed search engine...'
        )
        expect(messages[1].status).toBe('complete')

        // Submit follow-up question
        act(() => {
            chatStore
                .getState()
                .actions.submitQuestion('Tell me more about shards')
        })

        messages = chatStore.getState().chatMessages
        expect(messages).toHaveLength(4) // 2 user + 2 AI messages
        expect(messages[2].content).toBe('Tell me more about shards')
        expect(messages[3].status).toBe('streaming')

        // Verify first AI message unchanged
        expect(messages[1].content).toBe(
            'Elasticsearch is a distributed search engine...'
        )
        expect(messages[1].status).toBe('complete')
    })

    it('should clear conversation and start fresh', () => {
        // Create a conversation
        act(() => {
            chatStore.getState().actions.submitQuestion('Question 1')
        })
        const aiMessage = chatStore.getState().chatMessages[1]
        act(() => {
            chatStore
                .getState()
                .actions.updateAiMessage(aiMessage.id, 'Answer 1', 'complete')
        })

        expect(chatStore.getState().chatMessages).toHaveLength(2)
        const oldThreadId = chatStore.getState().threadId

        // Clear conversation
        act(() => {
            chatStore.getState().actions.clearChat()
        })

        // Verify fresh state
        expect(chatStore.getState().chatMessages).toHaveLength(0)
        expect(chatStore.getState().threadId).not.toBe(oldThreadId)

        // Start new conversation
        act(() => {
            chatStore.getState().actions.submitQuestion('New question')
        })

        const messages = chatStore.getState().chatMessages
        expect(messages).toHaveLength(2)
        expect(messages[0].content).toBe('New question')
    })

    /*
     * Note: ThreadId behavior is NOT tested here.
     *
     * ThreadId is used by the LLM backend to maintain conversational context
     * across follow-up questions. Testing that we assign a threadId to messages
     * doesn't verify the actual behavior (LLM maintaining context).
     *
     * This requires system/E2E testing with a real LLM backend, which is
     * outside the scope of frontend unit tests.
     */
})
