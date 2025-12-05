import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
import { EuiButton, EuiIcon, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import { forwardRef } from 'react'

interface TellMeMoreButtonProps {
    term: string
    onAsk: () => void
    onArrowUp?: () => void
    isInputFocused: boolean
}

export const TellMeMoreButton = forwardRef<
    HTMLButtonElement,
    TellMeMoreButtonProps
>(({ term, onAsk, onArrowUp, isInputFocused }, ref) => {
    const isAskAiCooldownActive = useIsAskAiCooldownActive()
    const { euiTheme } = useEuiTheme()

    const askAiButtonStyles = css`
        font-weight: ${euiTheme.font.weight.bold};
        color: ${euiTheme.colors.link};
    `

    return (
        <div
            css={css`
                @keyframes gradientMove {
                    from {
                        background-position: 0% 0%;
                    }
                    to {
                        background-position: 100% 0%;
                    }
                }
                height: 42px;
                background: linear-gradient(
                    90deg,
                    #f04e98 0%,
                    #02bcb7 25%,
                    #f04e98 50%,
                    #02bcb7 75%,
                    #f04e98 100%
                );
                background-size: 200% 100%;
                background-position: 0% 0%;
                display: flex;
                align-items: center;
                justify-content: center;
                border-radius: 4px;
                animation: gradientMove 3s ease infinite;
            `}
        >
            <EuiButton
                buttonRef={ref}
                css={css`
                    & > span {
                        display: flex;
                        align-items: center;
                        justify-content: flex-start;
                        width: 100%;
                        gap: ${euiTheme.size.s};
                    }
                    margin-inline: 1px;
                    border: none;
                    position: relative;
                    :focus .return-key-icon {
                        visibility: visible;
                    }
                `}
                color="text"
                fullWidth
                onClick={onAsk}
                disabled={isAskAiCooldownActive}
                onKeyDown={(e) => {
                    if (e.key === 'ArrowUp') {
                        e.preventDefault()
                        onArrowUp?.()
                    }
                }}
            >
                <span
                    css={css`
                        flex: 1;
                        min-width: 0;
                        overflow: hidden;
                        text-overflow: ellipsis;
                        white-space: nowrap;
                        text-align: left;
                    `}
                >
                    Tell me more about&nbsp;
                    <span css={askAiButtonStyles}>{term}</span>
                </span>
                <EuiIcon
                    className="return-key-icon"
                    css={css`
                        visibility: ${isInputFocused ? 'visible' : 'hidden'};
                        flex-shrink: 0;
                    `}
                    type="returnKey"
                    color="subdued"
                    size="m"
                />
            </EuiButton>
        </div>
    )
})

TellMeMoreButton.displayName = 'TellMeMoreButton'
