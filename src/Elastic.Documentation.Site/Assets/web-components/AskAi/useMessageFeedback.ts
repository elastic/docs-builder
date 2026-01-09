import { logWarn } from '../../telemetry/logging'
import { traceSpan } from '../../telemetry/tracing'
import { Reaction, useChatActions, useMessageReaction } from './chat.store'
import { useMutation } from '@tanstack/react-query'
import { useCallback, useRef } from 'react'

export type { Reaction } from './chat.store'

const DEBOUNCE_MS = 500

interface MessageFeedbackRequest {
    messageId: string
    conversationId: string
    reaction: Reaction
}

interface UseMessageFeedbackReturn {
    selectedReaction: Reaction | null
    submitFeedback: (reaction: Reaction) => void
}

const submitFeedbackToApi = async (
    payload: MessageFeedbackRequest
): Promise<void> => {
    await traceSpan('submit message-feedback', async (span) => {
        span.setAttribute('gen_ai.conversation.id', payload.conversationId) // correlation with backend
        span.setAttribute('ask_ai.message.id', payload.messageId)
        span.setAttribute('ask_ai.feedback.reaction', payload.reaction)

        const response = await fetch('/docs/_api/v1/ask-ai/message-feedback', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(payload),
        })

        if (!response.ok) {
            logWarn('Failed to submit feedback', {
                'http.status_code': response.status,
                'ask_ai.message.id': payload.messageId,
                'ask_ai.feedback.reaction': payload.reaction,
            })
        }
    })
}

export const useMessageFeedback = (
    messageId: string,
    conversationId: string | null
): UseMessageFeedbackReturn => {
    const selectedReaction = useMessageReaction(messageId)
    const { setMessageFeedback } = useChatActions()
    const debounceTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(
        null
    )

    const mutation = useMutation({
        mutationFn: submitFeedbackToApi,
        onError: (error) => {
            logWarn('Error submitting feedback', {
                'error.message':
                    error instanceof Error ? error.message : String(error),
            })
            // Don't reset selection on error - user intent was clear
        },
    })

    const submitFeedback = useCallback(
        (reaction: Reaction) => {
            if (!conversationId) {
                logWarn('Cannot submit feedback without conversationId', {
                    'ask_ai.message.id': messageId,
                })
                return
            }

            // Ignore if same reaction already selected
            if (selectedReaction === reaction) {
                return
            }

            // Optimistic update - stored in Zustand so it persists across tab switches
            setMessageFeedback(messageId, reaction)

            // Cancel any pending debounced submission
            if (debounceTimeoutRef.current) {
                clearTimeout(debounceTimeoutRef.current)
            }

            // Debounce the API call - only send the final selection
            debounceTimeoutRef.current = setTimeout(() => {
                mutation.mutate({
                    messageId,
                    conversationId,
                    reaction,
                })
            }, DEBOUNCE_MS)
        },
        [
            messageId,
            conversationId,
            selectedReaction,
            mutation,
            setMessageFeedback,
        ]
    )

    return {
        selectedReaction,
        submitFeedback,
    }
}
