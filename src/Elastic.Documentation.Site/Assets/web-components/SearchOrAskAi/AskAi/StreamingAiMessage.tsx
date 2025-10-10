import { ChatMessage } from './ChatMessage'
import {
    ChatMessage as ChatMessageType,
    useChatActions,
    useThreadId,
} from './chat.store'
import { useLlmGateway } from './useLlmGateway'
import * as React from 'react'
import { useEffect, useRef } from 'react'

interface StreamingAiMessageProps {
    message: ChatMessageType
    isLast: boolean
}

export const StreamingAiMessage = ({
    message,
    isLast,
}: StreamingAiMessageProps) => {
    const { updateAiMessage, hasMessageBeenSent, markMessageAsSent } =
        useChatActions()
    const threadId = useThreadId()
    const contentRef = useRef('')

    const { messages: llmMessages, sendQuestion } = useLlmGateway({
        threadId,
        onMessage: (llmMessage) => {
            if (llmMessage.type === 'ai_message_chunk') {
                contentRef.current += llmMessage.data.content
            } else if (llmMessage.type === 'agent_end') {
                updateAiMessage(message.id, contentRef.current, 'complete')
            }
        },
        onError: () => {
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

    return (
        <ChatMessage
            message={message}
            llmMessages={isLast ? llmMessages : []}
            streamingContent={
                isLast && message.status === 'streaming'
                    ? contentRef.current
                    : undefined
            }
        />
    )
}
