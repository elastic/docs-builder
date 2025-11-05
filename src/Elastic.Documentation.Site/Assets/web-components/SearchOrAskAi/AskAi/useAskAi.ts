import { ApiError, isRateLimitError } from '../errorHandling'
import { useAskAiCooldown, useModalActions } from '../modal.store'
import { AskAiEvent, AskAiEventSchema } from './AskAiEvent'
import { useAiProvider } from './chat.store'
import { useFetchEventSource } from './useFetchEventSource'
import { useMessageThrottling } from './useMessageThrottling'
import { EventSourceMessage } from '@microsoft/fetch-event-source'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import * as z from 'zod'

// Constants
const MESSAGE_THROTTLE_MS = 25 // Throttle messages to prevent UI flooding

export const AskAiRequestSchema = z.object({
    message: z.string(),
    conversationId: z.string().optional(),
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
    onError?: (error: Error) => void
    conversationId?: string
}

export const useAskAi = (props: Props): UseAskAiResponse => {
    const [events, setEvents] = useState<AskAiEvent[]>([])
    const [error, setError] = useState<ApiError | Error | null>(null)
    const storeCooldown = useAskAiCooldown()
    const { setAskAiCooldown } = useModalActions()
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
                // Parse JSON first
                const rawData = JSON.parse(sseEvent.data)

                // Use safeParse with reportInput to include input data in validation errors
                const result = AskAiEventSchema.safeParse(rawData, {
                    reportInput: true,
                })

                if (!result.success) {
                    // Log detailed validation errors with input data
                    console.error('[AI Provider] Failed to parse SSE event:', {
                        eventId: sseEvent.id || 'unknown',
                        eventType: sseEvent.event || 'unknown',
                        rawEventData: sseEvent.data,
                        validationErrors: result.error.issues,
                    })
                    throw new Error(
                        `Event validation failed: ${result.error.issues.map((e) => `${e.path.join('.')}: ${e.message}`).join('; ')}`
                    )
                }

                processMessage(result.data)
            } catch (error) {
                // Handle JSON parsing errors or other unexpected errors
                if (
                    error instanceof Error &&
                    error.message.includes('Event validation failed')
                ) {
                    // Already logged above, just re-throw
                    throw error
                }

                // Log JSON parsing or other errors
                console.error('[AI Provider] Failed to parse SSE event:', {
                    eventId: sseEvent.id || 'unknown',
                    eventType: sseEvent.event || 'unknown',
                    rawEventData: sseEvent.data,
                    error:
                        error instanceof Error ? error.message : String(error),
                    errorStack:
                        error instanceof Error ? error.stack : undefined,
                })

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
            console.error('[AI Provider] Error in useFetchEventSource:', {
                errorMessage: error.message,
                errorStack: error.stack,
                errorName: error.name,
                fullError: error,
            })
            setError(error)
            if (isRateLimitError(error) && error.retryAfter) {
                setAskAiCooldown(error.retryAfter)
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
                const payload = createAskAiRequest(
                    question,
                    props.conversationId
                )

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
        [props.conversationId, sendMessage, abort, clearQueue, storeCooldown]
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
