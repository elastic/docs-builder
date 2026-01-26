import { logError } from '../../telemetry/logging'
import { startAskAiStream, AiProvider } from '../shared/askAiStreamClient'
import { cooldownStore } from '../shared/cooldown.store'
import { ApiError, isApiError, isRateLimitError } from '../shared/errorHandling'
import { AskAiEvent } from './AskAiEvent'
import { MessageThrottler } from './MessageThrottler'
import { v4 as uuidv4 } from 'uuid'
import { createStore } from 'zustand'
import { createIndexedDBStorage } from 'zustand-indexeddb'
import { persist } from 'zustand/middleware'
import { useStore } from 'zustand/react'

// Use zustand-indexeddb for IndexedDB storage
const indexedDBStorage = createIndexedDBStorage('elastic-docs-v2', 'chat-store')

export type { AiProvider }
export type Reaction = 'thumbsUp' | 'thumbsDown'

export interface ChatMessage {
    id: string
    type: 'user' | 'ai'
    content: string
    conversationId: string
    timestamp: number
    status?: 'streaming' | 'complete' | 'error' | 'interrupted'
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

// Maximum number of messages to persist in localStorage
const MAX_PERSISTED_MESSAGES = 50

interface ChatState {
    chatMessages: ChatMessage[]
    totalMessageCount: number // Tracks all messages ever sent (for "X earlier messages not shown")
    conversationId: string | null
    aiProvider: AiProvider
    messageFeedback: Record<string, Reaction> // messageId -> reaction
    scrollPosition: number
    inputValue: string // Draft input text (persisted so user doesn't lose it)
    hasHydrated: boolean // True after IndexedDB hydration completes (not persisted)
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
        setInputValue: (value: string) => void
    }
}

export const chatStore = createStore<ChatState>()(
    persist(
        (set, get) => ({
            chatMessages: [],
            totalMessageCount: 0,
            conversationId: null, // Start with null - will be set by backend on first request
            aiProvider: 'LlmGateway', // Default to LLM Gateway
            messageFeedback: {},
            hasHydrated: false, // Will be set to true after IndexedDB hydration
            scrollPosition: 0,
            inputValue: '',
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
                        chatMessages: [
                            ...state.chatMessages,
                            userMessage,
                            aiMessage,
                        ],
                        totalMessageCount: state.totalMessageCount + 2, // user + AI message
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
                            msg.id === id
                                ? { ...msg, content, status, error }
                                : msg
                        ),
                    }))
                },

                addReasoningToMessage: (id, event) => {
                    set((state) => ({
                        chatMessages: state.chatMessages.map((msg) =>
                            msg.id === id && msg.reasoning
                                ? {
                                      ...msg,
                                      reasoning: [...msg.reasoning, event],
                                  }
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
                    set({
                        chatMessages: [],
                        totalMessageCount: 0,
                        conversationId: null,
                        messageFeedback: {},
                    })
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
                    // Collect latest content from active streams before aborting
                    const streamContents = new Map<string, string>()
                    state.chatMessages.forEach((msg) => {
                        if (msg.type === 'ai' && msg.status === 'streaming') {
                            const stream = activeStreams.get(msg.id)
                            if (stream) {
                                streamContents.set(msg.id, stream.content)
                                stream.throttler.clear()
                                stream.controller.abort()
                                activeStreams.delete(msg.id)
                            }
                        }
                    })
                    set((state) => ({
                        chatMessages: state.chatMessages.map((msg) =>
                            msg.type === 'ai' && msg.status === 'streaming'
                                ? {
                                      ...msg,
                                      content:
                                          streamContents.get(msg.id) ||
                                          msg.content,
                                      status: 'interrupted' as const,
                                  }
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

                setInputValue: (value) => {
                    set({ inputValue: value })
                },
            },
        }),
        {
            name: 'elastic-docs-ask-ai-chat',
            version: 1,
            storage: indexedDBStorage,
            skipHydration: true, // Manual hydration to avoid race condition with async storage
            partialize: (state) =>
                ({
                    chatMessages: state.chatMessages.slice(
                        -MAX_PERSISTED_MESSAGES
                    ),
                    totalMessageCount: state.totalMessageCount,
                    conversationId: state.conversationId,
                    messageFeedback: state.messageFeedback,
                    scrollPosition: state.scrollPosition,
                    inputValue: state.inputValue,
                }) as ChatState,
            onRehydrateStorage: () => (state) => {
                // Mark any messages that were streaming when page was closed as interrupted
                if (state) {
                    state.chatMessages = state.chatMessages.map(
                        (msg: ChatMessage) =>
                            msg.status === 'streaming'
                                ? { ...msg, status: 'interrupted' as const }
                                : msg
                    )
                }
            },
        }
    )
)

// Manually hydrate from IndexedDB since persist.rehydrate() doesn't properly await async storage
// We directly call getItem and set the state ourselves
if (typeof window !== 'undefined') {
    ;(async () => {
        try {
            const stored = await indexedDBStorage.getItem(
                'elastic-docs-ask-ai-chat'
            )

            if (stored && typeof stored === 'object' && 'state' in stored) {
                const persistedState = (
                    stored as { state: Partial<ChatState>; version?: number }
                ).state

                // Mark any streaming messages as interrupted (they were cut off)
                const messages = (persistedState.chatMessages ?? []).map(
                    (msg: ChatMessage) =>
                        msg.status === 'streaming'
                            ? { ...msg, status: 'interrupted' as const }
                            : msg
                )

                // Set the state directly (including hasHydrated: true)
                chatStore.setState({
                    chatMessages: messages,
                    totalMessageCount:
                        persistedState.totalMessageCount ?? messages.length,
                    conversationId: persistedState.conversationId ?? null,
                    messageFeedback: persistedState.messageFeedback ?? {},
                    scrollPosition: persistedState.scrollPosition ?? 0,
                    inputValue: persistedState.inputValue ?? '',
                    hasHydrated: true,
                })
            } else {
                // No stored data, but hydration is complete
                chatStore.setState({ hasHydrated: true })
            }
        } catch {
            // Hydration failed - continue with empty state but mark as hydrated
            chatStore.setState({ hasHydrated: true })
        }
    })()
}

/**
 * Mark streaming messages as interrupted before the page unloads.
 * This updates the state, triggering the persist middleware to write to IndexedDB.
 * As a safety net, onRehydrateStorage also marks any remaining 'streaming' messages
 * as 'interrupted' when the store rehydrates.
 */
if (typeof window !== 'undefined') {
    window.addEventListener('beforeunload', () => {
        const state = chatStore.getState()
        const hasStreamingMessages = state.chatMessages.some(
            (msg) => msg.status === 'streaming'
        )

        if (hasStreamingMessages) {
            // Update state to mark streaming messages as interrupted
            // This triggers the persist middleware to save to IndexedDB
            chatStore.setState((state) => ({
                chatMessages: state.chatMessages.map((msg) => {
                    if (msg.status === 'streaming') {
                        const stream = activeStreams.get(msg.id)
                        return {
                            ...msg,
                            content: stream?.content || msg.content,
                            status: 'interrupted' as const,
                        }
                    }
                    return msg
                }),
            }))
        }
    })
}

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

// Selectors - using useStore with the vanilla store for React compatibility
export const useChatMessages = () =>
    useStore(chatStore, (state) => state.chatMessages)
export const useConversationId = () =>
    useStore(chatStore, (state) => state.conversationId)
export const useAiProvider = () =>
    useStore(chatStore, (state) => state.aiProvider)
export const useChatScrollPosition = () =>
    useStore(chatStore, (state) => state.scrollPosition)
export const useInputValue = () =>
    useStore(chatStore, (state) => state.inputValue)
export const useChatActions = () =>
    useStore(chatStore, (state) => state.actions)
export const useMessageReaction = (messageId: string) =>
    useStore(chatStore, (state) => state.messageFeedback[messageId] ?? null)
export const useIsStreaming = () =>
    useStore(chatStore, (state) => {
        const last = state.chatMessages[state.chatMessages.length - 1]
        return last?.type === 'ai' && last?.status === 'streaming'
    })
export const useIsChatEmpty = () =>
    useStore(chatStore, (state) => state.chatMessages.length === 0)
export const useTrimmedMessageCount = () =>
    useStore(chatStore, (state) =>
        Math.max(0, state.totalMessageCount - state.chatMessages.length)
    )
export const useHasHydrated = () =>
    useStore(chatStore, (state) => state.hasHydrated)
