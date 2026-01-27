import { ElasticAiAssistantButtonIcon } from './ElasticAiAssistantButton'
import { euiShadow, useEuiScrollBar, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import { useCallback, useEffect, useRef } from 'react'

const CONFIG = {
    MAX_LINES: 10,
    MIN_HEIGHT: 48,
    BORDER_RADIUS: 24,
    BUTTON_SIZE: 32,
} as const

interface ChatInputProps {
    value: string
    onChange: (value: string) => void
    onSubmit: (value: string) => void
    onAbort?: () => void
    disabled?: boolean
    placeholder?: string
    inputRef?: React.MutableRefObject<HTMLTextAreaElement | null>
    isStreaming?: boolean
    onMetaSemicolon?: () => void
}

export const ChatInput = ({
    value,
    onChange,
    onSubmit,
    onAbort,
    disabled = false,
    placeholder = 'Ask the Elastic Docs AI Assistant',
    inputRef,
    isStreaming = false,
    onMetaSemicolon,
}: ChatInputProps) => {
    const { euiTheme } = useEuiTheme()
    const scollbarStyling = useEuiScrollBar()
    const shadowStyling = euiShadow(useEuiTheme(), 's')
    const internalRef = useRef<HTMLTextAreaElement | null>(null)

    const textareaRefCallback = useCallback(
        (element: HTMLTextAreaElement | null) => {
            internalRef.current = element
            if (inputRef) {
                inputRef.current = element
            }
        },
        [inputRef]
    )

    // Adjust height based on content
    useEffect(() => {
        const textarea = internalRef.current
        if (!textarea) return

        // Calculate max height from computed styles
        const computedStyle = window.getComputedStyle(textarea)
        const lineHeight = parseFloat(computedStyle.lineHeight) || 20
        const verticalPadding =
            parseFloat(computedStyle.paddingTop) +
            parseFloat(computedStyle.paddingBottom)
        const maxHeight = lineHeight * CONFIG.MAX_LINES + verticalPadding

        if (!value.trim()) {
            textarea.style.height = `${CONFIG.MIN_HEIGHT}px`
            return
        }

        textarea.style.height = 'auto'
        textarea.style.height = `${Math.min(
            Math.max(textarea.scrollHeight, CONFIG.MIN_HEIGHT),
            maxHeight
        )}px`
    }, [value])

    // Listen for Cmd+; to focus input
    useEffect(() => {
        if (!onMetaSemicolon) return
        const handleGlobalKeyDown = (e: KeyboardEvent) => {
            if ((e.metaKey || e.ctrlKey) && e.code === 'Semicolon') {
                e.preventDefault()
                onMetaSemicolon()
            }
        }
        window.addEventListener('keydown', handleGlobalKeyDown)
        return () => window.removeEventListener('keydown', handleGlobalKeyDown)
    }, [onMetaSemicolon])

    const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault()
            if (!isStreaming) {
                onSubmit(value)
            }
        }
    }

    const hasContent = value.trim().length > 0

    return (
        <div
            css={css`
                ${shadowStyling};
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.base};
                border-radius: ${CONFIG.BORDER_RADIUS}px;
                border: ${euiTheme.border.thin};
                border-color: ${euiTheme.border.color};
                background-color: ${disabled
                    ? euiTheme.colors.lightestShade
                    : euiTheme.colors.emptyShade};
                padding-right: 8px;
                min-height: ${CONFIG.MIN_HEIGHT}px;
                cursor: ${disabled ? 'not-allowed' : 'auto'};
                outline: 2px solid transparent;
                outline-offset: -1px;

                &:focus-within {
                    border-color: ${euiTheme.colors.primary};
                    outline-color: ${euiTheme.colors.primary};
                }
                &:hover {
                    border-color: ${euiTheme.colors.borderBasePlain};
                }
            `}
        >
            <textarea
                ref={textareaRefCallback}
                id="ask-ai-chat-input"
                name="chat-input"
                value={value}
                onChange={(e) => onChange(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={disabled}
                placeholder={placeholder}
                autoFocus
                spellCheck={false}
                rows={1}
                css={css`
                    ${scollbarStyling};
                    flex: 1;
                    resize: none;
                    border: none;
                    outline: none !important;
                    background: transparent;
                    min-height: 48px;
                    padding: 12px 20px;
                    overflow-y: auto;
                    box-sizing: border-box;
                    color: ${euiTheme.colors.textParagraph};
                    font-family: ${euiTheme.font.family};

                    &:focus,
                    &:focus-visible,
                    &:active {
                        outline: none !important;
                        border: none !important;
                        box-shadow: none !important;
                    }

                    &:disabled {
                        color: ${euiTheme.colors.textDisabled};
                        cursor: not-allowed;
                    }

                    &::placeholder {
                        color: ${euiTheme.colors.textDisabled};
                    }
                `}
            />
            <ElasticAiAssistantButtonIcon
                fill={hasContent || isStreaming}
                iconType={isStreaming ? 'stop' : 'kqlFunction'}
                aria-label={isStreaming ? 'Stop streaming' : 'Send message'}
                css={css`
                    inline-size: ${CONFIG.BUTTON_SIZE}px;
                    block-size: ${CONFIG.BUTTON_SIZE}px;
                    border-radius: 9999px;
                `}
                onClick={() => (isStreaming ? onAbort?.() : onSubmit(value))}
                disabled={disabled || (!hasContent && !isStreaming)}
            />
        </div>
    )
}
