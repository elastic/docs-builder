import { MarkdownContent } from '../shared/MarkdownContent'
import { startAskAiStream } from '../shared/askAiStreamClient'
import type { SearchResultItem } from './useFullPageSearchQuery'
import {
    EuiButtonIcon,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiLink,
    EuiPanel,
    EuiText,
    EuiToolTip,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, useEffect, useRef, useCallback } from 'react'

interface AIAnswerPanelProps {
    query: string
    inputQuery: string
    results: SearchResultItem[]
    visible: boolean
    forceCollapsed?: boolean
}

const PREVIEW_HEIGHT = 64 // Height to show content with read more overlay

// Status badge component for AI answer states
const StatusBadge = ({
    streaming,
    hasContent,
    justCompleted,
    isWaiting,
    euiTheme,
}: {
    streaming: boolean
    hasContent: boolean
    justCompleted: boolean
    isWaiting: boolean
    euiTheme: ReturnType<typeof useEuiTheme>['euiTheme']
}) => {
    // Determine the state
    const isThinking = streaming && !hasContent
    const isGenerating = streaming && hasContent
    const isReady = !streaming && hasContent && justCompleted

    if (!isThinking && !isGenerating && !isReady && !isWaiting) return null

    let text = ''
    let background = 'rgba(255, 255, 255, 0.2)'
    let animation = ''

    if (isWaiting) {
        text = 'Waiting...'
        animation = 'pulse 1.5s ease-in-out infinite'
    } else if (isThinking) {
        text = 'Thinking...'
        animation = 'pulse 1.5s ease-in-out infinite'
    } else if (isGenerating) {
        text = 'Generating...'
        animation = 'pulse 1.5s ease-in-out infinite'
    } else if (isReady) {
        text = 'Ready!'
        background = '#00BFA5'
        animation = 'wiggle 0.5s ease-in-out 3'
    }

    return (
        <span
            css={css`
                font-size: 12px;
                color: white;
                background: ${background};
                padding: 2px 8px;
                border-radius: ${euiTheme.border.radius.medium};
                animation: ${animation};
                font-weight: ${isReady ? '600' : '400'};

                @keyframes pulse {
                    0%,
                    100% {
                        background: rgba(255, 255, 255, 0.1);
                    }
                    50% {
                        background: rgba(255, 255, 255, 0.4);
                    }
                }

                @keyframes wiggle {
                    0%,
                    100% {
                        transform: rotate(0deg);
                    }
                    25% {
                        transform: rotate(-3deg) scale(1.1);
                    }
                    75% {
                        transform: rotate(3deg) scale(1.1);
                    }
                }
            `}
        >
            {text}
        </span>
    )
}

export const AIAnswerPanel = ({
    query,
    inputQuery,
    results,
    visible,
    forceCollapsed = false,
}: AIAnswerPanelProps) => {
    const { euiTheme } = useEuiTheme()
    const [content, setContent] = useState('')
    const [streaming, setStreaming] = useState(false)
    const [expanded, setExpanded] = useState(false)
    const [showSources, setShowSources] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const [unavailable, setUnavailable] = useState(false)
    const [justCompleted, setJustCompleted] = useState(false)
    const [isWaiting, setIsWaiting] = useState(false)
    const abortControllerRef = useRef<AbortController | null>(null)
    const streamingQueryRef = useRef<string | null>(null)
    const answeredQueryRef = useRef<string | null>(null)
    const resultsRef = useRef<SearchResultItem[]>(results)
    const mountedRef = useRef(true)
    const collapsedHeaderRef = useRef<HTMLDivElement>(null)
    const expandedHeaderRef = useRef<HTMLDivElement>(null)
    const waitingDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(
        null
    )

    // Track the query we want to stream for - set when query changes
    const pendingQueryRef = useRef<string | null>(null)

    // Streaming function - only called when we have results and want to stream
    const doStream = useCallback(
        async (currentQuery: string, currentResults: SearchResultItem[]) => {
            if (!currentQuery || currentResults.length === 0) return

            // Already answered this query
            if (answeredQueryRef.current === currentQuery) {
                return
            }

            // Already streaming this query
            if (streamingQueryRef.current === currentQuery) {
                return
            }

            // Abort any existing stream before starting a new one
            if (abortControllerRef.current) {
                abortControllerRef.current.abort()
            }

            streamingQueryRef.current = currentQuery

            setContent('')
            setStreaming(true)
            setShowSources(false)
            setError(null)
            setUnavailable(false)
            setIsWaiting(false)

            const controller = new AbortController()
            abortControllerRef.current = controller

            // Track accumulated content for this stream
            let accumulatedContent = ''

            try {
                const contextDocs = currentResults.slice(0, 5).map((r) => ({
                    title: r.title,
                    url: r.url,
                    content: r.aiRagOptimizedSummary || r.description,
                }))

                const contextText = contextDocs
                    .map(
                        (d) =>
                            `Title: ${d.title}\nURL: ${d.url}\nContent: ${d.content}`
                    )
                    .join('\n\n---\n\n')

                const messageWithContext = `Based on the following documentation context, answer this question: "${currentQuery}"\n\nContext:\n${contextText}`

                await startAskAiStream({
                    message: messageWithContext,
                    signal: controller.signal,
                    callbacks: {
                        onEvent: (event) => {
                            // Ignore events if component unmounted or this stream was superseded
                            if (!mountedRef.current) return
                            if (streamingQueryRef.current !== currentQuery)
                                return

                            if (event.type === 'message_chunk') {
                                accumulatedContent += event.content
                                setContent(accumulatedContent)
                            } else if (event.type === 'error') {
                                streamingQueryRef.current = null
                                setError(event.message || 'An error occurred')
                                setStreaming(false)
                            } else if (event.type === 'conversation_end') {
                                answeredQueryRef.current = currentQuery
                                streamingQueryRef.current = null
                                setStreaming(false)
                                setShowSources(true)
                                setJustCompleted(true)
                            }
                        },
                        onError: (err) => {
                            // Ignore errors from superseded streams
                            if (
                                streamingQueryRef.current !== currentQuery &&
                                streamingQueryRef.current !== null
                            ) {
                                return
                            }
                            streamingQueryRef.current = null
                            if (!mountedRef.current) return

                            setStreaming(false)
                            // Don't show error/unavailable for intentional aborts (new query typed)
                            if (err.name === 'AbortError') {
                                return
                            }
                            setError(err.message || 'Failed to get AI answer')
                            setUnavailable(true)
                        },
                    },
                })

                // Stream completed successfully (normal close)
                if (
                    mountedRef.current &&
                    streamingQueryRef.current === currentQuery
                ) {
                    answeredQueryRef.current = currentQuery
                    streamingQueryRef.current = null
                    setStreaming(false)
                    setShowSources(true)
                    setJustCompleted(true)
                }
            } catch (err) {
                // Ignore errors from superseded streams
                if (
                    streamingQueryRef.current !== currentQuery &&
                    streamingQueryRef.current !== null
                ) {
                    return
                }
                streamingQueryRef.current = null
                if (!mountedRef.current) {
                    // Component unmounted (likely StrictMode), don't update state
                    return
                }
                setStreaming(false)
                // Don't show error/unavailable for intentional aborts
                if (err instanceof Error && err.name === 'AbortError') {
                    return
                }
                setError(
                    err instanceof Error
                        ? err.message
                        : 'Failed to get AI answer'
                )
                setUnavailable(true)
            }
        },
        []
    )

    // Keep resultsRef in sync (no side effects, just sync the ref)
    useEffect(() => {
        resultsRef.current = results
    }, [results])

    // Debounced AI query - automatically submit to AI when user stops typing
    useEffect(() => {
        // Clear any existing debounce timer
        if (waitingDebounceRef.current) {
            clearTimeout(waitingDebounceRef.current)
            waitingDebounceRef.current = null
        }

        // If input query differs from submitted query, user is typing something new
        const trimmedInput = inputQuery.trim()
        const isTypingNewQuery =
            trimmedInput !== query.trim() && trimmedInput !== ''

        if (isTypingNewQuery) {
            // Show waiting state immediately
            setIsWaiting(true)
            setJustCompleted(false) // Clear "Ready!" state

            // Debounce: wait 500ms after user stops typing, then submit to AI
            waitingDebounceRef.current = setTimeout(() => {
                // Reset answered ref to allow streaming the new query
                answeredQueryRef.current = null
                // Start streaming with the typed query using current results as context
                // doStream will handle aborting any existing stream
                if (resultsRef.current.length > 0) {
                    doStream(trimmedInput, resultsRef.current)
                }
            }, 500)
        } else {
            // User cleared input or input matches submitted query
            setIsWaiting(false)
        }

        return () => {
            if (waitingDebounceRef.current) {
                clearTimeout(waitingDebounceRef.current)
            }
        }
    }, [inputQuery, query, doStream])

    // Single effect to handle streaming - triggered by query or results changes
    useEffect(() => {
        // Demo mode: fail=ai in query string forces unavailable state
        const params = new URLSearchParams(window.location.search)
        if (params.get('fail') === 'ai') {
            setUnavailable(true)
            return
        }

        if (!visible || !query) return

        // Track that we want to stream this query
        pendingQueryRef.current = query

        // If we already answered or are streaming this query, don't start again
        if (
            answeredQueryRef.current === query ||
            streamingQueryRef.current === query
        ) {
            return
        }

        // If we have results, start streaming
        if (results.length > 0) {
            doStream(query, results)
        }
        // No cleanup - we don't abort on re-renders, only on unmount or explicit stop
    }, [visible, query, results, doStream])

    // Track mounted state - don't abort on unmount to handle StrictMode
    // The stream will complete in the background and mounted check prevents state updates
    useEffect(() => {
        mountedRef.current = true
        return () => {
            mountedRef.current = false
        }
    }, [])

    // Collapse when forceCollapsed changes (facets, page, sort changed)
    useEffect(() => {
        if (forceCollapsed) {
            setExpanded(false)
        }
    }, [forceCollapsed])

    // Control gradient animation play state using Web Animations API
    // Pause immediately on mount if not streaming, and control based on streaming state
    useEffect(() => {
        const pauseAnimations = (el: HTMLDivElement | null) => {
            if (el) {
                const animations = el.getAnimations()
                animations.forEach((anim) => {
                    if (streaming) {
                        anim.play()
                    } else {
                        anim.pause()
                    }
                })
            }
        }

        pauseAnimations(collapsedHeaderRef.current)
        pauseAnimations(expandedHeaderRef.current)
    }, [streaming, expanded]) // Re-run when expanded changes to catch new elements

    const handleStop = () => {
        abortControllerRef.current?.abort()
        streamingQueryRef.current = null
        setStreaming(false)
    }

    const handleRegenerate = () => {
        // Reset refs to allow regeneration
        answeredQueryRef.current = null
        streamingQueryRef.current = null
        doStream(query, results)
    }

    if (!visible) return null

    // Unavailable state - show unobtrusive gray box
    if (unavailable && !content && !streaming) {
        return (
            <EuiPanel
                paddingSize="none"
                css={css`
                    margin-bottom: ${euiTheme.size.m};
                    background: ${euiTheme.colors.lightestShade};
                    border: 1px solid ${euiTheme.colors.lightShade};
                    overflow: hidden;
                `}
            >
                <div
                    css={css`
                        padding: ${euiTheme.size.s} ${euiTheme.size.m};
                    `}
                >
                    <EuiFlexGroup alignItems="center" gutterSize="s">
                        <EuiFlexItem grow={false}>
                            <EuiIcon type="sparkles" color="subdued" />
                        </EuiFlexItem>
                        <EuiFlexItem>
                            <EuiText size="s" color="subdued">
                                <strong>AI Answer Unavailable</strong>
                            </EuiText>
                        </EuiFlexItem>
                        <EuiFlexItem grow={false}>
                            <EuiToolTip content="Retry">
                                <EuiButtonIcon
                                    iconType="refresh"
                                    aria-label="Retry"
                                    onClick={handleRegenerate}
                                    size="s"
                                    color="text"
                                />
                            </EuiToolTip>
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </div>
            </EuiPanel>
        )
    }

    // Nothing to show yet
    if (!content && !streaming && !error) return null

    // Collapsed/Preview state - show fixed height with content preview (including while streaming)
    if (!expanded && (content || streaming)) {
        return (
            <EuiPanel
                paddingSize="none"
                css={css`
                    margin-bottom: ${euiTheme.size.m};
                    background: ${euiTheme.colors.emptyShade};
                    border: 1px solid #9b59b620;
                    cursor: pointer;
                    transition: all 0.2s ease;
                    overflow: hidden;

                    &:hover {
                        border-color: #9b59b640;
                        box-shadow: 0 2px 8px rgba(155, 89, 182, 0.15);
                    }
                `}
                onClick={() => {
                    setExpanded(true)
                    setJustCompleted(false)
                }}
            >
                {/* Purple-blue gradient header with animation while streaming */}
                <div
                    ref={collapsedHeaderRef}
                    css={css`
                        padding: ${euiTheme.size.s} ${euiTheme.size.m};
                        background: linear-gradient(
                            135deg,
                            #9b59b6 0%,
                            #0077cc 25%,
                            #9b59b6 50%,
                            #0077cc 75%,
                            #9b59b6 100%
                        );
                        background-size: 200% 200%;
                        animation: diagonalFlow 3s linear infinite;

                        @keyframes diagonalFlow {
                            0% {
                                background-position: 0% 0%;
                            }
                            100% {
                                background-position: 100% 100%;
                            }
                        }
                    `}
                >
                    <EuiFlexGroup alignItems="center" gutterSize="s">
                        <EuiFlexItem grow={false}>
                            <EuiIcon type="sparkles" color="ghost" />
                        </EuiFlexItem>
                        <EuiFlexItem grow={false}>
                            <div
                                css={css`
                                    display: flex;
                                    align-items: center;
                                    gap: 8px;
                                `}
                            >
                                <EuiText
                                    size="s"
                                    css={css`
                                        color: white;
                                    `}
                                >
                                    <strong>AI Answer</strong>
                                </EuiText>
                                <StatusBadge
                                    streaming={streaming}
                                    hasContent={!!content}
                                    justCompleted={justCompleted}
                                    isWaiting={isWaiting}
                                    euiTheme={euiTheme}
                                />
                            </div>
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </div>
                {/* Content preview with fade overlay */}
                <div
                    css={css`
                        background: ${euiTheme.colors.emptyShade};
                        position: relative;
                    `}
                >
                    <div
                        css={css`
                            padding: ${euiTheme.size.m} ${euiTheme.size.m};
                            max-height: ${PREVIEW_HEIGHT}px;
                            overflow: hidden;
                        `}
                    >
                        {content ? (
                            <>
                                <MarkdownContent
                                    content={content}
                                    enableCopyButtons={false}
                                />
                                {streaming && (
                                    <span
                                        css={css`
                                            display: inline-block;
                                            width: 8px;
                                            height: 16px;
                                            background: #9b59b6;
                                            margin-left: 4px;
                                            animation: blink 0.8s infinite;

                                            @keyframes blink {
                                                0%,
                                                100% {
                                                    opacity: 1;
                                                }
                                                50% {
                                                    opacity: 0;
                                                }
                                            }
                                        `}
                                    />
                                )}
                            </>
                        ) : (
                            <span
                                css={css`
                                    display: inline-block;
                                    width: 8px;
                                    height: 16px;
                                    background: #9b59b6;
                                    animation: blink 0.8s infinite;

                                    @keyframes blink {
                                        0%,
                                        100% {
                                            opacity: 1;
                                        }
                                        50% {
                                            opacity: 0;
                                        }
                                    }
                                `}
                            />
                        )}
                    </div>
                    {/* Fade overlay */}
                    <div
                        css={css`
                            position: absolute;
                            bottom: 0;
                            left: 0;
                            right: 0;
                            height: 50px;
                            background: linear-gradient(
                                rgba(255, 255, 255, 0),
                                rgba(255, 255, 255, 0.9) 40%,
                                rgba(255, 255, 255, 1)
                            );
                            pointer-events: none;
                        `}
                    />
                    {/* "Read more" positioned above overlay */}
                    <div
                        css={css`
                            position: absolute;
                            bottom: 0;
                            left: 0;
                            right: 0;
                            display: flex;
                            flex-direction: column;
                            align-items: center;
                            padding-bottom: ${euiTheme.size.xs};
                            pointer-events: none;
                            z-index: 1;
                        `}
                    >
                        <span
                            css={css`
                                font-size: 11px;
                                color: ${euiTheme.colors.darkestShade};
                                font-weight: 500;
                                text-transform: uppercase;
                                letter-spacing: 0.5px;
                            `}
                        >
                            read more
                        </span>
                        <EuiIcon
                            type="arrowDown"
                            size="s"
                            color={euiTheme.colors.darkestShade}
                        />
                    </div>
                </div>
            </EuiPanel>
        )
    }

    return (
        <EuiPanel
            paddingSize="none"
            css={css`
                margin-bottom: ${euiTheme.size.m};
                background: ${euiTheme.colors.emptyShade};
                border: 1px solid #9b59b630;
                border-radius: ${euiTheme.border.radius.medium};
            `}
        >
            {/* Purple-blue gradient header with animation while streaming - sticky when expanded */}
            <div
                ref={expandedHeaderRef}
                css={css`
                    position: sticky;
                    top: 0;
                    z-index: 10;
                    padding: ${euiTheme.size.m};
                    background: linear-gradient(
                        135deg,
                        #9b59b6 0%,
                        #0077cc 25%,
                        #9b59b6 50%,
                        #0077cc 75%,
                        #9b59b6 100%
                    );
                    background-size: 200% 200%;
                    animation: diagonalFlow 3s linear infinite;
                    border-radius: ${euiTheme.border.radius.medium}
                        ${euiTheme.border.radius.medium} 0 0;
                    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);

                    @keyframes diagonalFlow {
                        0% {
                            background-position: 0% 0%;
                        }
                        100% {
                            background-position: 100% 100%;
                        }
                    }
                `}
            >
                <EuiFlexGroup alignItems="center" justifyContent="spaceBetween">
                    <EuiFlexItem grow={false}>
                        <EuiFlexGroup alignItems="center" gutterSize="s">
                            <EuiFlexItem grow={false}>
                                <EuiIcon type="sparkles" color="ghost" />
                            </EuiFlexItem>
                            <EuiFlexItem grow={false}>
                                <div
                                    css={css`
                                        display: flex;
                                        align-items: center;
                                        gap: 8px;
                                    `}
                                >
                                    <EuiText
                                        size="s"
                                        css={css`
                                            color: white;
                                        `}
                                    >
                                        <strong>AI Answer</strong>
                                    </EuiText>
                                    <StatusBadge
                                        streaming={streaming}
                                        hasContent={!!content}
                                        justCompleted={justCompleted}
                                        isWaiting={isWaiting}
                                        euiTheme={euiTheme}
                                    />
                                </div>
                            </EuiFlexItem>
                        </EuiFlexGroup>
                    </EuiFlexItem>
                    <EuiFlexItem grow={false}>
                        <EuiFlexGroup gutterSize="xs">
                            {streaming ? (
                                <EuiFlexItem grow={false}>
                                    <EuiToolTip content="Stop">
                                        <EuiButtonIcon
                                            iconType="stop"
                                            aria-label="Stop"
                                            onClick={handleStop}
                                            size="s"
                                            css={css`
                                                color: white !important;
                                            `}
                                        />
                                    </EuiToolTip>
                                </EuiFlexItem>
                            ) : (
                                <EuiFlexItem grow={false}>
                                    <EuiToolTip content="Regenerate">
                                        <EuiButtonIcon
                                            iconType="refresh"
                                            aria-label="Regenerate"
                                            onClick={handleRegenerate}
                                            size="s"
                                            css={css`
                                                color: white !important;
                                            `}
                                        />
                                    </EuiToolTip>
                                </EuiFlexItem>
                            )}
                            {content && (
                                <EuiFlexItem grow={false}>
                                    <EuiToolTip content="Collapse">
                                        <EuiButtonIcon
                                            iconType="arrowUp"
                                            aria-label="Collapse"
                                            onClick={() => setExpanded(false)}
                                            size="s"
                                            css={css`
                                                color: white !important;
                                            `}
                                        />
                                    </EuiToolTip>
                                </EuiFlexItem>
                            )}
                        </EuiFlexGroup>
                    </EuiFlexItem>
                </EuiFlexGroup>
            </div>

            {/* Content body - white background */}
            <div
                css={css`
                    padding: ${euiTheme.size.m};
                    background: ${euiTheme.colors.emptyShade};
                `}
            >
                {error ? (
                    <EuiText color="danger" size="s">
                        {error}
                    </EuiText>
                ) : (
                    <>
                        <MarkdownContent
                            content={content}
                            enableCopyButtons={!streaming}
                            copyButtonPrefix="ai-answer-codecell-"
                        />
                        {streaming && (
                            <span
                                css={css`
                                    display: inline-block;
                                    width: 8px;
                                    height: 16px;
                                    background: #9b59b6;
                                    margin-left: 4px;
                                    animation: blink 0.8s infinite;

                                    @keyframes blink {
                                        0%,
                                        100% {
                                            opacity: 1;
                                        }
                                        50% {
                                            opacity: 0;
                                        }
                                    }
                                `}
                            />
                        )}
                    </>
                )}

                {showSources && results.length > 0 && (
                    <div
                        css={css`
                            margin-top: ${euiTheme.size.m};
                            padding-top: ${euiTheme.size.m};
                            border-top: 1px solid rgba(0, 119, 204, 0.2);
                        `}
                    >
                        <EuiText
                            size="xs"
                            css={css`
                                color: #0077cc;
                            `}
                        >
                            Sources
                        </EuiText>
                        <EuiFlexGroup
                            gutterSize="s"
                            wrap
                            css={css`
                                margin-top: ${euiTheme.size.xs};
                            `}
                        >
                            {results.slice(0, 3).map((result, idx) => (
                                <EuiFlexItem key={idx} grow={false}>
                                    <EuiLink
                                        href={result.url}
                                        css={css`
                                            font-size: 12px;
                                            background: rgba(0, 119, 204, 0.1);
                                            padding: 4px 8px;
                                            border-radius: ${euiTheme.border
                                                .radius.medium};
                                            color: #0077cc !important;

                                            &:hover {
                                                background: rgba(
                                                    0,
                                                    119,
                                                    204,
                                                    0.2
                                                );
                                            }
                                        `}
                                    >
                                        {result.title}
                                    </EuiLink>
                                </EuiFlexItem>
                            ))}
                        </EuiFlexGroup>
                    </div>
                )}

                <EuiText
                    size="xs"
                    color="subdued"
                    css={css`
                        margin-top: ${euiTheme.size.m};
                    `}
                >
                    AI-generated answer. Always verify with official docs.
                </EuiText>
            </div>
        </EuiPanel>
    )
}
