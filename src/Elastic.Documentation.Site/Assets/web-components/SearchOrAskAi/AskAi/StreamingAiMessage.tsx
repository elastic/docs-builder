import { EventTypes } from './AskAiEvent'
import { ChatMessage } from './ChatMessage'
import {
    ChatMessage as ChatMessageType,
    useChatActions,
    useConversationId,
} from './chat.store'
import { useAskAi } from './useAskAi'
import { useEffect, useRef } from 'react'

interface StreamingAiMessageProps {
    message: ChatMessageType
    isLast: boolean
}

export const StreamingAiMessage = ({
    message,
    isLast,
}: StreamingAiMessageProps) => {
    const {
        updateAiMessage,
        hasMessageBeenSent,
        markMessageAsSent,
        setConversationId,
    } = useChatActions()
    const conversationId = useConversationId()
    const contentRef = useRef('')

    const { events, sendQuestion } = useAskAi({
        conversationId: conversationId ?? undefined,
        onEvent: (event) => {
            if (event.type === EventTypes.CONVERSATION_START) {
                // Capture conversationId from backend on first request
                if (event.conversationId && !conversationId) {
                    setConversationId(event.conversationId)
                }
            } else if (event.type === EventTypes.MESSAGE_CHUNK) {
                contentRef.current += event.content
            } else if (event.type === EventTypes.ERROR) {
                // Handle error events from the stream
                updateAiMessage(
                    message.id,
                    event.message || 'An error occurred',
                    'error'
                )
            } else if (event.type === EventTypes.CONVERSATION_END) {
                updateAiMessage(message.id, message.content || contentRef.current, 'complete')
            }
        },
        onError: (error) => {
            console.error('[AI Provider] Error in StreamingAiMessage:', {
                messageId: message.id,
                errorMessage: error.message,
                errorStack: error.stack,
                errorName: error.name,
                fullError: error,
            })
            updateAiMessage(
                message.id,
                message.content || 'Error occurred',
                'error'
            )
        },
    })

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

    // Always use contentRef.current if it has content (regardless of status)
    // This way we don't need to save to message.content and can just use streamingContent
    const streamingContentToPass =
        isLast && contentRef.current ? contentRef.current : undefined

    return (
        <ChatMessage
            message={message}
            events={isLast ? events : []}
            streamingContent={streamingContentToPass}
        />
    )
}
