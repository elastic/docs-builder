import { AskAiSuggestions } from './AskAiSuggestions'
import { ChatInput } from './ChatInput'
import { ChatMessageList } from './ChatMessageList'
import { InfoBanner } from './InfoBanner'
import { LegalDisclaimer } from './LegalDisclaimer'
import AiIcon from './ai-icon.svg'
import { useAskAiModalActions } from './askAi.modal.store'
import {
    ChatMessage,
    useChatActions,
    useChatMessages,
    useChatScrollPosition,
    useIsChatEmpty,
    useIsStreaming,
} from './chat.store'
import { useIsAskAiCooldownActive } from './useAskAiCooldown'
import {
    EuiButtonIcon,
    EuiEmptyPrompt,
    EuiFlexGroup,
    EuiFlexItem,
    EuiHorizontalRule,
    EuiIcon,
    EuiSpacer,
    EuiToolTip,
    useEuiFontSize,
    useEuiOverflowScroll,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import {
    RefObject,
    useCallback,
    useEffect,
    useLayoutEffect,
    useRef,
    useState,
} from 'react'

export const Chat = () => {
    const { euiTheme } = useEuiTheme()
    const isEmpty = useIsChatEmpty()
    const inputRef = useRef<HTMLTextAreaElement>(null)
    const scrollRef = useRef<HTMLDivElement>(null)

    const handleScroll = useScrollPersistence(scrollRef)
    useFocusOnComplete(inputRef)

    // Focus input when Cmd+; is pressed
    const handleMetaSemicolon = useCallback(() => {
        inputRef.current?.focus()
    }, [])

    const [scrollAreaProps, setScrollAreaProps] = useState<{
        onAbortReady: (abort: () => void) => void
        isStreaming: boolean
    }>({
        onAbortReady: () => {},
        isStreaming: false,
    })

    return (
        <EuiFlexGroup
            direction="column"
            gutterSize="none"
            css={containerStyles}
        >
            <ChatHeader />

            {isEmpty ? (
                <EuiFlexItem grow={true} css={emptyStateContainerStyles}>
                    <EuiEmptyPrompt
                        icon={<EuiIcon type={AiIcon} size="xxl" />}
                        title={<h2>Hi! I'm the Elastic Docs AI Assistant</h2>}
                        body={
                            <p>
                                I'm here to help you find answers about Elastic,
                                powered entirely by our technical documentation.
                                How can I help?
                            </p>
                        }
                    />
                </EuiFlexItem>
            ) : (
                <ChatScrollArea
                    scrollRef={scrollRef}
                    onScroll={handleScroll}
                    onAbortReady={scrollAreaProps.onAbortReady}
                    isStreaming={scrollAreaProps.isStreaming}
                />
            )}

            {isEmpty && (
                <>
                    <AskAiSuggestions />
                    <EuiSpacer size="m" />
                    <div
                        css={css`
                            padding-inline: ${euiTheme.size.base};
                        `}
                    >
                        <LegalDisclaimer />
                    </div>
                    <EuiSpacer size="m" />
                </>
            )}

            <ChatInputArea
                inputRef={inputRef}
                onMetaSemicolon={handleMetaSemicolon}
                onStateChange={setScrollAreaProps}
            />
            <InfoBanner />
        </EuiFlexGroup>
    )
}

const ChatHeader = () => {
    const { closeModal } = useAskAiModalActions()
    const { clearChat } = useChatActions()
    const messages = useChatMessages()
    const { euiTheme } = useEuiTheme()
    const smallFontsize = useEuiFontSize('s').fontSize
    return (
        <EuiFlexItem
            grow={false}
            css={css`
                flex-shrink: 0;
            `}
        >
            <div
                css={css`
                    padding-block: ${euiTheme.size.m};
                    padding-inline: ${euiTheme.size.base};
                    display: grid;
                    height: 56px;
                    grid-template-columns: 1fr auto auto;
                    align-items: center;
                `}
            >
                <div
                    css={css`
                        display: flex;
                        align-items: center;
                        gap: ${euiTheme.size.m};
                    `}
                >
                    <EuiIcon type={AiIcon} />
                    <span
                        css={css`
                            font-size: ${smallFontsize};
                            font-weight: ${euiTheme.font.weight.medium};
                        `}
                    >
                        Elastic Docs AI Assistant
                    </span>
                </div>
                <div
                    css={css`
                        display: flex;
                        gap: ${euiTheme.size.s};
                    `}
                >
                    <EuiToolTip content="Clear conversation">
                        <EuiButtonIcon
                            aria-label="Clear conversation"
                            iconType="trash"
                            color="text"
                            onClick={() => clearChat()}
                            css={css`
                                visibility: ${messages.length > 0
                                    ? 'visible'
                                    : 'hidden'};
                            `}
                        />
                    </EuiToolTip>
                    <EuiButtonIcon
                        aria-label="Close Ask AI modal"
                        iconType="cross"
                        color="text"
                        onClick={() => closeModal()}
                    />
                </div>
            </div>
            <EuiHorizontalRule margin="none" />
        </EuiFlexItem>
    )
}

interface ChatScrollAreaProps {
    scrollRef: RefObject<HTMLDivElement>
    onScroll: () => void
    onAbortReady: (abort: () => void) => void
    isStreaming: boolean
}

const ChatScrollArea = ({
    scrollRef,
    onScroll,
    onAbortReady,
    isStreaming,
}: ChatScrollAreaProps) => {
    const messages = useChatMessages()
    const { euiTheme } = useEuiTheme()
    const spacerHeight = useSpacerHeight(scrollRef, isStreaming, messages)
    // Initialize with current count to prevent scroll on remount (only scroll on NEW messages)
    const initialUserMessageCount = messages.filter(
        (m) => m.type === 'user'
    ).length
    const lastUserMessageCountRef = useRef(initialUserMessageCount)

    // Scroll to user message when a new one is added
    useLayoutEffect(() => {
        const userMessageCount = messages.filter(
            (m) => m.type === 'user'
        ).length
        if (userMessageCount > lastUserMessageCountRef.current) {
            // New user message added - use double RAF to ensure spacer DOM update
            // First RAF: React processes state updates and schedules re-render
            // Second RAF: DOM is updated with new spacer, safe to scroll
            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    if (scrollRef.current) {
                        const scrollMargin = parseInt(euiTheme.size.l, 10)
                        scrollUserMessageToTop(scrollRef.current, scrollMargin)
                    }
                })
            })
        }
        lastUserMessageCountRef.current = userMessageCount
    }, [messages, scrollRef, euiTheme.size.l])

    const scrollableStyles = css`
        height: 100%;
        overflow-y: auto;
        scrollbar-gutter: stable;
        padding: ${euiTheme.size.l};
        ${useEuiOverflowScroll('y', false)}
    `

    return (
        <EuiFlexItem grow={true} css={scrollContainerStyles}>
            <div ref={scrollRef} css={scrollableStyles} onScroll={onScroll}>
                <div css={messagesStyles}>
                    <ChatMessageList
                        messages={messages}
                        onAbortReady={onAbortReady}
                    />
                    {spacerHeight > 0 && (
                        <div style={{ minHeight: spacerHeight }} />
                    )}
                </div>
            </div>
        </EuiFlexItem>
    )
}

interface ChatInputAreaProps {
    inputRef: RefObject<HTMLTextAreaElement>
    onMetaSemicolon?: () => void
    onStateChange?: (state: {
        onAbortReady: (abort: () => void) => void
        isStreaming: boolean
    }) => void
}

const ChatInputArea = ({
    inputRef,
    onMetaSemicolon,
    onStateChange,
}: ChatInputAreaProps) => {
    const { euiTheme } = useEuiTheme()
    const {
        inputValue,
        setInputValue,
        handleSubmit,
        handleAbort,
        handleAbortReady,
        isStreaming,
        isCooldownActive,
    } = useChatSubmit()

    useEffect(() => {
        onStateChange?.({
            onAbortReady: handleAbortReady,
            isStreaming,
        })
    }, [handleAbortReady, isStreaming, onStateChange])

    return (
        <EuiFlexItem grow={false}>
            <div
                css={css`
                    position: relative;
                    padding-inline: ${euiTheme.size.base};
                `}
            >
                <ChatInput
                    value={inputValue}
                    onChange={setInputValue}
                    onSubmit={handleSubmit}
                    onAbort={handleAbort}
                    disabled={isCooldownActive}
                    inputRef={inputRef}
                    isStreaming={isStreaming}
                    onMetaSemicolon={onMetaSemicolon}
                />
            </div>
        </EuiFlexItem>
    )
}

function useChatSubmit() {
    const { submitQuestion, clearNon429Errors, cancelStreaming } =
        useChatActions()
    const isCooldownActive = useIsAskAiCooldownActive()
    const isStreaming = useIsStreaming()
    // Note: Scrolling is now handled in ChatScrollArea via useLayoutEffect

    const [inputValue, setInputValue] = useState('')
    const abortRef = useRef<(() => void) | null>(null)

    useEffect(() => {
        if (!isStreaming) {
            abortRef.current = null
        }
    }, [isStreaming])

    const handleSubmit = useCallback(
        (question: string) => {
            const trimmed = question.trim()
            if (!trimmed || isCooldownActive) return

            clearNon429Errors()
            submitQuestion(trimmed)
            setInputValue('')
        },
        [submitQuestion, isCooldownActive, clearNon429Errors]
    )

    const handleAbort = useCallback(() => {
        if (abortRef.current) {
            abortRef.current()
            abortRef.current = null
            cancelStreaming()
        }
    }, [cancelStreaming])

    const handleAbortReady = useCallback((abort: () => void) => {
        abortRef.current = abort
    }, [])

    return {
        inputValue,
        setInputValue,
        handleSubmit,
        handleAbort,
        handleAbortReady,
        isStreaming,
        isCooldownActive,
    }
}

/**
 * Manages scroll position persistence across modal open/close.
 */
function useScrollPersistence(scrollRef: RefObject<HTMLDivElement | null>) {
    const savedPosition = useChatScrollPosition()
    const { setScrollPosition } = useChatActions()

    useEffect(() => {
        if (scrollRef.current && savedPosition > 0) {
            requestAnimationFrame(() => {
                if (scrollRef.current) {
                    scrollRef.current.scrollTop = savedPosition
                }
            })
        }
    }, [])

    const handleScroll = useCallback(() => {
        if (scrollRef.current) {
            setScrollPosition(scrollRef.current.scrollTop)
        }
    }, [setScrollPosition, scrollRef])

    return handleScroll
}

/**
 * Auto-focuses input when AI response completes streaming.
 */
function useFocusOnComplete(inputRef: RefObject<HTMLTextAreaElement | null>) {
    const messages = useChatMessages()
    const lastStatusRef = useRef<string | null>(null)

    useEffect(() => {
        const last = messages[messages.length - 1]
        if (last?.type === 'ai') {
            const currentStatus = last.status
            const previousStatus = lastStatusRef.current

            if (
                previousStatus === 'streaming' &&
                currentStatus === 'complete'
            ) {
                setTimeout(() => inputRef.current?.focus(), 100)
            }

            lastStatusRef.current = currentStatus || null
        }
    }, [messages, inputRef])
}

/**
 * Calculates spacer height to keep user message at top during/after streaming.
 * During streaming: spacer fills remaining space after user message.
 * After streaming: keeps spacer if AI response is shorter than available space.
 */
function useSpacerHeight(
    scrollRef: RefObject<HTMLDivElement | null>,
    isStreaming: boolean,
    messages: ChatMessage[]
): number {
    const { euiTheme } = useEuiTheme()
    const scrollMargin = parseInt(euiTheme.size.l, 10)
    const [spacerHeight, setSpacerHeight] = useState(0)
    const [containerSize, setContainerSize] = useState({ width: 0, height: 0 })

    // Track container size changes with ResizeObserver
    useLayoutEffect(() => {
        if (!scrollRef.current) return

        const container = scrollRef.current
        const resizeObserver = new ResizeObserver((entries) => {
            for (const entry of entries) {
                const { width, height } = entry.contentRect
                setContainerSize((prev) => {
                    if (prev.width !== width || prev.height !== height) {
                        return { width, height }
                    }
                    return prev
                })
            }
        })

        resizeObserver.observe(container)
        return () => resizeObserver.disconnect()
    }, [scrollRef])

    // Calculate spacer height - use useLayoutEffect for synchronous updates
    useLayoutEffect(() => {
        if (!scrollRef.current) return

        const container = scrollRef.current
        const containerHeight = container.clientHeight
        const lastUserMessage = getLastMessage(container, 'user')

        if (!lastUserMessage) {
            setSpacerHeight(0)
            return
        }

        const userMessageHeight = lastUserMessage.offsetHeight

        if (isStreaming) {
            // During streaming: spacer = remaining space after user message
            const calculatedHeight =
                containerHeight - userMessageHeight - scrollMargin * 2 - 1
            setSpacerHeight(Math.max(0, calculatedHeight))
        } else {
            // After streaming: keep spacer if AI response is shorter than available space
            const lastAiMessage = getLastMessage(container, 'ai')
            const aiMessageHeight = lastAiMessage?.offsetHeight || 0

            const contentHeight = userMessageHeight + aiMessageHeight
            const remainingSpace =
                containerHeight - contentHeight - scrollMargin * 2 - 1
            setSpacerHeight(Math.max(0, remainingSpace))
        }
    }, [isStreaming, scrollRef, messages, scrollMargin, containerSize])

    return spacerHeight
}

// ============================================================================
// Constants & Styles (implementation details)
// ============================================================================

const CONTENT_AREA_HEIGHT = 400

// ============================================================================
// DOM Helpers
// ============================================================================

function getLastMessage(
    container: HTMLElement,
    type: 'user' | 'ai'
): HTMLElement | null {
    const messages = container.querySelectorAll(`[data-message-type="${type}"]`)
    return (messages[messages.length - 1] as HTMLElement) || null
}

function scrollUserMessageToTop(container: HTMLElement, margin: number): void {
    const lastUserMessage = getLastMessage(container, 'user')
    if (!lastUserMessage) return

    const containerRect = container.getBoundingClientRect()
    const messageRect = lastUserMessage.getBoundingClientRect()
    const scrollOffset =
        messageRect.top - containerRect.top + container.scrollTop - margin

    container.scrollTo({
        top: Math.max(0, scrollOffset),
        behavior: 'smooth',
    })
}

const containerStyles = css`
    height: 100vh;
    overflow: hidden;
`

const scrollContainerStyles = css`
    position: relative;
    overflow: hidden;
    min-height: ${CONTENT_AREA_HEIGHT}px;
`

const emptyStateContainerStyles = css`
    display: flex;
    align-items: center;
    justify-content: center;
`

const messagesStyles = css`
    max-width: 800px;
    margin: 0 auto;

    & > [data-message-type='user']:first-child {
        margin-top: 0;
    }
`
