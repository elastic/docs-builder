import { askAiConfig } from './askAi.config'
import { useChatActions } from './chat.store'
import { useIsAskAiCooldownActive } from './useAskAiCooldown'
import { EuiButton, EuiText, useEuiTheme, EuiSpacer } from '@elastic/eui'
import { css } from '@emotion/react'
import { useMemo } from 'react'

export const AskAiSuggestions = () => {
    const { submitQuestion } = useChatActions()
    const disabled = useIsAskAiCooldownActive()
    const { euiTheme } = useEuiTheme()

    const selectedSuggestions = useMemo(() => {
        const shuffled = [...askAiConfig.suggestions].sort(
            () => Math.random() - 0.5
        )
        return shuffled.slice(0, 3)
    }, [])

    return (
        <div
            css={css`
                padding-inline: ${euiTheme.size.base};
            `}
        >
            <EuiSpacer size="m" />
            <EuiText size="xs">Example questions:</EuiText>
            <ul>
                {selectedSuggestions.map((suggestion) => (
                    <li
                        key={suggestion.question}
                        css={css`
                            margin-top: ${euiTheme.size.s};
                        `}
                    >
                        <EuiButton
                            color="text"
                            size="s"
                            onClick={() => {
                                if (!disabled) {
                                    submitQuestion(suggestion.question)
                                }
                            }}
                            disabled={disabled}
                            css={css`
                                height: auto;
                                white-space: normal;
                                text-align: left;
                                padding: ${euiTheme.size.s};
                            `}
                        >
                            {suggestion.question}
                        </EuiButton>
                    </li>
                ))}
            </ul>
        </div>
    )
}
