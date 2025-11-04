import { EventTypes } from './AskAiEvent'
import { ChatMessage } from './ChatMessage'
import {
    ChatMessage as ChatMessageType,
    useChatActions,
    useThreadId,
} from './chat.store'
import { useAskAi } from './useAskAi'
import { ApiError } from '../errorHandling'
import * as React from 'react'
import { useEffect, useRef } from 'react'

interface StreamingAiMessageProps {
    message: ChatMessageType
    isLast: boolean
    onAbortReady?: (abort: () => void) => void
    onCountdownChange?: (countdown: number | null) => void
}

export const StreamingAiMessage = ({
    message,
    isLast,
    onAbortReady,
    onCountdownChange,
}: StreamingAiMessageProps) => {
    const {
        updateAiMessage,
        hasMessageBeenSent,
        markMessageAsSent,
        setThreadId,
    } = useChatActions()
    const threadId = useThreadId()
    const contentRef = useRef('')

    const { events, sendQuestion, abort, error } = useAskAi({
        threadId: threadId ?? undefined,
        onEvent: (event) => {
            if (event.type === EventTypes.CONVERSATION_START) {
                // Capture conversationId from backend on first request
                if (event.conversationId && !threadId) {
                    setThreadId(event.conversationId)
                }
            } else if (event.type === EventTypes.CHUNK) {
                contentRef.current += event.content
            } else if (event.type === EventTypes.ERROR) {
                // Handle error events from the stream
                updateAiMessage(
                    message.id,
                    event.message || 'An error occurred',
                    'error'
                )
            } else if (event.type === EventTypes.CONVERSATION_END) {
                updateAiMessage(message.id, contentRef.current, 'complete')
            }
        },
        onError: (error: ApiError | Error | null) => {
            updateAiMessage(
                message.id,
                message.content || error?.message || 'Error occurred',
                'error'
            )
        },
    })

    // Expose abort function to parent when this is the last message
    useEffect(() => {
        if (isLast && message.status === 'streaming') {
            onAbortReady?.(abort)
        }
    }, [isLast, message.status, abort, onAbortReady])

    useEffect(() => {
        if (
            isLast &&
            message.status === 'streaming' &&
            message.question &&
            !hasMessageBeenSent(message.id)
        ) {
            markMessageAsSent(message.id)
            contentRef.current = ''
            sendQuestion(message.question)
        }
    }, [
        isLast,
        message.status,
        message.question,
        message.id,
        sendQuestion,
        hasMessageBeenSent,
        markMessageAsSent,
    ])

    return (
        <ChatMessage
            message={message}
            events={isLast ? events : []}
            streamingContent={
                isLast && message.status === 'streaming'
                    ? contentRef.current
                    : undefined
            }
            error={isLast ? error : null}
            onCountdownChange={onCountdownChange}
        />
    )
}
