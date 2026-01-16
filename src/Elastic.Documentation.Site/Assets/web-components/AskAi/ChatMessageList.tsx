import { ChatMessage } from './ChatMessage'
import { StreamingAiMessage } from './StreamingAiMessage'
import { ChatMessage as ChatMessageType } from './chat.store'
import * as React from 'react'
import { useMemo } from 'react'

interface ChatMessageListProps {
    messages: ChatMessageType[]
    onAbortReady?: (abort: () => void) => void
}

export const ChatMessageList = ({
    messages,
    onAbortReady,
}: ChatMessageListProps) => {
    const lastErrorMessageId = useMemo(() => {
        const errorMessages = messages.filter((m) => m.status === 'error')
        return errorMessages.length > 0
            ? errorMessages[errorMessages.length - 1].id
            : null
    }, [messages])

    return (
        <>
            {messages.map((message, index) => (
                <React.Fragment key={message.id}>
                    {message.type === 'user' ? (
                        <ChatMessage message={message} />
                    ) : (
                        <StreamingAiMessage
                            message={message}
                            isLast={index === messages.length - 1}
                            onAbortReady={onAbortReady}
                            showError={message.id === lastErrorMessageId}
                        />
                    )}
                </React.Fragment>
            ))}
        </>
    )
}
