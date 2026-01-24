/**
 * StreamingAiMessage - Renders an AI message that may be streaming.
 *
 * Streaming is managed by the Zustand store, which persists across
 * component mounts/unmounts. This component simply renders from the store.
 */
import { ChatMessage } from './ChatMessage'
import {
    ChatMessage as ChatMessageType,
    useChatActions,
    useIsStreaming,
} from './chat.store'
import { useIsAskAiCooldownActive } from './useAskAiCooldown'
import { useCallback, useEffect } from 'react'

interface StreamingAiMessageProps {
    message: ChatMessageType
    isLast: boolean
    onAbortReady?: (abort: () => void) => void
    showError?: boolean
}

export const StreamingAiMessage = ({
    message,
    isLast,
    onAbortReady,
    showError,
}: StreamingAiMessageProps) => {
    const { abortMessage, cancelStreaming, submitQuestion } = useChatActions()
    const isCooldownActive = useIsAskAiCooldownActive()
    const isStreaming = useIsStreaming()

    // Create abort function for this specific message
    const handleAbort = useCallback(() => {
        abortMessage(message.id)
        cancelStreaming()
    }, [message.id, abortMessage, cancelStreaming])

    // Handler for re-asking an interrupted question
    const handleAskAgain = useCallback(
        (question: string) => {
            submitQuestion(question)
        },
        [submitQuestion]
    )

    // Expose abort function to parent when this is the last message and streaming
    useEffect(() => {
        if (isLast && message.status === 'streaming' && onAbortReady) {
            onAbortReady(handleAbort)
        }
    }, [isLast, message.status, onAbortReady, handleAbort])

    const canAskAgain =
        message.status === 'interrupted' || message.status === 'error'
    const isAskAgainDisabled = isCooldownActive || isStreaming

    return (
        <ChatMessage
            message={message}
            events={message.reasoning || []}
            error={message.error ?? undefined}
            showError={showError}
            onAskAgain={canAskAgain ? handleAskAgain : undefined}
            askAgainDisabled={isAskAgainDisabled}
        />
    )
}
