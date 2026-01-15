import { logError } from '../../telemetry/logging'
import { cooldownStore } from '../shared/cooldown.store'
import {
    ApiError,
    isApiError,
    isRateLimitError,
} from '../shared/errorHandling'
import {
    startAskAiStream,
    AiProvider,
} from '../shared/askAiStreamClient'
import { AskAiEvent } from './AskAiEvent'
import { MessageThrottler } from './MessageThrottler'
import { v4 as uuidv4 } from 'uuid'
import { create } from 'zustand/react'

export type { AiProvider }
export type Reaction = 'thumbsUp' | 'thumbsDown'

export interface ChatMessage {
    id: string
    type: 'user' | 'ai'
    content: string
    conversationId: string
    timestamp: number
    status?: 'streaming' | 'complete' | 'error'
    question?: string // For AI messages, store the question
    error?: ApiError | Error | null
    reasoning?: AskAiEvent[] // Reasoning steps for status display (search, tool calls, etc.)
}

interface ActiveStream {
    controller: AbortController
    content: string
    throttler: MessageThrottler<AskAiEvent>
}

// Active streams stored outside the reactive state (non-serializable)
const activeStreams = new Map<string, ActiveStream>()

const sentAiMessageIds = new Set<string>()

interface ChatState {
    chatMessages: ChatMessage[]
    conversationId: string | null
    aiProvider: AiProvider
    messageFeedback: Record<string, Reaction> // messageId -> reaction
    scrollPosition: number
    actions: {
        submitQuestion: (question: string) => void
        updateAiMessage: (
            id: string,
            content: string,
            status: ChatMessage['status'],
            error?: ApiError | Error | null
        ) => void
        addReasoningToMessage: (id: string, event: AskAiEvent) => void
        setConversationId: (conversationId: string) => void
        setAiProvider: (provider: AiProvider) => void
        clearChat: () => void
        clearNon429Errors: () => void
        cancelStreaming: () => void
        setMessageFeedback: (messageId: string, reaction: Reaction) => void
        abortMessage: (messageId: string) => void
        isStreaming: (messageId: string) => boolean
        setScrollPosition: (position: number) => void
    }
}

export const chatStore = create<ChatState>((set, get) => ({
    chatMessages: [],
    conversationId: null, // Start with null - will be set by backend on first request
    aiProvider: 'LlmGateway', // Default to LLM Gateway
    messageFeedback: {},
    scrollPosition: 0,
    actions: {
        submitQuestion: (question: string) => {
            const state = get()
            const aiMessageId = uuidv4()

            const userMessage: ChatMessage = {
                id: uuidv4(),
                type: 'user',
                content: question,
                conversationId: state.conversationId ?? '',
                timestamp: Date.now(),
            }

            const aiMessage: ChatMessage = {
                id: aiMessageId,
                type: 'ai',
                content: '',
                question,
                conversationId: state.conversationId ?? '',
                timestamp: Date.now(),
                status: 'streaming',
                reasoning: [],
            }

            set({
                chatMessages: [...state.chatMessages, userMessage, aiMessage],
            })

            // Start streaming (runs outside React lifecycle)
            startStream(
                aiMessageId,
                question,
                state.conversationId,
                state.aiProvider
            )
        },

        updateAiMessage: (id, content, status, error = null) => {
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) =>
                    msg.id === id ? { ...msg, content, status, error } : msg
                ),
            }))
        },

        addReasoningToMessage: (id, event) => {
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) =>
                    msg.id === id && msg.reasoning
                        ? { ...msg, reasoning: [...msg.reasoning, event] }
                        : msg
                ),
            }))
        },

        setConversationId: (conversationId) => {
            set({ conversationId })
        },

        setAiProvider: (provider) => {
            set({ aiProvider: provider })
        },

        clearChat: () => {
            // Abort all active streams and clear their throttlers
            for (const [, stream] of activeStreams) {
                stream.throttler.clear()
                stream.controller.abort()
            }
            sentAiMessageIds.clear()
            activeStreams.clear()
            set({ chatMessages: [], conversationId: null, messageFeedback: {} })
        },

        clearNon429Errors: () => {
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) => {
                    if (
                        msg.status === 'error' &&
                        msg.error &&
                        !isRateLimitError(msg.error)
                    ) {
                        return {
                            ...msg,
                            status: 'complete',
                            error: null,
                            content: '',
                        }
                    }
                    return msg
                }),
            }))
        },

        cancelStreaming: () => {
            const state = get()
            state.chatMessages.forEach((msg) => {
                if (msg.type === 'ai' && msg.status === 'streaming') {
                    const stream = activeStreams.get(msg.id)
                    if (stream) {
                        stream.throttler.clear()
                        stream.controller.abort()
                        activeStreams.delete(msg.id)
                    }
                }
            })
            set((state) => ({
                chatMessages: state.chatMessages.map((msg) =>
                    msg.type === 'ai' && msg.status === 'streaming'
                        ? { ...msg, status: 'complete' }
                        : msg
                ),
            }))
        },

        setMessageFeedback: (messageId: string, reaction: Reaction) => {
            set((state) => ({
                messageFeedback: {
                    ...state.messageFeedback,
                    [messageId]: reaction,
                },
            }))
        },

        abortMessage: (messageId) => {
            const stream = activeStreams.get(messageId)
            if (stream) {
                stream.throttler.clear()
                stream.controller.abort()
                activeStreams.delete(messageId)
            }
        },

        isStreaming: (messageId) => activeStreams.has(messageId),

        setScrollPosition: (position) => {
            set({ scrollPosition: position })
        },
    },
}))

/**
 * Start streaming for a message. Runs outside React lifecycle.
 */
async function startStream(
    messageId: string,
    question: string,
    conversationId: string | null,
    aiProvider: AiProvider
): Promise<void> {
    if (activeStreams.has(messageId)) return

    const controller = new AbortController()

    // Create a throttler for this stream to control update frequency
    // Throttler automatically speeds up after 10 seconds to drain buffers faster
    const throttler = new MessageThrottler<AskAiEvent>({
        onMessage: (event) => handleStreamEvent(messageId, event),
    })

    activeStreams.set(messageId, { controller, content: '', throttler })

    try {
        await startAskAiStream({
            message: question,
            conversationId,
            aiProvider,
            signal: controller.signal,
            callbacks: {
                onEvent: (event) => throttler.push(event),
                onError: (error) => {
                    controller.abort()
                    handleStreamError(messageId, error)
                },
                // Don't clear throttler on close - let pending events drain naturally.
                // The conversation_end event will handle cleanup when it's processed.
            },
        })
    } catch (error) {
        if (error instanceof Error && error.name !== 'AbortError') {
            handleStreamError(messageId, error as ApiError | Error)
        }
        // Only clear on actual errors, not normal close
        const stream = activeStreams.get(messageId)
        if (stream) {
            stream.throttler.clear()
            activeStreams.delete(messageId)
        }
    }
}

function handleStreamEvent(messageId: string, event: AskAiEvent): void {
    const stream = activeStreams.get(messageId)
    if (!stream) return

    const { actions } = chatStore.getState()

    // Add event to message reasoning for status display (search, tool calls, etc.)
    actions.addReasoningToMessage(messageId, event)

    if (event.type === 'conversation_start' && event.conversationId) {
        actions.setConversationId(event.conversationId)
    } else if (event.type === 'message_chunk') {
        // Start speedup timer on first content chunk
        stream.throttler.startSpeedupTimer()
        stream.content += event.content
        actions.updateAiMessage(messageId, stream.content, 'streaming')
    } else if (event.type === 'error') {
        const error = new Error(
            event.message || 'An error occurred'
        ) as ApiError
        error.statusCode = 500
        actions.updateAiMessage(
            messageId,
            event.message || 'An error occurred',
            'error',
            error
        )
        stream.throttler.clear()
        activeStreams.delete(messageId)
    } else if (event.type === 'conversation_end') {
        actions.updateAiMessage(messageId, stream.content, 'complete')
        stream.throttler.clear()
        activeStreams.delete(messageId)
    }
}

function handleStreamError(messageId: string, error: ApiError | Error): void {
    // Log real errors (not cancellations)
    if (error.name !== 'AbortError') {
        logError('AskAi stream error', {
            'error.message': error.message,
            'error.name': error.name,
            ...(isApiError(error) && { 'error.statusCode': error.statusCode }),
        })
    }

    const stream = activeStreams.get(messageId)
    const content = stream?.content || ''

    const { actions } = chatStore.getState()
    actions.updateAiMessage(
        messageId,
        content || error.message || 'Error occurred',
        'error',
        error
    )

    if (isApiError(error) && isRateLimitError(error) && error.retryAfter) {
        cooldownStore.getState().actions.setCooldown('askAi', error.retryAfter)
    }

    activeStreams.delete(messageId)
}

// Selectors
export const useChatMessages = () => chatStore((state) => state.chatMessages)
export const useConversationId = () =>
    chatStore((state) => state.conversationId)
export const useAiProvider = () => chatStore((state) => state.aiProvider)
export const useChatScrollPosition = () =>
    chatStore((state) => state.scrollPosition)
export const useChatActions = () => chatStore((state) => state.actions)
export const useMessageReaction = (messageId: string) =>
    chatStore((state) => state.messageFeedback[messageId] ?? null)
export const useIsStreaming = () =>
    chatStore((state) => {
        const last = state.chatMessages[state.chatMessages.length - 1]
        return last?.type === 'ai' && last?.status === 'streaming'
    })
export const useIsChatEmpty = () =>
    chatStore((state) => state.chatMessages.length === 0)
