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
import type { SearchResultItem } from './useFullPageSearchQuery'
import { MarkdownContent } from '../shared/MarkdownContent'

interface AIAnswerPanelProps {
    query: string
    results: SearchResultItem[]
    visible: boolean
    forceCollapsed?: boolean
}

const PREVIEW_HEIGHT = 48 // Height to show approximately one line

export const AIAnswerPanel = ({
    query,
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
    const abortControllerRef = useRef<AbortController | null>(null)
    const streamingQueryRef = useRef<string | null>(null)
    const answeredQueryRef = useRef<string | null>(null)
    const resultsRef = useRef<SearchResultItem[]>(results)
    const prevResultsLengthRef = useRef(0)
    const mountedRef = useRef(true)

    // Track the query we want to stream for - set when query changes
    const pendingQueryRef = useRef<string | null>(null)

    // Streaming function - only called when we have results and want to stream
    const doStream = useCallback(async (currentQuery: string, currentResults: SearchResultItem[]) => {
        if (!currentQuery || currentResults.length === 0) return

        // Already answered this query
        if (answeredQueryRef.current === currentQuery) {
            return
        }

        // Already streaming this query
        if (streamingQueryRef.current === currentQuery) {
            return
        }

        streamingQueryRef.current = currentQuery

        setContent('')
        setStreaming(true)
        setShowSources(false)
        setError(null)
        setUnavailable(false)

        const controller = new AbortController()
        abortControllerRef.current = controller

        try {
            const contextDocs = currentResults.slice(0, 5).map((r) => ({
                title: r.title,
                url: r.url,
                content: r.aiRagOptimizedSummary || r.description,
            }))

            const contextText = contextDocs
                .map((d) => `Title: ${d.title}\nURL: ${d.url}\nContent: ${d.content}`)
                .join('\n\n---\n\n')

            const messageWithContext = `Based on the following documentation context, answer this question: "${currentQuery}"\n\nContext:\n${contextText}`

            const response = await fetch('/docs/_api/v1/ask-ai/stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    message: messageWithContext,
                    conversationId: null,
                }),
                signal: controller.signal,
            })

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`)
            }

            const reader = response.body?.getReader()
            if (!reader) {
                throw new Error('No response body')
            }

            const decoder = new TextDecoder()
            let buffer = ''

            while (true) {
                const { done, value } = await reader.read()
                if (done) break

                buffer += decoder.decode(value, { stream: true })

                const lines = buffer.split('\n')
                buffer = lines.pop() || ''

                for (const line of lines) {
                    if (line.startsWith('data:')) {
                        const data = line.slice(5).trim()
                        if (data === '[DONE]') {
                            answeredQueryRef.current = currentQuery
                            streamingQueryRef.current = null
                            if (mountedRef.current) {
                                setStreaming(false)
                                setShowSources(true)
                            }
                            return
                        }

                        try {
                            const parsed = JSON.parse(data)
                            if (parsed.content && mountedRef.current) {
                                setContent((prev) => prev + parsed.content)
                            } else if (parsed.error) {
                                streamingQueryRef.current = null
                                if (mountedRef.current) {
                                    setError(parsed.error)
                                    setStreaming(false)
                                }
                                return
                            }
                        } catch {
                            if (data && data !== '[DONE]' && mountedRef.current) {
                                setContent((prev) => prev + data)
                            }
                        }
                    }
                }
            }

            answeredQueryRef.current = currentQuery
            streamingQueryRef.current = null
            if (mountedRef.current) {
                setStreaming(false)
                setShowSources(true)
            }
        } catch (err) {
            streamingQueryRef.current = null
            if (!mountedRef.current) {
                // Component unmounted (likely StrictMode), don't update state
                return
            }
            setStreaming(false)
            if (err instanceof Error && err.name === 'AbortError') {
                // Only show unavailable if this was the current pending query and we're still mounted
                if (pendingQueryRef.current === currentQuery && mountedRef.current) {
                    setUnavailable(true)
                }
                return
            }
            setError(err instanceof Error ? err.message : 'Failed to get AI answer')
            setUnavailable(true)
        }
    }, [])

    // Keep resultsRef in sync (no side effects, just sync the ref)
    useEffect(() => {
        resultsRef.current = results
    }, [results])

    // Single effect to handle streaming - triggered by query or results changes
    useEffect(() => {
        // Demo mode: fa=1 in query string forces unavailable state
        const params = new URLSearchParams(window.location.search)
        if (params.get('fa') === '1') {
            setUnavailable(true)
            return
        }

        if (!visible || !query) return

        // Track that we want to stream this query
        pendingQueryRef.current = query

        // If we already answered or are streaming this query, don't start again
        if (answeredQueryRef.current === query || streamingQueryRef.current === query) {
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

    const handleStop = () => {
        abortControllerRef.current?.abort()
        streamingQueryRef.current = null
        setStreaming(false)
    }

    const handleRegenerate = () => {
        // Reset refs to allow regeneration
        answeredQueryRef.current = null
        streamingQueryRef.current = null
        streamAnswer(query, results)
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
                                    color="subdued"
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
                    background: ${euiTheme.colors.success}15;
                    border: 1px solid ${euiTheme.colors.success}40;
                    cursor: pointer;
                    transition: all 0.2s ease;
                    overflow: hidden;

                    &:hover {
                        background: ${euiTheme.colors.success}20;
                    }
                `}
                onClick={() => setExpanded(true)}
            >
                {/* Header */}
                <div
                    css={css`
                        padding: ${euiTheme.size.s} ${euiTheme.size.m};
                        border-bottom: 1px solid ${euiTheme.colors.success}30;
                    `}
                >
                    <EuiFlexGroup alignItems="center" gutterSize="s">
                        <EuiFlexItem grow={false}>
                            <EuiIcon type="sparkles" color="success" />
                        </EuiFlexItem>
                        <EuiFlexItem>
                            <EuiText size="s" css={css`color: ${euiTheme.colors.success};`}>
                                <strong>AI Answer</strong>
                            </EuiText>
                        </EuiFlexItem>
                        {streaming && (
                            <EuiFlexItem grow={false}>
                                <span
                                    css={css`
                                        font-size: 12px;
                                        color: ${euiTheme.colors.success};
                                        background: ${euiTheme.colors.success}20;
                                        padding: 2px 8px;
                                        border-radius: ${euiTheme.border.radius.medium};
                                        animation: pulse 1.5s infinite;

                                        @keyframes pulse {
                                            0%, 100% { opacity: 1; }
                                            50% { opacity: 0.5; }
                                        }
                                    `}
                                >
                                    Generating...
                                </span>
                            </EuiFlexItem>
                        )}
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
                            padding: ${euiTheme.size.s} ${euiTheme.size.m};
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
                                            background: ${euiTheme.colors.success};
                                            margin-left: 4px;
                                            animation: blink 0.8s infinite;

                                            @keyframes blink {
                                                0%, 100% { opacity: 1; }
                                                50% { opacity: 0; }
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
                                    background: ${euiTheme.colors.success};
                                    animation: blink 0.8s infinite;

                                    @keyframes blink {
                                        0%, 100% { opacity: 1; }
                                        50% { opacity: 0; }
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
                            height: 40px;
                            background: linear-gradient(rgba(255, 255, 255, 0), rgba(255, 255, 255, 1));
                            display: flex;
                            justify-content: center;
                            align-items: flex-end;
                            padding-bottom: ${euiTheme.size.xs};
                            pointer-events: none;
                        `}
                    >
                        <EuiIcon type="arrowDown" size="s" color="subdued" />
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
                background: ${euiTheme.colors.success}15;
                border: 1px solid ${euiTheme.colors.success}40;
                overflow: hidden;
            `}
        >
            {/* Header */}
            <div
                css={css`
                    padding: ${euiTheme.size.m};
                    border-bottom: 1px solid ${euiTheme.colors.success}30;
                `}
            >
                <EuiFlexGroup alignItems="center" justifyContent="spaceBetween">
                    <EuiFlexItem grow={false}>
                        <EuiFlexGroup alignItems="center" gutterSize="s">
                            <EuiFlexItem grow={false}>
                                <EuiIcon type="sparkles" color="success" />
                            </EuiFlexItem>
                            <EuiFlexItem>
                                <EuiText size="s" css={css`color: ${euiTheme.colors.success};`}>
                                    <strong>AI Answer</strong>
                                </EuiText>
                            </EuiFlexItem>
                            {streaming && (
                                <EuiFlexItem grow={false}>
                                    <span
                                        css={css`
                                            font-size: 12px;
                                            color: ${euiTheme.colors.success};
                                            background: ${euiTheme.colors.success}20;
                                            padding: 2px 8px;
                                            border-radius: ${euiTheme.border.radius.medium};
                                            animation: pulse 1.5s infinite;

                                            @keyframes pulse {
                                                0%, 100% { opacity: 1; }
                                                50% { opacity: 0.5; }
                                            }
                                        `}
                                    >
                                        Generating...
                                    </span>
                                </EuiFlexItem>
                            )}
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
                                    background: ${euiTheme.colors.success};
                                    margin-left: 4px;
                                    animation: blink 0.8s infinite;

                                    @keyframes blink {
                                        0%, 100% { opacity: 1; }
                                        50% { opacity: 0; }
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
                            border-top: 1px solid ${euiTheme.colors.lightShade};
                        `}
                    >
                        <EuiText size="xs" color="subdued">
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
                                            background: ${euiTheme.colors.lightestShade};
                                            padding: 4px 8px;
                                            border-radius: ${euiTheme.border.radius.medium};

                                            &:hover {
                                                background: ${euiTheme.colors.lightShade};
                                            }
                                        `}
                                    >
                                        {result.title.replace(/<[^>]*>/g, '')}
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
