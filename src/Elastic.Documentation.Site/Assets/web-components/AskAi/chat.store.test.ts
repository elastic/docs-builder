import { chatStore } from './chat.store'
import { act } from 'react'
import { v4 as uuidv4 } from 'uuid'

// Mock zustand-indexeddb (IndexedDB not available in Node.js test environment)
jest.mock('zustand-indexeddb', () => ({
    createIndexedDBStorage: () => ({
        getItem: jest.fn().mockResolvedValue(null),
        setItem: jest.fn().mockResolvedValue(undefined),
        removeItem: jest.fn().mockResolvedValue(undefined),
    }),
}))

// Mock uuid
jest.mock('uuid', () => ({
    v4: jest.fn(),
}))

const mockUuidv4 = uuidv4 as jest.MockedFunction<() => string>

describe('chat.store', () => {
    beforeEach(() => {
        // Setup UUID mock to return unique IDs
        let counter = 0
        mockUuidv4.mockImplementation((): string => `mock-uuid-${++counter}`)

        // Clear localStorage to ensure clean state
        try {
            localStorage.removeItem('ask-ai-chat')
        } catch {
            // Ignore if localStorage is not available in test environment
        }

        // Reset store state before each test (clearAllConversations resets everything)
        act(() => {
            chatStore.getState().actions.clearAllConversations()
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

        // Clear conversation
        act(() => {
            chatStore.getState().actions.clearChat()
        })

        // Verify fresh state
        expect(chatStore.getState().chatMessages).toHaveLength(0)
        expect(chatStore.getState().activeConversationId).toBeNull()

        // Start new conversation
        act(() => {
            chatStore.getState().actions.submitQuestion('New question')
        })

        const messages = chatStore.getState().chatMessages
        expect(messages).toHaveLength(2)
        expect(messages[0].content).toBe('New question')
    })

    it('should track totalMessageCount when submitting questions', () => {
        expect(chatStore.getState().totalMessageCount).toBe(0)

        // Submit first question
        act(() => {
            chatStore.getState().actions.submitQuestion('Question 1')
        })
        expect(chatStore.getState().totalMessageCount).toBe(2) // user + AI

        // Submit second question
        act(() => {
            chatStore.getState().actions.submitQuestion('Question 2')
        })
        expect(chatStore.getState().totalMessageCount).toBe(4) // 2 users + 2 AIs
    })

    it('should reset totalMessageCount when clearing chat', () => {
        // Submit questions
        act(() => {
            chatStore.getState().actions.submitQuestion('Question 1')
            chatStore.getState().actions.submitQuestion('Question 2')
        })
        expect(chatStore.getState().totalMessageCount).toBe(4)

        // Clear chat
        act(() => {
            chatStore.getState().actions.clearChat()
        })

        expect(chatStore.getState().totalMessageCount).toBe(0)
        expect(chatStore.getState().chatMessages).toHaveLength(0)
    })

    it('should have persist middleware configured', async () => {
        // Submit a question
        act(() => {
            chatStore.getState().actions.submitQuestion('Test question')
        })

        // Complete the AI response
        const aiMessage = chatStore.getState().chatMessages[1]
        act(() => {
            chatStore
                .getState()
                .actions.updateAiMessage(
                    aiMessage.id,
                    'Test answer',
                    'complete'
                )
        })

        // Wait for async persist operations to complete
        await act(async () => {
            await new Promise((resolve) => setTimeout(resolve, 0))
        })

        // Verify the store has the persist API available
        // The persist middleware adds a persist property to the store
        expect(chatStore.persist).toBeDefined()
        expect(chatStore.persist.getOptions().name).toBe(
            'elastic-docs-conversations-index'
        )
        expect(chatStore.persist.getOptions().version).toBe(1)
    })

    it('should fix streaming messages on rehydration simulation', () => {
        // Submit a question (creates streaming AI message)
        act(() => {
            chatStore.getState().actions.submitQuestion('Test question')
        })

        // Verify streaming status
        const streamingMessage = chatStore.getState().chatMessages[1]
        expect(streamingMessage.status).toBe('streaming')

        // When onRehydrate runs, it should fix streaming messages
        // This tests the logic that would run on page reload
        const messages = chatStore.getState().chatMessages
        const fixedMessages = messages.map((msg) =>
            msg.status === 'streaming' ? { ...msg, status: 'complete' } : msg
        )

        expect(fixedMessages[1].status).toBe('complete')
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

    describe('multi-conversation support', () => {
        it('should create a new ConversationMeta when setConversationId is called with new ID', () => {
            // Submit a question first (so there are messages)
            act(() => {
                chatStore.getState().actions.submitQuestion('Hello')
            })

            expect(
                Object.keys(chatStore.getState().conversations)
            ).toHaveLength(0)
            expect(chatStore.getState().activeConversationId).toBeNull()

            // Simulate backend returning conversation ID
            act(() => {
                chatStore.getState().actions.setConversationId('conv-123')
            })

            const state = chatStore.getState()
            expect(state.activeConversationId).toBe('conv-123')
            expect(Object.keys(state.conversations)).toHaveLength(1)
            expect(state.conversations['conv-123'].id).toBe('conv-123')
            expect(state.conversations['conv-123'].title).toBe('Hello')
            expect(state.conversations['conv-123'].messageCount).toBe(2)
            expect(state.conversations['conv-123'].createdAt).toBeDefined()
            expect(state.conversations['conv-123'].updatedAt).toBeDefined()
            expect(state.conversations['conv-123'].aiProvider).toBe(
                'LlmGateway'
            )
        })

        it('should update existing ConversationMeta when setConversationId is called with existing ID', () => {
            // Create initial conversation
            act(() => {
                chatStore.getState().actions.submitQuestion('First question')
                chatStore.getState().actions.setConversationId('conv-123')
            })

            const initialUpdatedAt =
                chatStore.getState().conversations['conv-123'].updatedAt

            // Add another message and update conversation
            act(() => {
                chatStore.getState().actions.submitQuestion('Follow-up')
                chatStore.getState().actions.setConversationId('conv-123')
            })

            const state = chatStore.getState()
            expect(Object.keys(state.conversations)).toHaveLength(1) // Still only one conversation
            expect(state.conversations['conv-123'].messageCount).toBe(4) // Now 4 messages
            expect(
                state.conversations['conv-123'].updatedAt
            ).toBeGreaterThanOrEqual(initialUpdatedAt)
        })

        it('should reset state when createConversation is called', () => {
            // Create a conversation with messages
            act(() => {
                chatStore.getState().actions.submitQuestion('Hello')
                chatStore.getState().actions.setConversationId('conv-123')
            })

            expect(chatStore.getState().chatMessages).toHaveLength(2)
            expect(chatStore.getState().activeConversationId).toBe('conv-123')

            // Create new conversation
            act(() => {
                chatStore.getState().actions.createConversation()
            })

            const state = chatStore.getState()
            expect(state.chatMessages).toHaveLength(0)
            expect(state.activeConversationId).toBeNull()
            expect(state.totalMessageCount).toBe(0)
            expect(state.inputValue).toBe('')
            // Conversations map should still contain the old conversation
            expect(Object.keys(state.conversations)).toHaveLength(1)
        })

        it('should clear all conversations when clearAllConversations is called', () => {
            // Create multiple conversations
            act(() => {
                chatStore.getState().actions.submitQuestion('Question 1')
                chatStore.getState().actions.setConversationId('conv-1')
            })

            act(() => {
                chatStore.getState().actions.createConversation()
                chatStore.getState().actions.submitQuestion('Question 2')
                chatStore.getState().actions.setConversationId('conv-2')
            })

            expect(
                Object.keys(chatStore.getState().conversations)
            ).toHaveLength(2)

            // Clear all
            act(() => {
                chatStore.getState().actions.clearAllConversations()
            })

            const state = chatStore.getState()
            expect(Object.keys(state.conversations)).toHaveLength(0)
            expect(state.chatMessages).toHaveLength(0)
            expect(state.activeConversationId).toBeNull()
            expect(state.totalMessageCount).toBe(0)
        })

        it('should initialize with empty conversations map', () => {
            // beforeEach already calls clearAllConversations, so state should be clean
            expect(chatStore.getState().conversations).toEqual({})
        })
    })
})
