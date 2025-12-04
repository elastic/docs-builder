import { initCopyButton } from '../../../copybutton'
import { hljs } from '../../../hljs'
import { aiGradients } from '../ElasticAiAssitant'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import { ApiError } from '../errorHandling'
import { AskAiEvent, ChunkEvent, EventTypes } from './AskAiEvent'
import { GeneratingStatus } from './GeneratingStatus'
import { References } from './RelatedResources'
import { ChatMessage as ChatMessageType } from './chat.store'
import { useStatusMinDisplay } from './useStatusMinDisplay'
import {
    EuiButtonIcon,
    EuiCopy,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
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
    isComplete: message.status === 'complete' || message.status === 'error',
    hasError: message.status === 'error',
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
    onRetry,
}: {
    content: string
    onRetry?: () => void
}) => (
    <EuiFlexGroup
        responsive={false}
        component="span"
        gutterSize="none"
        direction="rowReverse"
    >
        <EuiFlexItem grow={false}>
            <EuiToolTip content="Not helpful">
                <EuiButtonIcon
                    aria-label="This answer was not helpful"
                    iconType="thumbDown"
                    color="danger"
                    size="xs"
                    iconSize="s"
                />
            </EuiToolTip>
        </EuiFlexItem>
        <EuiFlexItem grow={false}>
            <EuiToolTip content="Mark as helpful">
                <EuiButtonIcon
                    aria-label="This answer was helpful"
                    iconType="thumbUp"
                    color="success"
                    size="xs"
                    iconSize="s"
                />
            </EuiToolTip>
        </EuiFlexItem>
        <EuiFlexItem grow={false}>
            <EuiCopy
                textToCopy={content}
                beforeMessage="Copy as markdown"
                afterMessage="Copied!"
            >
                {(copy) => (
                    <EuiButtonIcon
                        aria-label="Copy markdown"
                        iconType="copy"
                        size="xs"
                        iconSize="s"
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
                        iconSize="s"
                    />
                </EuiToolTip>
            </EuiFlexItem>
        )}
    </EuiFlexGroup>
)

export const ChatMessage = ({
    message,
    events = [],
    streamingContent,
    error,
    onRetry,
    showError = true,
}: ChatMessageProps) => {
    const { euiTheme } = useEuiTheme()
    const { isUser, isLoading, isComplete } = getMessageState(message)

    if (isUser) {
        return (
            <>
                <EuiSpacer size="l" />
                <div
                    data-message-type="user"
                    data-message-id={message.id}
                    css={css`
                        max-width: 50%;
                        justify-self: flex-end;
                        display: flex;
                        flex-direction: column;
                        align-items: flex-end;
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
            </>
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

    return (
        <EuiFlexGroup
            gutterSize="s"
            alignItems="flexStart"
            responsive={false}
            data-message-type="ai"
            data-message-id={message.id}
        >
            {!hasError && shouldRenderContent && (
                <EuiFlexItem>
                    <EuiPanel
                        paddingSize="none"
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
                                <ActionBar
                                    content={mainContent}
                                    onRetry={onRetry}
                                />
                            </>
                        )}
                    </EuiPanel>
                </EuiFlexItem>
            )}
            {hasError && (
                <EuiFlexItem>
                    <EuiFlexGroup
                        gutterSize="s"
                        alignItems="flexStart"
                        responsive={false}
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
                                <EuiIcon
                                    name="Elastic Docs AI"
                                    size="xl"
                                    type="logoElastic"
                                />
                            </div>
                        </EuiFlexItem>
                        <EuiFlexItem>
                            <SearchOrAskAiErrorCallout
                                error={message.error || error || null}
                                domain="askAi"
                                inline={true}
                            />
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </EuiFlexItem>
            )}
        </EuiFlexGroup>
    )
}
