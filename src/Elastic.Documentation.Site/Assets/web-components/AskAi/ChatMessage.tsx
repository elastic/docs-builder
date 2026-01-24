import { initCopyButton } from '../../copybutton'
import { hljs } from '../../hljs'
import { ApiError } from '../shared/errorHandling'
import { useHtmxContainer } from '../shared/htmx/useHtmxContainer'
import { AskAiEvent, ChunkEvent, EventTypes } from './AskAiEvent'
import { aiGradients } from './ElasticAiAssitant'
import { GeneratingStatus } from './GeneratingStatus'
import { References } from './RelatedResources'
import { ChatMessage as ChatMessageType, useConversationId } from './chat.store'
import { useMessageFeedback } from './useMessageFeedback'
import { useStatusMinDisplay } from './useStatusMinDisplay'
import {
    EuiButtonEmpty,
    EuiButtonIcon,
    EuiCopy,
    EuiFlexGroup,
    EuiFlexItem,
    EuiPanel,
    EuiSpacer,
    EuiText,
    EuiToolTip,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import DOMPurify from 'dompurify'
import { Marked, RendererObject, Tokens } from 'marked'
import { useEffect, useMemo, useRef } from 'react'

// Create the marked instance once globally (renderer never changes)
const createMarkedInstance = () => {
    const renderer: RendererObject = {
        code({ text, lang }: Tokens.Code): string {
            let highlighted: string
            try {
                highlighted = lang
                    ? hljs.highlight(text, { language: lang }).value
                    : hljs.highlightAuto(text).value
            } catch {
                // Fallback to auto highlighting if the specified language is not found
                highlighted = hljs.highlightAuto(text).value
            }
            return `<div class="highlight">
                <pre>
                    <code class="language-${lang}">${highlighted}</code>
                </pre>
            </div>`
        },
        table(token: Tokens.Table): string {
            const defaultMarked = new Marked()
            const defaultTableHtml = defaultMarked.parse(token.raw)
            return `<div class="table-wrapper">${defaultTableHtml}</div>`
        },
    }
    return new Marked({ renderer })
}

const markedInstance = createMarkedInstance()

interface ChatMessageProps {
    message: ChatMessageType
    events?: AskAiEvent[]
    streamingContent?: string
    error?: ApiError | Error
    onRetry?: () => void
    onAskAgain?: (question: string) => void
    askAgainDisabled?: boolean
    onCountdownChange?: (countdown: number | null) => void
    showError?: boolean
}

const splitContentAndReferences = (
    content: string
): { mainContent: string; referencesJson: string | null } => {
    const startDelimiter = '<!--REFERENCES'
    const endDelimiter = '-->'

    const startIndex = content.indexOf(startDelimiter)
    if (startIndex === -1) {
        return { mainContent: content, referencesJson: null }
    }

    const endIndex = content.indexOf(endDelimiter, startIndex)
    if (endIndex === -1) {
        return { mainContent: content, referencesJson: null }
    }

    const mainContent = content.substring(0, startIndex).trim()
    const referencesJson = content
        .substring(startIndex + startDelimiter.length, endIndex)
        .trim()

    return { mainContent, referencesJson }
}

const getMessageState = (message: ChatMessageType) => ({
    isUser: message.type === 'user',
    isLoading: message.status === 'streaming',
    isComplete:
        message.status === 'complete' ||
        message.status === 'error' ||
        message.status === 'interrupted',
    hasError: message.status === 'error',
    isInterrupted: message.status === 'interrupted',
})

// Status message constants
const STATUS_MESSAGES = {
    THINKING: 'Thinking',
    ANALYZING: 'Analyzing results',
    GATHERING: 'Gathering resources',
    GENERATING: 'Generating',
} as const

// Helper to extract search query from tool call arguments
const tryParseSearchQuery = (argsJson: string): string | null => {
    try {
        const args = JSON.parse(argsJson)
        return args.searchQuery || args.query || null
    } catch {
        return null
    }
}

// Helper to get tool call status message
const getToolCallStatus = (event: AskAiEvent): string => {
    if (event.type !== EventTypes.TOOL_CALL) {
        return STATUS_MESSAGES.THINKING
    }

    const query = tryParseSearchQuery(event.arguments)
    return query ? `Searching for "${query}"` : `Using ${event.toolName}`
}

// Helper function for computing AI status - time-based latest status
const computeAiStatus = (
    events: AskAiEvent[],
    isComplete: boolean,
    content: string
): string | null => {
    if (isComplete) return null

    // Don't show status if there's an error event
    if (events.some((e) => e.type === EventTypes.ERROR)) {
        return null
    }

    // Get events sorted by timestamp (most recent last)
    const statusEvents = events
        .filter(
            (m) =>
                m.type === EventTypes.REASONING ||
                m.type === EventTypes.SEARCH_TOOL_CALL ||
                m.type === EventTypes.TOOL_CALL ||
                m.type === EventTypes.TOOL_RESULT ||
                m.type === EventTypes.MESSAGE_CHUNK
        )
        .sort((a, b) => a.timestamp - b.timestamp)

    // Get the most recent status-worthy event
    const latestEvent = statusEvents[statusEvents.length - 1]

    // If no events but we have content, we're generating
    // (streaming managed by singleton manager outside React)
    if (!latestEvent) {
        if (content) {
            if (content.includes('<!--REFERENCES')) {
                return STATUS_MESSAGES.GATHERING
            }
            return STATUS_MESSAGES.GENERATING
        }
        return STATUS_MESSAGES.THINKING
    }

    switch (latestEvent.type) {
        case EventTypes.REASONING:
            return latestEvent.message || STATUS_MESSAGES.THINKING

        case EventTypes.SEARCH_TOOL_CALL:
            return `Searching Elastic's Docs for "${latestEvent.searchQuery}"`

        case EventTypes.TOOL_CALL:
            return getToolCallStatus(latestEvent)

        case EventTypes.TOOL_RESULT:
            return STATUS_MESSAGES.ANALYZING

        case EventTypes.MESSAGE_CHUNK: {
            const allContent = events
                .filter((m) => m.type === EventTypes.MESSAGE_CHUNK)
                .map((m) => (m as ChunkEvent).content)
                .join('')

            if (allContent.includes('<!--REFERENCES')) {
                return STATUS_MESSAGES.GATHERING
            }
            return STATUS_MESSAGES.GENERATING
        }

        default:
            return STATUS_MESSAGES.THINKING
    }
}

// Action bar for complete AI messages
const ActionBar = ({
    content,
    messageId,
    onRetry,
}: {
    content: string
    messageId: string
    onRetry?: () => void
}) => {
    const { euiTheme } = useEuiTheme()
    const conversationId = useConversationId()
    const { selectedReaction, submitFeedback } = useMessageFeedback(
        messageId,
        conversationId
    )
    return (
        <div
            css={css`
                display: flex;
                gap: ${euiTheme.size.xxs};
                align-items: center;
                flex-direction: row-reverse;
            `}
        >
            <EuiToolTip content="Not helpful">
                <EuiButtonIcon
                    aria-label="This answer was not helpful"
                    iconType="thumbDown"
                    color="danger"
                    size="xs"
                    iconSize="s"
                    display={
                        selectedReaction === 'thumbsDown' ? 'base' : 'empty'
                    }
                    onClick={() => submitFeedback('thumbsDown')}
                />
            </EuiToolTip>
            <EuiToolTip content="Mark as helpful">
                <EuiButtonIcon
                    aria-label="This answer was helpful"
                    iconType="thumbUp"
                    color="success"
                    size="xs"
                    iconSize="s"
                    display={selectedReaction === 'thumbsUp' ? 'base' : 'empty'}
                    onClick={() => submitFeedback('thumbsUp')}
                />
            </EuiToolTip>
            <EuiCopy
                textToCopy={content}
                beforeMessage="Copy markdown"
                afterMessage="Copied!"
            >
                {(copy) => (
                    <EuiButtonIcon
                        aria-label="Copy markdown"
                        iconType="copy"
                        size="xs"
                        iconSize="s"
                        color="text"
                        onClick={copy}
                    />
                )}
            </EuiCopy>
            {onRetry && (
                <EuiToolTip content="Request a new answer">
                    <EuiButtonIcon
                        aria-label="Request a new answer"
                        iconType="refresh"
                        onClick={onRetry}
                        size="s"
                        iconSize="s"
                    />
                </EuiToolTip>
            )}
        </div>
    )
}

export const ChatMessage = ({
    message,
    events = [],
    streamingContent,
    error,
    onRetry,
    onAskAgain,
    askAgainDisabled = false,
    showError = true,
}: ChatMessageProps) => {
    const { euiTheme } = useEuiTheme()
    const { isUser, isLoading, isComplete, isInterrupted } =
        getMessageState(message)

    if (isUser) {
        return (
            <div
                data-message-type="user"
                data-message-id={message.id}
                css={css`
                    max-width: 50%;
                    justify-self: flex-end;
                    display: flex;
                    flex-direction: column;
                    align-items: flex-end;
                    margin-top: ${euiTheme.size.l};
                    .copy-button {
                        visibility: hidden;
                    }
                    &:hover .copy-button {
                        visibility: visible;
                    }
                `}
            >
                <EuiPanel
                    paddingSize="none"
                    hasShadow={false}
                    hasBorder={false}
                    css={css`
                        color: white;
                        border-radius: 24px 24px 2px 24px;
                        padding-inline: ${euiTheme.size.base};
                        padding-block: ${euiTheme.size.m};
                        background: ${aiGradients.dark};
                    `}
                >
                    <EuiText
                        size="s"
                        css={css`
                            white-space: pre-wrap;
                        `}
                    >
                        {message.content}
                    </EuiText>
                </EuiPanel>
                <EuiSpacer size="xs" />

                <EuiCopy
                    textToCopy={message.content}
                    beforeMessage="Copy"
                    afterMessage="Copied!"
                >
                    {(copy) => (
                        <EuiButtonIcon
                            className="copy-button"
                            aria-label="Copy"
                            iconType="copy"
                            iconSize="s"
                            color="text"
                            size="xs"
                            onClick={copy}
                        />
                    )}
                </EuiCopy>
            </div>
        )
    }

    // Use streamingContent during streaming, otherwise use message.content from store
    // message.content is updated atomically with status when CONVERSATION_END arrives
    const content = streamingContent || message.content

    const hasError = (message.status === 'error' || !!error) && showError

    // Don't render content for error messages that aren't being shown
    const shouldRenderContent =
        !message.status || message.status !== 'error' || hasError

    // Only split content and references when complete for better performance
    const { mainContent, referencesJson } = useMemo(() => {
        if (isComplete) {
            return splitContentAndReferences(content)
        }
        // During streaming, strip out unparsed references but don't parse them yet
        const startDelimiter = '<!--REFERENCES'
        const delimiterIndex = content.indexOf(startDelimiter)
        if (delimiterIndex !== -1) {
            return {
                mainContent: content.substring(0, delimiterIndex).trim(),
                referencesJson: null,
            }
        }
        return { mainContent: content, referencesJson: null }
    }, [content, isComplete])

    const parsed = useMemo(() => {
        const html = markedInstance.parse(mainContent) as string
        return DOMPurify.sanitize(html)
    }, [mainContent])

    const rawAiStatus = useMemo(
        () => computeAiStatus(events, isComplete, content),
        [events, isComplete, content]
    )

    // Apply minimum display time to prevent status flickering
    const aiStatus = useStatusMinDisplay(rawAiStatus)

    const ref = useRef<HTMLDivElement>(null)

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

    // Process internal docs links for htmx navigation
    useHtmxContainer(ref, [parsed])

    return (
        <EuiFlexGroup
            gutterSize="s"
            alignItems="flexStart"
            responsive={false}
            data-message-type="ai"
            data-message-id={message.id}
        >
            {(shouldRenderContent || hasError || isInterrupted) && (
                <EuiFlexItem>
                    <EuiPanel
                        paddingSize="none"
                        hasShadow={false}
                        hasBorder={false}
                        css={css`
                            padding-top: 8px;
                        `}
                    >
                        {content && !hasError && (
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

                        {isComplete &&
                            content &&
                            !isInterrupted &&
                            !hasError && (
                                <>
                                    <EuiSpacer size="m" />
                                    <ActionBar
                                        content={mainContent}
                                        messageId={message.id}
                                        onRetry={onRetry}
                                    />
                                </>
                            )}

                        {(isInterrupted || hasError) && (
                            <>
                                <EuiSpacer size="m" />
                                <EuiFlexGroup
                                    justifyContent="flexEnd"
                                    gutterSize="s"
                                    alignItems="center"
                                    responsive={false}
                                >
                                    <EuiFlexItem grow={false}>
                                        <EuiText
                                            size="xs"
                                            color="subdued"
                                            css={css`
                                                font-style: italic;
                                            `}
                                        >
                                            {isInterrupted
                                                ? content
                                                    ? 'Response stopped early.'
                                                    : 'Response stopped before starting.'
                                                : 'Something went wrong.'}
                                        </EuiText>
                                    </EuiFlexItem>
                                    {onAskAgain && message.question && (
                                        <EuiFlexItem grow={false}>
                                            <EuiButtonEmpty
                                                iconType="editorRedo"
                                                size="xs"
                                                disabled={askAgainDisabled}
                                                onClick={() =>
                                                    onAskAgain(
                                                        message.question!
                                                    )
                                                }
                                            >
                                                Ask again
                                            </EuiButtonEmpty>
                                        </EuiFlexItem>
                                    )}
                                </EuiFlexGroup>
                            </>
                        )}
                    </EuiPanel>
                </EuiFlexItem>
            )}
        </EuiFlexGroup>
    )
}
