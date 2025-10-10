import { initCopyButton } from '../../../copybutton'
import { GeneratingStatus } from './GeneratingStatus'
import { References } from './RelatedResources'
import { ChatMessage as ChatMessageType } from './chat.store'
import { LlmGatewayMessage } from './useLlmGateway'
import {
    EuiButtonIcon,
    EuiCallOut,
    EuiCopy,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiLoadingElastic,
    EuiPanel,
    EuiSpacer,
    EuiText,
    EuiToolTip,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import DOMPurify from 'dompurify'
import hljs from 'highlight.js/lib/core'
import { Marked, RendererObject, Tokens } from 'marked'
import * as React from 'react'
import { useEffect, useMemo } from 'react'

// Create the marked instance once globally (renderer never changes)
const createMarkedInstance = () => {
    const renderer: RendererObject = {
        code({ text, lang }: Tokens.Code): string {
            const highlighted = lang
                ? hljs.highlight(text, { language: lang }).value
                : hljs.highlightAuto(text).value
            return `<div class="highlight">
                <pre>
                    <code class="language-${lang}">${highlighted}</code>
                </pre>
            </div>`
        },
    }
    return new Marked({ renderer })
}

const markedInstance = createMarkedInstance()

interface ChatMessageProps {
    message: ChatMessageType
    llmMessages?: LlmGatewayMessage[]
    streamingContent?: string
    error?: Error | null
    onRetry?: () => void
}

const getAccumulatedContent = (messages: LlmGatewayMessage[]) => {
    return messages
        .filter((m) => m.type === 'ai_message_chunk')
        .map((m) => m.data.content)
        .join('')
}

const splitContentAndReferences = (
    content: string
): { mainContent: string; referencesJson: string | null } => {
    const delimiter = '--- references ---'
    const delimiterIndex = content.indexOf(delimiter)

    if (delimiterIndex === -1) {
        return { mainContent: content, referencesJson: null }
    }

    const mainContent = content.substring(0, delimiterIndex).trim()
    const referencesJson = content
        .substring(delimiterIndex + delimiter.length)
        .trim()

    return { mainContent, referencesJson }
}

const getMessageState = (message: ChatMessageType) => ({
    isUser: message.type === 'user',
    isLoading: message.status === 'streaming',
    isComplete: message.status === 'complete',
    hasError: message.status === 'error',
})

// Helper functions for computing AI status
const getToolCallSearchQuery = (
    messages: LlmGatewayMessage[]
): string | null => {
    const toolCallMessage = messages.find((m) => m.type === 'tool_call')
    if (!toolCallMessage) return null

    try {
        const toolCalls = toolCallMessage.data?.toolCalls
        if (toolCalls && toolCalls.length > 0) {
            const firstToolCall = toolCalls[0]
            return firstToolCall.args?.searchQuery || null
        }
    } catch (e) {
        console.error('Error extracting search query from tool call:', e)
    }

    return null
}

const hasContentStarted = (messages: LlmGatewayMessage[]): boolean => {
    return messages.some((m) => m.type === 'ai_message_chunk' && m.data.content)
}

const hasReachedReferences = (messages: LlmGatewayMessage[]): boolean => {
    const accumulatedContent = messages
        .filter((m) => m.type === 'ai_message_chunk')
        .map((m) => m.data.content)
        .join('')
    return accumulatedContent.includes('--- references ---')
}

const computeAiStatus = (
    llmMessages: LlmGatewayMessage[],
    isComplete: boolean
): string | null => {
    if (isComplete) return null

    const searchQuery = getToolCallSearchQuery(llmMessages)
    const contentStarted = hasContentStarted(llmMessages)
    const reachedReferences = hasReachedReferences(llmMessages)

    if (reachedReferences) {
        return 'Gathering resources'
    } else if (contentStarted) {
        return 'Generating'
    } else if (searchQuery) {
        return `Searching for "${searchQuery}"`
    }

    return 'Thinking'
}

// Action bar for complete AI messages
const ActionBar = ({
    content,
    onRetry,
}: {
    content: string
    onRetry?: () => void
}) => (
    <EuiFlexGroup responsive={false} component="span" gutterSize="none">
        <EuiFlexItem grow={false}>
            <EuiToolTip content="This answer was helpful">
                <EuiButtonIcon
                    aria-label="This answer was helpful"
                    iconType="thumbUp"
                    color="success"
                    size="s"
                />
            </EuiToolTip>
        </EuiFlexItem>
        <EuiFlexItem grow={false}>
            <EuiToolTip content="This answer was not helpful">
                <EuiButtonIcon
                    aria-label="This answer was not helpful"
                    iconType="thumbDown"
                    color="danger"
                    size="s"
                />
            </EuiToolTip>
        </EuiFlexItem>
        <EuiFlexItem grow={false}>
            <EuiCopy
                textToCopy={content}
                beforeMessage="Copy markdown"
                afterMessage="Copied!"
            >
                {(copy) => (
                    <EuiButtonIcon
                        aria-label="Copy markdown"
                        iconType="copy"
                        size="s"
                        onClick={copy}
                    />
                )}
            </EuiCopy>
        </EuiFlexItem>
        {onRetry && (
            <EuiFlexItem grow={false}>
                <EuiToolTip content="Request a new answer">
                    <EuiButtonIcon
                        aria-label="Request a new answer"
                        iconType="refresh"
                        onClick={onRetry}
                        size="s"
                    />
                </EuiToolTip>
            </EuiFlexItem>
        )}
    </EuiFlexGroup>
)

export const ChatMessage = ({
    message,
    llmMessages = [],
    streamingContent,
    error,
    onRetry,
}: ChatMessageProps) => {
    const { euiTheme } = useEuiTheme()
    const { isUser, isLoading, isComplete } = getMessageState(message)

    if (isUser) {
        return (
            <div
                data-message-type="user"
                data-message-id={message.id}
                css={css`
                    max-width: 50%;
                    justify-self: flex-end;
                `}
            >
                <EuiPanel
                    paddingSize="s"
                    hasShadow={false}
                    hasBorder={true}
                    css={css`
                        border-radius: ${euiTheme.border.radius.medium};
                        background-color: ${euiTheme.colors
                            .backgroundLightText};
                    `}
                >
                    <EuiText size="s">{message.content}</EuiText>
                </EuiPanel>
            </div>
        )
    }

    const content =
        streamingContent ||
        (llmMessages.length > 0
            ? getAccumulatedContent(llmMessages)
            : message.content)

    const hasError = message.status === 'error' || !!error

    // Only split content and references when complete for better performance
    const { mainContent, referencesJson } = useMemo(() => {
        if (isComplete) {
            return splitContentAndReferences(content)
        }
        // During streaming, strip out unparsed references but don't parse them yet
        const delimiter = '--- references ---'
        const delimiterIndex = content.indexOf(delimiter)
        if (delimiterIndex !== -1) {
            return { mainContent: content.substring(0, delimiterIndex).trim(), referencesJson: null }
        }
        return { mainContent: content, referencesJson: null }
    }, [content, isComplete])

    const parsed = useMemo(() => {
        const html = markedInstance.parse(mainContent) as string
        return DOMPurify.sanitize(html)
    }, [mainContent])

    const aiStatus = useMemo(
        () => computeAiStatus(llmMessages, isComplete),
        [llmMessages, isComplete]
    )

    const ref = React.useRef<HTMLDivElement>(null)

    useEffect(() => {
        if (isComplete && ref.current) {
            const timer = setTimeout(() => {
                try {
                    initCopyButton(
                        '.highlight pre',
                        ref.current!,
                        'ai-message-codecell-'
                    )
                } catch (error) {
                    console.error('Failed to initialize copy buttons:', error)
                }
            }, 100)
            return () => clearTimeout(timer)
        }
    }, [isComplete])

    return (
        <EuiFlexGroup
            gutterSize="s"
            alignItems="flexStart"
            responsive={false}
            data-message-type="ai"
            data-message-id={message.id}
        >
            <EuiFlexItem grow={false}>
                <div
                    css={css`
                        block-size: 32px;
                        inline-size: 32px;
                        border-radius: 50%;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                    `}
                >
                    {isLoading ? (
                        <EuiLoadingElastic
                            size="xl"
                            css={css`
                                margin-top: -1px;
                            `}
                        />
                    ) : (
                        <EuiIcon
                            name="Elastic Docs AI"
                            size="xl"
                            type="logoElastic"
                        />
                    )}
                </div>
            </EuiFlexItem>
            <EuiFlexItem>
                <EuiPanel
                    paddingSize="m"
                    hasShadow={false}
                    hasBorder={false}
                    css={css`
                        padding-top: 8px;
                    `}
                >
                    {content && (
                        <div
                            ref={ref}
                            className="markdown-content"
                            css={css`
                                font-size: 14px;
                                & > *:first-child {
                                    margin-top: 0;
                                }
                            `}
                            dangerouslySetInnerHTML={{ __html: parsed }}
                        />
                    )}

                    {referencesJson && (
                        <References referencesJson={referencesJson} />
                    )}

                    {content && isLoading && <EuiSpacer size="m" />}
                    <GeneratingStatus status={aiStatus} />

                    {isComplete && content && (
                        <>
                            <EuiSpacer size="m" />
                            <ActionBar content={mainContent} onRetry={onRetry} />
                        </>
                    )}

                    {hasError && (
                        <>
                            <EuiSpacer size="m" />
                            <EuiCallOut
                                title="Sorry, there was an error"
                                color="danger"
                                iconType="error"
                                size="s"
                            >
                                <p>
                                    The Elastic Docs AI Assistant encountered an
                                    error. Please try again.
                                </p>
                            </EuiCallOut>
                        </>
                    )}
                </EuiPanel>
            </EuiFlexItem>
        </EuiFlexGroup>
    )
}
