import { ApiError, isApiError } from '../errorHandling'
import { useCooldown, useModalActions } from '../modal.store'
import { AskAiEvent, AskAiEventSchema } from './AskAiEvent'
import { useAiProvider } from './aiProviderStore'
import { useFetchEventSource } from './useFetchEventSource'
import { useMessageThrottling } from './useMessageThrottling'
import { EventSourceMessage } from '@microsoft/fetch-event-source'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import * as z from 'zod'

// Constants
const MESSAGE_THROTTLE_MS = 25 // Throttle messages to prevent UI flooding

export const AskAiRequestSchema = z.object({
    message: z.string(),
    threadId: z.string().optional(),
})

export type AskAiRequest = z.infer<typeof AskAiRequestSchema>

export interface UseAskAiResponse {
    events: AskAiEvent[]
    abort: () => void
    error: ApiError | Error | null
    sendQuestion: (question: string) => Promise<void>
}

interface Props {
    onEvent?: (event: AskAiEvent) => void
    onError?: (error: ApiError | Error | null) => void
    threadId?: string
}

export const useAskAi = (props: Props): UseAskAiResponse => {
    const [events, setEvents] = useState<AskAiEvent[]>([])
    const [error, setError] = useState<ApiError | Error | null>(null)
    const storeCooldown = useCooldown()
    const { setCooldown } = useModalActions()
    const lastSentQuestionRef = useRef<string>('')

    // Get AI provider from store (user-controlled via UI)
    const aiProvider = useAiProvider()

    // Log which provider is being used for this conversation
    useEffect(() => {
        console.log(`[AI Provider] Using ${aiProvider} for this conversation`)
    }, [aiProvider])

    // Prepare headers with AI provider
    const headers = useMemo(
        () => ({
            'X-AI-Provider': aiProvider,
        }),
        [aiProvider]
    )

    const { processMessage, clearQueue } = useMessageThrottling<AskAiEvent>({
        delayInMs: MESSAGE_THROTTLE_MS,
        onMessage: (event) => {
            setEvents((prev) => [...prev, event])
            props.onEvent?.(event)
        },
    })

    const onMessage = useCallback(
        (sseEvent: EventSourceMessage) => {
            try {
                // Parse and validate the canonical AskAiEvent format
                const rawData = JSON.parse(sseEvent.data)
                const askAiEvent = AskAiEventSchema.parse(rawData)

                processMessage(askAiEvent)
            } catch (error) {
                console.error('[AI Provider] Failed to parse SSE event:', {
                    eventData: sseEvent.data,
                    error:
                        error instanceof Error ? error.message : String(error),
                })
                // Re-throw to trigger onError handler
                throw new Error(
                    `Event parsing failed: ${error instanceof Error ? error.message : String(error)}`
                )
            }
        },
        [processMessage]
    )

    const { sendMessage, abort } = useFetchEventSource<AskAiRequest>({
        apiEndpoint: '/docs/_api/v1/ask-ai/stream',
        headers,
        onMessage,
        onError: (error) => {
            setError(error)
            if (isApiError(error)) {
                const apiError = error as ApiError
                if (apiError.statusCode === 429 && apiError.retryAfter) {
                    setCooldown(apiError.retryAfter)
                }
            }
            props.onError?.(error)
        },
    })

    const sendQuestion = useCallback(
        async (question: string) => {
            // Prevent sending during cooldown period (check store cooldown)
            if (storeCooldown !== null && storeCooldown > 0) {
                return
            }

            if (question.trim() && question !== lastSentQuestionRef.current) {
                abort()
                setError(null)
                setEvents([])
                clearQueue()
                lastSentQuestionRef.current = question
                const payload = createAskAiRequest(question, props.threadId)

                try {
                    await sendMessage(payload)
                } catch (error) {
                    if (error instanceof Error) {
                        setError(error)
                        throw error
                    }
                }
            }
        },
        [props.threadId, sendMessage, abort, clearQueue, storeCooldown]
    )

    useEffect(() => {
        return () => {
            setError(null)
            setEvents([])
            clearQueue()
        }
    }, [clearQueue])

    return {
        events,
        error,
        sendQuestion,
        abort: () => {
            abort()
            clearQueue()
        },
    }
}

function createAskAiRequest(message: string, threadId?: string): AskAiRequest {
    return AskAiRequestSchema.parse({
        message,
        threadId,
    })
}
