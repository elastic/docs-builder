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

// IndexedDB storage for conversations index (lightweight, always loaded)
const conversationsIndexStorage = createIndexedDBStorage(
    'elastic-docs',
    'conversations-index'
)

// IndexedDB storage for per-conversation messages (lazy loaded)
// Each conversation gets its own storage key
const messageStorageCache = new Map<
    ConversationId,
    ReturnType<typeof createIndexedDBStorage>
>()

function getMessageStorage(conversationId: ConversationId) {
    if (!messageStorageCache.has(conversationId)) {
        messageStorageCache.set(
            conversationId,
            createIndexedDBStorage('elastic-docs', `messages-${conversationId}`)
        )
    }
    return messageStorageCache.get(conversationId)!
}

// Helper to save messages for a conversation
async function saveConversationMessages(
    conversationId: ConversationId,
    messages: ChatMessage[],
    totalMessageCount: number,
    messageFeedback: Record<string, Reaction>
) {
    const storage = getMessageStorage(conversationId)
    await storage.setItem(`messages-${conversationId}`, {
        state: {
            chatMessages: messages.slice(-MAX_PERSISTED_MESSAGES),
            totalMessageCount,
            messageFeedback,
        },
        version: 1,
    })
}

// Helper to load messages for a conversation
async function loadConversationMessages(
    conversationId: ConversationId
): Promise<{
    chatMessages: ChatMessage[]
    totalMessageCount: number
    messageFeedback: Record<string, Reaction>
} | null> {
    const storage = getMessageStorage(conversationId)
    try {
        const stored = await storage.getItem(`messages-${conversationId}`)
        if (stored && typeof stored === 'object' && 'state' in stored) {
            const state = (
                stored as {
                    state: {
                        chatMessages?: ChatMessage[]
                        totalMessageCount?: number
                        messageFeedback?: Record<string, Reaction>
                    }
                }
            ).state

            // Mark any streaming messages as interrupted
            const messages = (state.chatMessages ?? []).map(
                (msg: ChatMessage) =>
                    msg.status === 'streaming'
                        ? { ...msg, status: 'interrupted' as const }
                        : msg
            )

            return {
                chatMessages: messages,
                totalMessageCount: state.totalMessageCount ?? messages.length,
                messageFeedback: state.messageFeedback ?? {},
            }
        }
    } catch {
        // Ignore errors
    }
    return null
}

// Helper to delete messages for a conversation
async function deleteConversationMessages(conversationId: ConversationId) {
    const storage = getMessageStorage(conversationId)
    await storage.removeItem(`messages-${conversationId}`)
    messageStorageCache.delete(conversationId)
}

export type { AiProvider }
export type ConversationId = string
export type Reaction = 'thumbsUp' | 'thumbsDown'

export interface ChatMessage {
    id: string
    type: 'user' | 'ai'
    content: string
    conversationId: ConversationId
    timestamp: number
    status?: 'streaming' | 'complete' | 'error' | 'interrupted'
    question?: string // For AI messages, store the question
    error?: ApiError | Error | null
    reasoning?: AskAiEvent[] // Reasoning steps for status display (search, tool calls, etc.)
}

export interface ConversationMeta {
    id: ConversationId // Backend conversation ID (set on conversation_start)
    title: string // First user message, truncated
    createdAt: number // When conversation started
    updatedAt: number // When last message was added
    messageCount: number
    aiProvider: AiProvider // Which AI provider this conversation uses
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
    // Conversation index (lightweight, always loaded) - keyed by conversation ID for O(1) lookup
    conversations: Record<ConversationId, ConversationMeta>
    activeConversationId: ConversationId | null
    // Active conversation data (lazy loaded)
    chatMessages: ChatMessage[]
    totalMessageCount: number // Tracks all messages ever sent (for "X earlier messages not shown")
    // Other state
    aiProvider: AiProvider
    messageFeedback: Record<string, Reaction> // messageId -> reaction
    scrollPosition: number
    inputValue: string // Draft input text (persisted so user doesn't lose it)
    hasHydrated: boolean // True after IndexedDB hydration completes (not persisted)
    actions: {
        // Existing actions
        submitQuestion: (question: string) => void
        updateAiMessage: (
            id: string,
            content: string,
            status: ChatMessage['status'],
            error?: ApiError | Error | null
        ) => void
        addReasoningToMessage: (id: string, event: AskAiEvent) => void
        setConversationId: (conversationId: ConversationId) => void
        setAiProvider: (provider: AiProvider) => void
        clearChat: () => void
        clearNon429Errors: () => void
        cancelStreaming: () => void
        setMessageFeedback: (messageId: string, reaction: Reaction) => void
        abortMessage: (messageId: string) => void
        isStreaming: (messageId: string) => boolean
        setScrollPosition: (position: number) => void
        setInputValue: (value: string) => void
        // New multi-conversation actions
        switchConversation: (id: ConversationId) => Promise<void>
        createConversation: () => void
        deleteConversation: (id: ConversationId) => Promise<void>
        clearAllConversations: () => void
    }
}

export const chatStore = createStore<ChatState>()(
    persist(
        (set, get) => ({
            // Conversation index
            conversations: {},
            activeConversationId: null,
            // Active conversation data
            chatMessages: [],
            totalMessageCount: 0,
            // Other state
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
                        conversationId: state.activeConversationId ?? '',
                        timestamp: Date.now(),
                    }

                    const aiMessage: ChatMessage = {
                        id: aiMessageId,
                        type: 'ai',
                        content: '',
                        question,
                        conversationId: state.activeConversationId ?? '',
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
                        state.activeConversationId,
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
                    const state = get()
                    const existingConv = state.conversations[conversationId]

                    if (existingConv) {
                        // Update existing conversation's metadata
                        set({
                            activeConversationId: conversationId,
                            conversations: {
                                ...state.conversations,
                                [conversationId]: {
                                    ...existingConv,
                                    updatedAt: Date.now(),
                                    messageCount: state.chatMessages.length,
                                },
                            },
                        })
                    } else {
                        // Create new conversation entry
                        const firstUserMessage = state.chatMessages.find(
                            (m) => m.type === 'user'
                        )
                        const newConv: ConversationMeta = {
                            id: conversationId,
                            title:
                                firstUserMessage?.content.slice(0, 100) ??
                                'New conversation',
                            createdAt: Date.now(),
                            updatedAt: Date.now(),
                            messageCount: state.chatMessages.length,
                            aiProvider: state.aiProvider,
                        }
                        set({
                            activeConversationId: conversationId,
                            conversations: {
                                ...state.conversations,
                                [conversationId]: newConv,
                            },
                        })
                    }
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
                        activeConversationId: null,
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

                // Multi-conversation actions
                switchConversation: async (id) => {
                    const state = get()

                    // Save current conversation's messages before switching
                    if (
                        state.activeConversationId &&
                        state.chatMessages.length > 0
                    ) {
                        await saveConversationMessages(
                            state.activeConversationId,
                            state.chatMessages,
                            state.totalMessageCount,
                            state.messageFeedback
                        )
                    }

                    // Find the conversation to get its aiProvider
                    const targetConv = state.conversations[id]

                    // Load the new conversation's messages
                    const loaded = await loadConversationMessages(id)

                    set({
                        activeConversationId: id,
                        chatMessages: loaded?.chatMessages ?? [],
                        totalMessageCount: loaded?.totalMessageCount ?? 0,
                        messageFeedback: loaded?.messageFeedback ?? {},
                        aiProvider: targetConv?.aiProvider ?? state.aiProvider,
                        scrollPosition: 0,
                        inputValue: '',
                    })
                },

                createConversation: () => {
                    const state = get()

                    // Save current conversation before creating new one
                    if (
                        state.activeConversationId &&
                        state.chatMessages.length > 0
                    ) {
                        saveConversationMessages(
                            state.activeConversationId,
                            state.chatMessages,
                            state.totalMessageCount,
                            state.messageFeedback
                        )
                    }

                    // Clear for new conversation (ID will be set by backend)
                    set({
                        activeConversationId: null,
                        chatMessages: [],
                        totalMessageCount: 0,
                        messageFeedback: {},
                        scrollPosition: 0,
                        inputValue: '',
                    })
                },

                deleteConversation: async (id) => {
                    const state = get()

                    // Delete from IndexedDB
                    await deleteConversationMessages(id)

                    // Remove from conversations map
                    // eslint-disable-next-line @typescript-eslint/no-unused-vars
                    const { [id]: _, ...updatedConversations } =
                        state.conversations

                    // If deleting active conversation, switch to most recent or clear
                    if (state.activeConversationId === id) {
                        // Find most recent conversation by updatedAt
                        const remaining = Object.values(updatedConversations)
                        const mostRecent = remaining.sort(
                            (a, b) => b.updatedAt - a.updatedAt
                        )[0]

                        if (mostRecent) {
                            const loaded = await loadConversationMessages(
                                mostRecent.id
                            )
                            set({
                                conversations: updatedConversations,
                                activeConversationId: mostRecent.id,
                                chatMessages: loaded?.chatMessages ?? [],
                                totalMessageCount:
                                    loaded?.totalMessageCount ?? 0,
                                messageFeedback: loaded?.messageFeedback ?? {},
                                aiProvider: mostRecent.aiProvider,
                                scrollPosition: 0,
                            })
                        } else {
                            set({
                                conversations: updatedConversations,
                                activeConversationId: null,
                                chatMessages: [],
                                totalMessageCount: 0,
                                messageFeedback: {},
                                scrollPosition: 0,
                            })
                        }
                    } else {
                        set({ conversations: updatedConversations })
                    }
                },

                clearAllConversations: () => {
                    const state = get()

                    // Abort all active streams
                    for (const [, stream] of activeStreams) {
                        stream.throttler.clear()
                        stream.controller.abort()
                    }
                    sentAiMessageIds.clear()
                    activeStreams.clear()

                    // Delete all conversation messages from IndexedDB
                    for (const conv of Object.values(state.conversations)) {
                        deleteConversationMessages(conv.id)
                    }

                    set({
                        conversations: {},
                        activeConversationId: null,
                        chatMessages: [],
                        totalMessageCount: 0,
                        messageFeedback: {},
                        scrollPosition: 0,
                        inputValue: '',
                    })
                },
            },
        }),
        {
            name: 'elastic-docs-conversations-index',
            version: 1,
            storage: conversationsIndexStorage,
            skipHydration: true, // Manual hydration to properly sequence loading
            // Only persist conversations index and UI state (not messages - those are stored per-conversation)
            partialize: (state) =>
                ({
                    conversations: state.conversations,
                    activeConversationId: state.activeConversationId,
                    aiProvider: state.aiProvider,
                    scrollPosition: state.scrollPosition,
                    inputValue: state.inputValue,
                }) as Partial<ChatState>,
        }
    )
)

// Two-phase hydration from IndexedDB:
// 1. Load conversations index (lightweight, fast)
// 2. Load active conversation's messages (if any)
if (typeof window !== 'undefined') {
    ;(async () => {
        try {
            // Phase 1: Load conversations index
            const stored = await conversationsIndexStorage.getItem(
                'elastic-docs-conversations-index'
            )

            let conversations: Record<ConversationId, ConversationMeta> = {}
            let activeConversationId: ConversationId | null = null
            let aiProvider: AiProvider = 'LlmGateway'
            let scrollPosition = 0
            let inputValue = ''

            if (stored && typeof stored === 'object' && 'state' in stored) {
                const persistedState = (
                    stored as { state: Partial<ChatState>; version?: number }
                ).state

                conversations = persistedState.conversations ?? {}
                activeConversationId =
                    persistedState.activeConversationId ?? null
                scrollPosition = persistedState.scrollPosition ?? 0
                inputValue = persistedState.inputValue ?? ''

                // Get aiProvider from the active conversation, or fall back to stored/default
                const activeConv = activeConversationId
                    ? conversations[activeConversationId]
                    : undefined
                aiProvider =
                    activeConv?.aiProvider ??
                    persistedState.aiProvider ??
                    'LlmGateway'
            }

            // Phase 2: Load active conversation's messages (if any)
            let chatMessages: ChatMessage[] = []
            let totalMessageCount = 0
            let messageFeedback: Record<string, Reaction> = {}

            if (activeConversationId) {
                const loaded =
                    await loadConversationMessages(activeConversationId)
                if (loaded) {
                    chatMessages = loaded.chatMessages
                    totalMessageCount = loaded.totalMessageCount
                    messageFeedback = loaded.messageFeedback
                }
            }

            // Set the complete state
            chatStore.setState({
                conversations,
                activeConversationId,
                chatMessages,
                totalMessageCount,
                aiProvider,
                messageFeedback,
                scrollPosition,
                inputValue,
                hasHydrated: true,
            })
        } catch {
            // Hydration failed - continue with empty state but mark as hydrated
            chatStore.setState({ hasHydrated: true })
        }
    })()
}

/**
 * Save messages and mark streaming ones as interrupted before the page unloads.
 * The persist middleware handles the conversations index; we manually save messages.
 */
if (typeof window !== 'undefined') {
    window.addEventListener('beforeunload', () => {
        const state = chatStore.getState()

        // Mark streaming messages as interrupted
        const messages = state.chatMessages.map((msg) => {
            if (msg.status === 'streaming') {
                const stream = activeStreams.get(msg.id)
                return {
                    ...msg,
                    content: stream?.content || msg.content,
                    status: 'interrupted' as const,
                }
            }
            return msg
        })

        // Save current conversation's messages to IndexedDB
        if (state.activeConversationId && messages.length > 0) {
            saveConversationMessages(
                state.activeConversationId,
                messages,
                state.totalMessageCount,
                state.messageFeedback
            )
        }
    })
}

/**
 * Start streaming for a message. Runs outside React lifecycle.
 */
async function startStream(
    messageId: string,
    question: string,
    conversationId: ConversationId | null,
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

        // Save messages to IndexedDB after streaming completes
        const state = chatStore.getState()
        if (state.activeConversationId) {
            saveConversationMessages(
                state.activeConversationId,
                state.chatMessages,
                state.totalMessageCount,
                state.messageFeedback
            )
        }
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
    useStore(chatStore, (state) => state.activeConversationId)
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

// Multi-conversation selectors
export const useConversations = () =>
    useStore(chatStore, (state) => state.conversations)
export const useActiveConversationId = () =>
    useStore(chatStore, (state) => state.activeConversationId)
