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

interface AIAnswerPanelProps {
    query: string
    results: SearchResultItem[]
    visible: boolean
}

export const AIAnswerPanel = ({
    query,
    results,
    visible,
}: AIAnswerPanelProps) => {
    const { euiTheme } = useEuiTheme()
    const [content, setContent] = useState('')
    const [streaming, setStreaming] = useState(false)
    const [collapsed, setCollapsed] = useState(false)
    const [showSources, setShowSources] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const abortControllerRef = useRef<AbortController | null>(null)

    const streamAnswer = useCallback(async () => {
        if (!query || results.length === 0) return

        setContent('')
        setStreaming(true)
        setShowSources(false)
        setError(null)

        abortControllerRef.current = new AbortController()

        try {
            // Build context from top results
            const contextDocs = results.slice(0, 5).map((r) => ({
                title: r.title,
                url: r.url,
                content: r.aiRagOptimizedSummary || r.description,
            }))

            // Build context string from top results
            const contextText = contextDocs
                .map((d) => `Title: ${d.title}\nURL: ${d.url}\nContent: ${d.content}`)
                .join('\n\n---\n\n')

            const messageWithContext = `Based on the following documentation context, answer this question: "${query}"\n\nContext:\n${contextText}`

            const response = await fetch('/docs/_api/v1/ask-ai/stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    message: messageWithContext,
                    conversationId: null,
                }),
                signal: abortControllerRef.current.signal,
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

                // Process SSE events
                const lines = buffer.split('\n')
                buffer = lines.pop() || ''

                for (const line of lines) {
                    if (line.startsWith('data:')) {
                        const data = line.slice(5).trim()
                        if (data === '[DONE]') {
                            setStreaming(false)
                            setShowSources(true)
                            return
                        }

                        try {
                            const parsed = JSON.parse(data)
                            if (parsed.content) {
                                setContent((prev) => prev + parsed.content)
                            } else if (parsed.error) {
                                setError(parsed.error)
                                setStreaming(false)
                                return
                            }
                        } catch {
                            // Non-JSON data, might be raw content
                            if (data && data !== '[DONE]') {
                                setContent((prev) => prev + data)
                            }
                        }
                    }
                }
            }

            setStreaming(false)
            setShowSources(true)
        } catch (err) {
            if (err instanceof Error && err.name === 'AbortError') {
                // User cancelled
                return
            }
            setError(err instanceof Error ? err.message : 'Failed to get AI answer')
            setStreaming(false)
        }
    }, [query, results])

    useEffect(() => {
        if (visible && query && results.length > 0) {
            streamAnswer()
        }

        return () => {
            abortControllerRef.current?.abort()
        }
    }, [visible, query, results, streamAnswer])

    const handleStop = () => {
        abortControllerRef.current?.abort()
        setStreaming(false)
    }

    const handleRegenerate = () => {
        streamAnswer()
    }

    if (!visible) return null

    if (collapsed) {
        return (
            <EuiPanel
                color="accent"
                paddingSize="m"
                css={css`
                    margin-bottom: ${euiTheme.size.l};
                    cursor: pointer;
                `}
                onClick={() => setCollapsed(false)}
            >
                <EuiFlexGroup alignItems="center" justifyContent="spaceBetween">
                    <EuiFlexItem grow={false}>
                        <EuiFlexGroup alignItems="center" gutterSize="s">
                            <EuiFlexItem grow={false}>
                                <EuiIcon type="sparkles" color="accent" />
                            </EuiFlexItem>
                            <EuiFlexItem>
                                <EuiText size="s">
                                    <strong>AI Answer available</strong>
                                </EuiText>
                            </EuiFlexItem>
                        </EuiFlexGroup>
                    </EuiFlexItem>
                    <EuiFlexItem grow={false}>
                        <EuiIcon type="arrowDown" color="subdued" />
                    </EuiFlexItem>
                </EuiFlexGroup>
            </EuiPanel>
        )
    }

    return (
        <EuiPanel
            color="accent"
            css={css`
                margin-bottom: ${euiTheme.size.l};
                overflow: hidden;
            `}
        >
            <EuiFlexGroup
                alignItems="center"
                justifyContent="spaceBetween"
                css={css`
                    padding-bottom: ${euiTheme.size.s};
                    border-bottom: 1px solid ${euiTheme.colors.lightShade};
                    margin-bottom: ${euiTheme.size.m};
                `}
            >
                <EuiFlexItem grow={false}>
                    <EuiFlexGroup alignItems="center" gutterSize="s">
                        <EuiFlexItem grow={false}>
                            <EuiIcon type="sparkles" color="accent" />
                        </EuiFlexItem>
                        <EuiFlexItem>
                            <EuiText size="s">
                                <strong>AI Answer</strong>
                            </EuiText>
                        </EuiFlexItem>
                        {streaming && (
                            <EuiFlexItem grow={false}>
                                <span
                                    css={css`
                                        font-size: ${euiTheme.size.m};
                                        color: ${euiTheme.colors.accent};
                                        background: ${euiTheme.colors
                                            .lightestShade};
                                        padding: ${euiTheme.size.xs}
                                            ${euiTheme.size.s};
                                        border-radius: ${euiTheme.border.radius
                                            .medium};
                                        animation: pulse 1.5s infinite;

                                        @keyframes pulse {
                                            0%,
                                            100% {
                                                opacity: 1;
                                            }
                                            50% {
                                                opacity: 0.5;
                                            }
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
                                    />
                                </EuiToolTip>
                            </EuiFlexItem>
                        )}
                        <EuiFlexItem grow={false}>
                            <EuiToolTip content="Collapse">
                                <EuiButtonIcon
                                    iconType="arrowUp"
                                    aria-label="Collapse"
                                    onClick={() => setCollapsed(true)}
                                />
                            </EuiToolTip>
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </EuiFlexItem>
            </EuiFlexGroup>

            {error ? (
                <EuiText color="danger" size="s">
                    {error}
                </EuiText>
            ) : (
                <EuiText
                    size="s"
                    css={css`
                        white-space: pre-wrap;
                        line-height: 1.6;

                        code {
                            background: ${euiTheme.colors.lightestShade};
                            padding: ${euiTheme.size.xs};
                            border-radius: ${euiTheme.border.radius.small};
                        }

                        pre {
                            background: ${euiTheme.colors.lightestShade};
                            padding: ${euiTheme.size.m};
                            border-radius: ${euiTheme.border.radius.medium};
                            overflow-x: auto;
                        }
                    `}
                >
                    {content}
                    {streaming && (
                        <span
                            css={css`
                                display: inline-block;
                                width: 8px;
                                height: 16px;
                                background: ${euiTheme.colors.accent};
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
                </EuiText>
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
                                        font-size: ${euiTheme.size.m};
                                        background: ${euiTheme.colors
                                            .lightestShade};
                                        padding: ${euiTheme.size.xs}
                                            ${euiTheme.size.s};
                                        border-radius: ${euiTheme.border.radius
                                            .medium};

                                        &:hover {
                                            background: ${euiTheme.colors
                                                .lightShade};
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
        </EuiPanel>
    )
}
