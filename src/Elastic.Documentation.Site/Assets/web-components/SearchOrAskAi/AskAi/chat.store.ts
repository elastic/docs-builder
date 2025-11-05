import { v4 as uuidv4 } from 'uuid'
import { create } from 'zustand/react'
import { ApiError, isRateLimitError } from '../errorHandling'

export interface ChatMessage {
    id: string
    type: 'user' | 'ai'
    content: string
    threadId: string
    timestamp: number
    status?: 'streaming' | 'complete' | 'error'
    question?: string // For AI messages, store the question
    error?: ApiError | Error | null
}

// Track which AI messages have had their requests sent (persists across remounts)
const sentAiMessageIds = new Set<string>()

interface ChatState {
    chatMessages: ChatMessage[]
    threadId: string | null
    actions: {
        submitQuestion: (question: string) => void
        updateAiMessage: (
            id: string,
            content: string,
            status: ChatMessage['status'],
            error?: ApiError | Error | null
        ) => void
        setThreadId: (threadId: string) => void
        clearChat: () => void
        clearNon429Errors: () => void
        hasMessageBeenSent: (id: string) => boolean
        markMessageAsSent: (id: string) => void
    }
}

export const chatStore = create<ChatState>((set) => ({
    chatMessages: [],
    threadId: null, // Start with null - will be set by backend on first request
    actions: {
        submitQuestion: (question: string) => {
            set((state) => {
                const userMessage: ChatMessage = {
                    id: uuidv4(),
                    type: 'user',
                    content: question,
                    threadId: state.threadId ?? '',
                    timestamp: Date.now(),
                }

                const aiMessage: ChatMessage = {
                    id: uuidv4(),
                    type: 'ai',
                    content: '',
                    question,
                    threadId: state.threadId ?? '',
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

        setThreadId: (threadId: string) => {
            set({ threadId })
        },

        clearChat: () => {
            sentAiMessageIds.clear()
            set({ chatMessages: [], threadId: null })
        },

        clearNon429Errors: () => {
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) => {
                    if (
                        msg.status === 'error' &&
                        msg.error &&
                        !isRateLimitError(msg.error)
                    ) {
                        return { ...msg, status: 'complete', error: null }
                    }
                    return msg
                }),
            }))
        },

        hasMessageBeenSent: (id: string) => sentAiMessageIds.has(id),

        markMessageAsSent: (id: string) => {
            sentAiMessageIds.add(id)
        },
    },
}))

export const useChatMessages = () => chatStore((state) => state.chatMessages)
export const useThreadId = () => chatStore((state) => state.threadId)
export const useChatActions = () => chatStore((state) => state.actions)
