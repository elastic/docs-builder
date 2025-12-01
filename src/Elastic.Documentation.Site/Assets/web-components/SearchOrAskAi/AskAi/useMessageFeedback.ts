import { logWarn } from '../../../telemetry/logging'
import { traceSpan } from '../../../telemetry/tracing'
import { useMutation } from '@tanstack/react-query'
import { useState, useCallback } from 'react'

export type Reaction = 'thumbsUp' | 'thumbsDown'

interface MessageFeedbackRequest {
    messageId: string
    conversationId: string
    reaction: Reaction
}

interface UseMessageFeedbackReturn {
    selectedReaction: Reaction | null
    submitFeedback: (reaction: Reaction) => void
    isPending: boolean
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
    const [selectedReaction, setSelectedReaction] = useState<Reaction | null>(
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

            // Ignore if already submitting
            if (mutation.isPending) {
                return
            }

            // Optimistic update
            setSelectedReaction(reaction)

            // Submit to API
            mutation.mutate({
                messageId,
                conversationId,
                reaction,
            })
        },
        [messageId, conversationId, selectedReaction, mutation]
    )

    return {
        selectedReaction,
        submitFeedback,
        isPending: mutation.isPending,
    }
}
