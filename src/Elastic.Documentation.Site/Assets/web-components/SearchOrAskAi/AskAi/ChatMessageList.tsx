import { ChatMessage as ChatMessageType } from './chat.store'
import { ChatMessage } from './ChatMessage'
import { StreamingAiMessage } from './StreamingAiMessage'
import { EuiSpacer } from '@elastic/eui'
import * as React from 'react'

interface ChatMessageListProps {
    messages: ChatMessageType[]
}

export const ChatMessageList = ({ messages }: ChatMessageListProps) => {
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
                        />
                    )}
                    {index < messages.length - 1 && <EuiSpacer size="m" />}
                </React.Fragment>
            ))}
        </>
    )
}
