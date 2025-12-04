import { useIsAskAiCooldownActive } from '../AskAi/useAskAiCooldown'
import { ElasticAiAssistantButton } from '../ElasticAiAssitant'
import { useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import { forwardRef } from 'react'

interface TellMeMoreButtonProps {
    term: string
    onAsk: () => void
    onArrowUp?: () => void
}

export const TellMeMoreButton = forwardRef<
    HTMLButtonElement,
    TellMeMoreButtonProps
>(({ term, onAsk, onArrowUp }, ref) => {
    const isAskAiCooldownActive = useIsAskAiCooldownActive()
    const { euiTheme } = useEuiTheme()

    return (
        <ElasticAiAssistantButton
            buttonRef={ref}
            fullWidth
            css={css`
                height: 40px;
                & > span {
                    justify-content: flex-start;
                }
            `}
            onClick={onAsk}
            disabled={isAskAiCooldownActive}
            onKeyDown={(e) => {
                if (e.key === 'ArrowUp') {
                    e.preventDefault()
                    onArrowUp?.()
                }
            }}
        >
            <span>Tell me more about</span>{' '}
            <span
                css={css`
                    font-weight: ${euiTheme.font.weight.bold};
                `}
            >
                {term}
            </span>
        </ElasticAiAssistantButton>
    )
})

TellMeMoreButton.displayName = 'TellMeMoreButton'
