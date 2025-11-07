import { ApiError, isRateLimitError } from '../errorHandling'
import { v4 as uuidv4 } from 'uuid'
import { create } from 'zustand/react'

export type AiProvider = 'AgentBuilder' | 'LlmGateway'

export interface ChatMessage {
    id: string
    type: 'user' | 'ai'
    content: string
    conversationId: string
    timestamp: number
    status?: 'streaming' | 'complete' | 'error'
    question?: string // For AI messages, store the question
    error?: ApiError | Error | null
}

// Track which AI messages have had their requests sent (persists across remounts)
const sentAiMessageIds = new Set<string>()

interface ChatState {
    chatMessages: ChatMessage[]
    conversationId: string | null
    aiProvider: AiProvider
    actions: {
        submitQuestion: (question: string) => void
        updateAiMessage: (
            id: string,
            content: string,
            status: ChatMessage['status'],
            error?: ApiError | Error | null
        ) => void
        setConversationId: (conversationId: string) => void
        setAiProvider: (provider: AiProvider) => void
        clearChat: () => void
        clearNon429Errors: () => void
        hasMessageBeenSent: (id: string) => boolean
        markMessageAsSent: (id: string) => void
        cancelStreaming: () => void
    }
}

export const chatStore = create<ChatState>((set) => ({
    chatMessages: [],
    conversationId: null, // Start with null - will be set by backend on first request
    aiProvider: 'LlmGateway', // Default to LLM Gateway
    actions: {
        submitQuestion: (question: string) => {
            set((state) => {
                const userMessage: ChatMessage = {
                    id: uuidv4(),
                    type: 'user',
                    content: question,
                    conversationId: state.conversationId ?? '',
                    timestamp: Date.now(),
                }

                const aiMessage: ChatMessage = {
                    id: uuidv4(),
                    type: 'ai',
                    content: '',
                    question,
                    conversationId: state.conversationId ?? '',
                    timestamp: Date.now(),
                    status: 'streaming',
                }

                return {
                    chatMessages: [
                        ...state.chatMessages,
                        userMessage,
                        aiMessage,
                    ],
                }
            })
        },

        updateAiMessage: (
            id: string,
            content: string,
            status: ChatMessage['status'],
            error: ApiError | Error | null = null
        ) => {
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) =>
                    msg.id === id ? { ...msg, content, status, error } : msg
                ),
            }))
        },

        setConversationId: (conversationId: string) => {
            set({ conversationId })
        },

        setAiProvider: (provider: AiProvider) => {
            set({ aiProvider: provider })
        },

        clearChat: () => {
            sentAiMessageIds.clear()
            set({ chatMessages: [], conversationId: null })
        },

        clearNon429Errors: () => {
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) => {
                    if (
                        msg.status === 'error' &&
                        msg.error &&
                        !isRateLimitError(msg.error)
                    ) {
                        return { ...msg, status: 'complete', error: null, content: '' }
                    }
                    return msg
                }),
            }))
        },

        hasMessageBeenSent: (id: string) => sentAiMessageIds.has(id),

        markMessageAsSent: (id: string) => {
            sentAiMessageIds.add(id)
        },

        cancelStreaming: () => {
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) =>
                    msg.type === 'ai' && msg.status === 'streaming'
                        ? { ...msg, status: 'complete' }
                        : msg
                ),
            }))
        },
    },
}))

export const useChatMessages = () => chatStore((state) => state.chatMessages)
export const useConversationId = () =>
    chatStore((state) => state.conversationId)
export const useAiProvider = () => chatStore((state) => state.aiProvider)
export const useChatActions = () => chatStore((state) => state.actions)
