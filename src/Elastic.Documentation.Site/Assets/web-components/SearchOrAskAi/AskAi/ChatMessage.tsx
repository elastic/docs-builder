import { ChatMessage as ChatMessageType } from './chat.store'
import { LlmGatewayMessage } from './useLlmGateway'
import {
    EuiFlexGroup,
    EuiFlexItem,
    EuiLoadingSpinner,
    EuiMarkdownFormat,
    EuiPanel,
    EuiSpacer,
    EuiText,
    EuiButtonIcon,
    EuiToolTip,
    useEuiTheme,
    EuiCallOut,
    EuiIcon,
    EuiAvatar,
    EuiCopy,
    EuiLoadingElastic,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

interface ChatMessageProps {
    message: ChatMessageType
    llmMessages?: LlmGatewayMessage[]
    error?: Error | null
    onRetry?: () => void
}

// Helper function to accumulate AI message content
const getAccumulatedContent = (messages: LlmGatewayMessage[]) => {
    return messages
        .filter((m) => m.type === 'ai_message_chunk') // Only accumulate chunks, not the final message
        .map((m) => m.data.content)
        .join('')
}

// Derived state helper for readability
const getMessageState = (message: ChatMessageType) => ({
    isUser: message.type === 'user',
    isLoading: message.status === 'streaming',
    isComplete: message.status === 'complete',
    hasError: message.status === 'error',
})

// Extracted styles for markdown render
const markdownFormatStyles = css`
    .euiScreenReaderOnly {
        display: none;
    }
    font-size: 14px;
    b,
    strong {
        font-weight: 600;
    }
    h1,
    h2,
    h3,
    h4,
    h5,
    h6 {
        b,
        strong {
            font-weight: inherit;
        }
    }
`

// Action bar for complete AI messages
const ActionBar = ({
    content,
    onRetry,
}: {
    content: string
    onRetry?: () => void
}) => (
    <EuiFlexGroup responsive={false} component="span" gutterSize="s">
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
            <EuiCopy textToCopy={content} beforeMessage="Copy markdown" afterMessage="Copied!">
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
    error,
    onRetry,
}: ChatMessageProps) => {
    const { euiTheme } = useEuiTheme()
    const { isUser, isLoading, isComplete } = getMessageState(message)

    if (isUser) {
        return (
            <EuiFlexGroup
                gutterSize="s"
                alignItems="flexStart"
                responsive={false}
                data-message-type="user"
                data-message-id={message.id}
            >
                <EuiFlexItem grow={false}>
                    <EuiAvatar
                        name="User"
                        size="m"
                        color="#6DCCB1"
                        iconType="user"
                    />
                </EuiFlexItem>
                <EuiFlexItem>
                    <EuiPanel
                        paddingSize="m"
                        hasShadow={false}
                        hasBorder={true}
                        css={css`
                            background-color: ${euiTheme.colors.emptyShade};
                        `}
                    >
                        <EuiText size="s">{message.content}</EuiText>
                    </EuiPanel>
                </EuiFlexItem>
            </EuiFlexGroup>
        )
    }

    // AI message
    const content =
        llmMessages.length > 0
            ? getAccumulatedContent(llmMessages)
            : message.content

    const hasError = message.status === 'error' || !!error

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
                    hasBorder={true}
                    css={css`
                        background-color: ${euiTheme.colors
                            .backgroundLightText};
                    `}
                >
                    {content && (
                        <EuiMarkdownFormat css={markdownFormatStyles}>
                            {content}
                        </EuiMarkdownFormat>
                    )}

                    {isLoading && (
                        <>
                            {content && <EuiSpacer size="s" />}
                            <EuiFlexGroup
                                alignItems="center"
                                gutterSize="s"
                                responsive={false}
                            >
                                <EuiFlexItem grow={false}>
                                    <EuiLoadingSpinner size="s" />
                                </EuiFlexItem>
                                <EuiFlexItem grow={false}>
                                    <EuiText size="xs" color="subdued">
                                        Generating...
                                    </EuiText>
                                </EuiFlexItem>
                            </EuiFlexGroup>
                        </>
                    )}

                    {isComplete && content && (
                        <>
                            <EuiSpacer size="m" />
                            <ActionBar content={content} onRetry={onRetry} />
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
