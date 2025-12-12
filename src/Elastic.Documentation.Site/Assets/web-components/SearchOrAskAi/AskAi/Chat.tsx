import { InfoBanner } from '../InfoBanner'
import { KeyboardShortcutsFooter } from '../KeyboardShortcutsFooter'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import AiIcon from '../ai-icon.svg'
import { useModalActions } from '../modal.store'
import { AskAiSuggestions } from './AskAiSuggestions'
import { ChatInput } from './ChatInput'
import { ChatMessageList } from './ChatMessageList'
import {
    ChatMessage,
    useChatActions,
    useChatMessages,
    useChatScrollPosition,
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
    EuiText,
    EuiToolTip,
    useEuiFontSize,
    useEuiOverflowScroll,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { RefObject, useCallback, useEffect, useRef, useState } from 'react'

export const Chat = () => {
    const messages = useChatMessages()
    const inputRef = useRef<HTMLTextAreaElement>(null)
    const scrollRef = useRef<HTMLDivElement>(null)

    const handleScroll = useScrollPersistence(scrollRef)
    useFocusOnComplete(inputRef)

    // Focus input when Cmd+; is pressed
    const handleMetaSemicolon = useCallback(() => {
        inputRef.current?.focus()
    }, [])

    const {
        inputValue,
        setInputValue,
        handleSubmit,
        handleAbort,
        handleAbortReady,
        isStreaming,
        isCooldownActive,
    } = useChatSubmit(scrollRef)

    return (
        <EuiFlexGroup
            direction="column"
            gutterSize="none"
            css={containerStyles}
        >
            <ChatHeader />

            <ChatScrollArea
                scrollRef={scrollRef}
                onScroll={handleScroll}
                messages={messages}
                isCooldownActive={isCooldownActive}
                onAbortReady={handleAbortReady}
            />

            <ChatInputArea
                inputRef={inputRef}
                value={inputValue}
                onChange={setInputValue}
                onSubmit={handleSubmit}
                onAbort={handleAbort}
                disabled={isCooldownActive}
                isStreaming={isStreaming}
                onMetaSemicolon={handleMetaSemicolon}
            />

            <InfoBanner />
            <KeyboardShortcutsFooter shortcuts={KEYBOARD_SHORTCUTS} />
        </EuiFlexGroup>
    )
}

const ChatHeader = () => {
    const { closeModal } = useModalActions()
    const { clearChat } = useChatActions()
    const messages = useChatMessages()
    const { euiTheme } = useEuiTheme()
    const smallFontsize = useEuiFontSize('s').fontSize
    return (
        <>
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
                    {messages.length > 0 && (
                        <EuiToolTip content="Clear conversation">
                            <EuiButtonIcon
                                aria-label="Clear conversation"
                                iconType="trash"
                                color="text"
                                onClick={() => clearChat()}
                            />
                        </EuiToolTip>
                    )}
                    <EuiButtonIcon
                        aria-label="Close Ask AI modal"
                        iconType="cross"
                        color="text"
                        onClick={() => closeModal()}
                    />
                </div>
            </div>
            <EuiHorizontalRule margin="none" />
        </>
    )
}

interface ChatScrollAreaProps {
    scrollRef: RefObject<HTMLDivElement>
    onScroll: () => void
    messages: ChatMessage[]
    isCooldownActive: boolean
    onAbortReady: (abort: () => void) => void
}

const ChatScrollArea = ({
    scrollRef,
    onScroll,
    messages,
    isCooldownActive,
    onAbortReady,
}: ChatScrollAreaProps) => {
    const { euiTheme } = useEuiTheme()

    const scrollableStyles = css`
        height: 100%;
        overflow-y: auto;
        scrollbar-gutter: stable;
        padding: ${euiTheme.size.l};
        ${useEuiOverflowScroll('y', true)}
    `

    return (
        <EuiFlexItem grow={true} css={scrollContainerStyles}>
            <div ref={scrollRef} css={scrollableStyles} onScroll={onScroll}>
                {messages.length === 0 ? (
                    <ChatEmptyState disabled={isCooldownActive} />
                ) : (
                    <div css={messagesStyles}>
                        <ChatMessageList
                            messages={messages}
                            onAbortReady={onAbortReady}
                        />
                    </div>
                )}
            </div>
        </EuiFlexItem>
    )
}

const ChatEmptyState = ({ disabled }: { disabled: boolean }) => (
    <>
        <EuiEmptyPrompt
            icon={<EuiIcon type={AiIcon} size="xxl" />}
            title={<h2>Hi! I'm the Elastic Docs AI Assistant</h2>}
            body={
                <p>
                    I'm here to help you find answers about Elastic, powered
                    entirely by our technical documentation. How can I help?
                </p>
            }
        />
        <EuiSpacer size="s" />
        <div>
            <EuiText size="xs" color="subdued">
                Example questions
            </EuiText>
            <EuiSpacer size="s" />
            <AskAiSuggestions disabled={disabled} />
        </div>
        <div css={messagesStyles}>
            <SearchOrAskAiErrorCallout error={null} domain="askAi" />
        </div>
    </>
)

interface ChatInputAreaProps {
    inputRef: RefObject<HTMLTextAreaElement>
    value: string
    onChange: (value: string) => void
    onSubmit: (question: string) => void
    onAbort: () => void
    disabled: boolean
    isStreaming: boolean
    onMetaSemicolon?: () => void
}

const ChatInputArea = ({
    inputRef,
    value,
    onChange,
    onSubmit,
    onAbort,
    disabled,
    isStreaming,
    onMetaSemicolon,
}: ChatInputAreaProps) => {
    const { euiTheme } = useEuiTheme()

    return (
        <EuiFlexItem grow={false}>
            <EuiSpacer size="s" />
            <div
                css={css`
                    position: relative;
                    padding-inline: ${euiTheme.size.base};
                `}
            >
                <ChatInput
                    value={value}
                    onChange={onChange}
                    onSubmit={onSubmit}
                    onAbort={onAbort}
                    disabled={disabled}
                    inputRef={inputRef}
                    isStreaming={isStreaming}
                    onMetaSemicolon={onMetaSemicolon}
                />
            </div>
        </EuiFlexItem>
    )
}

function useChatSubmit(scrollRef: RefObject<HTMLDivElement | null>) {
    const { submitQuestion, clearNon429Errors, cancelStreaming } =
        useChatActions()
    const isCooldownActive = useIsAskAiCooldownActive()
    const isStreaming = useIsStreaming()

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

            setTimeout(() => {
                if (scrollRef.current) {
                    scrollRef.current.scrollTop = scrollRef.current.scrollHeight
                }
            }, 100)
        },
        [submitQuestion, isCooldownActive, clearNon429Errors, scrollRef]
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

// ============================================================================
// Constants & Styles (implementation details)
// ============================================================================

const KEYBOARD_SHORTCUTS = [
    { keys: ['⌘K'], label: 'Search' },
    { keys: ['Esc'], label: 'Close' },
]

const containerStyles = css`
    height: 100%;
    max-height: 70vh;
    overflow: hidden;
`

const scrollContainerStyles = css`
    position: relative;
    overflow: hidden;
`

const messagesStyles = css`
    max-width: 800px;
    margin: 0 auto;
`
