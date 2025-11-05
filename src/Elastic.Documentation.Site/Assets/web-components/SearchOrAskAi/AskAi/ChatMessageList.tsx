import { ChatMessage } from './ChatMessage'
import { StreamingAiMessage } from './StreamingAiMessage'
import { ChatMessage as ChatMessageType } from './chat.store'
import { EuiSpacer } from '@elastic/eui'
import * as React from 'react'

interface ChatMessageListProps {
    messages: ChatMessageType[]
    onAbortReady?: (abort: () => void) => void
}

export const ChatMessageList = ({ messages, onAbortReady }: ChatMessageListProps) => {
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
                        />
                    )}
                    {index < messages.length - 1 && <EuiSpacer size="l" />}
                </React.Fragment>
            ))}
        </>
    )
}
