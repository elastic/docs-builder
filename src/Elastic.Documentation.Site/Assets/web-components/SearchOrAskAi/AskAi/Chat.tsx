/** @jsxImportSource @emotion/react */
import { AiProviderSelector } from './AiProviderSelector'
import { AskAiSuggestions } from './AskAiSuggestions'
import { ChatMessageList } from './ChatMessageList'
import { useChatActions, useChatMessages } from './chat.store'
import {
    useEuiOverflowScroll,
    EuiButtonEmpty,
    EuiButtonIcon,
    EuiFieldText,
    EuiFlexGroup,
    EuiFlexItem,
    EuiEmptyPrompt,
    EuiSpacer,
    EuiTitle,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useCallback, useEffect, useRef, useState } from 'react'

const containerStyles = css`
    height: 100%;
    max-height: 70vh;
    overflow: hidden;
`

const scrollContainerStyles = css`
    position: relative;
    overflow: hidden;
`

const scrollableStyles = css`
    height: 100%;
    overflow-y: auto;
    scrollbar-gutter: stable;
    padding: 1rem;
`

const messagesStyles = css`
    max-width: 800px;
    margin: 0 auto;
`

// Small helper for scroll behavior
const scrollToBottom = (container: HTMLDivElement | null) => {
    if (!container) return
    container.scrollTop = container.scrollHeight
}

// Header shown when a conversation exists
const NewConversationHeader = ({ onClick }: { onClick: () => void }) => (
    <EuiFlexItem grow={false}>
        <EuiFlexGroup justifyContent="flexEnd" alignItems="center">
            <EuiFlexItem grow={false}>
                <EuiButtonEmpty size="xs" onClick={onClick} iconType="refresh">
                    New conversation
                </EuiButtonEmpty>
            </EuiFlexItem>
        </EuiFlexGroup>
        <EuiSpacer size="s" />
    </EuiFlexItem>
)

export const Chat = () => {
    const messages = useChatMessages()
    const { submitQuestion, clearChat } = useChatActions()
    const inputRef = useRef<HTMLInputElement>(null)
    const scrollRef = useRef<HTMLDivElement>(null)
    const lastMessageStatusRef = useRef<string | null>(null)
    const abortFunctionRef = useRef<(() => void) | null>(null)
    const [inputValue, setInputValue] = useState('')
    const [countdown, setCountdown] = useState<number | null>(null)

    const dynamicScrollableStyles = css`
        ${scrollableStyles}
        ${useEuiOverflowScroll('y', true)}
    `

    // Check if there's an active streaming query
    const isStreaming = messages.length > 0 && 
        messages[messages.length - 1].type === 'ai' &&
        messages[messages.length - 1].status === 'streaming'

    // Handle abort function from StreamingAiMessage
    const handleAbortReady = useCallback((abort: () => void) => {
        abortFunctionRef.current = abort
    }, [])

    // Handle countdown changes from error messages
    const handleCountdownChange = useCallback((newCountdown: number | null) => {
        setCountdown(newCountdown)
    }, [])

    // Clear abort function when streaming ends
    useEffect(() => {
        if (!isStreaming) {
            abortFunctionRef.current = null
        }
    }, [isStreaming])

    // Handle countdown for 429 errors - removed redundant useEffect

    const handleSubmit = useCallback(
        (question: string) => {
            if (!question.trim()) return
            
            // Prevent submission during countdown
            if (countdown !== null && countdown > 0) {
                return
            }

            submitQuestion(question.trim())

            if (inputRef.current) {
                inputRef.current.value = ''
            }
            setInputValue('')

            // Scroll to bottom after new message
            setTimeout(() => scrollToBottom(scrollRef.current), 100)
        },
        [submitQuestion, countdown]
    )

    const handleButtonClick = useCallback(() => {
        if (isStreaming && abortFunctionRef.current) {
            // Interrupt current query
            abortFunctionRef.current()
            abortFunctionRef.current = null
        } else if (inputRef.current) {
            handleSubmit(inputRef.current.value)
        }
    }, [isStreaming, handleSubmit])

    // Refocus input when AI answer transitions to complete
    useEffect(() => {
        if (messages.length > 0) {
            const lastMessage = messages[messages.length - 1]

            // Track status transitions for AI messages
            if (lastMessage.type === 'ai') {
                const currentStatus = lastMessage.status
                const previousStatus = lastMessageStatusRef.current

                // If status changed from streaming to complete, focus input
                if (
                    previousStatus === 'streaming' &&
                    currentStatus === 'complete'
                ) {
                    setTimeout(() => {
                        inputRef.current?.focus()
                    }, 100)
                }

                // Update the tracked status
                lastMessageStatusRef.current = currentStatus || null
            }
        }
    }, [messages])

    return (
        <EuiFlexGroup
            direction="column"
            gutterSize="none"
            css={containerStyles}
        >
            <EuiSpacer size="m" />

            {messages.length > 0 && (
                <NewConversationHeader onClick={clearChat} />
            )}

            <EuiFlexItem grow={true} css={scrollContainerStyles}>
                <div ref={scrollRef} css={dynamicScrollableStyles}>
                    {messages.length === 0 ? (
                        <EuiEmptyPrompt
                            iconType="logoElastic"
                            title={
                                <h2>Hi! I'm the Elastic Docs AI Assistant</h2>
                            }
                            body={
                                <>
                                    <p>
                                        I can help answer your questions about
                                        Elastic documentation. <br />
                                        Ask me anything about Elasticsearch,
                                        Kibana, Observability, Security, and
                                        more.
                                    </p>
                                    <EuiSpacer size="m" />
                                    <AiProviderSelector />
                                </>
                            }
                            footer={
                                <>
                                    <EuiTitle size="xxs">
                                        <h3>Try asking me:</h3>
                                    </EuiTitle>
                                    <EuiSpacer size="s" />
                                    <AskAiSuggestions />
                                </>
                            }
                        />
                    ) : (
                        <div css={messagesStyles}>
                            <ChatMessageList 
                                messages={messages} 
                                onAbortReady={handleAbortReady}
                                onCountdownChange={handleCountdownChange}
                            />
                        </div>
                    )}
                </div>
            </EuiFlexItem>

            {/* Input */}
            <EuiFlexItem grow={false}>
                <EuiSpacer size="s" />
                <div
                    css={css`
                        position: relative;
                    `}
                >
                    <EuiFieldText
                        autoFocus
                        inputRef={inputRef}
                        fullWidth
                        placeholder="Ask Elastic Docs AI Assistant"
                        onChange={(e) => setInputValue(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                                handleSubmit(e.currentTarget.value)
                            }
                        }}
                        disabled={countdown !== null && countdown > 0}
                    />
                    <EuiButtonIcon
                        aria-label={isStreaming ? "Interrupt query" : "Send message"}
                        css={css`
                            position: absolute;
                            right: 8px;
                            top: 50%;
                            transform: translateY(-50%);
                            border-radius: 9999px;
                        `}
                        color="primary"
                        iconType={isStreaming ? "cross" : "sortUp"}
                        display={inputValue.trim() || isStreaming ? 'fill' : 'base'}
                        onClick={handleButtonClick}
                        disabled={countdown !== null && countdown > 0}
                    ></EuiButtonIcon>
                </div>
                <EuiSpacer size="m" />
            </EuiFlexItem>
        </EuiFlexGroup>
    )
}
