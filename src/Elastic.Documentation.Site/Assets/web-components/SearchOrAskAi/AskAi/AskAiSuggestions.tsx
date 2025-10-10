import { useModalActions } from '../modal.store'
import { useChatActions } from './chat.store'
import { EuiButton, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

const buttonStyles = css`
    border: none;
    & > span {
        justify-content: flex-start;
    }
`

export interface AskAiSuggestion {
    question: string
}

interface Props {
    suggestions: Set<AskAiSuggestion>
}

export const AskAiSuggestions = (props: Props) => {
    const { submitQuestion } = useChatActions()
    const { setModalMode } = useModalActions()
    const { euiTheme } = useEuiTheme()
    
    const dynamicButtonStyles = css`
        ${buttonStyles}
        svg {
            color: ${euiTheme.colors.textSubdued};
        }
    `
    
    return (
        <ul>
            {Array.from(props.suggestions).map((suggestion) => (
                <li key={suggestion.question}>
                    <EuiButton
                        iconType="newChat"
                        color="text"
                        fullWidth
                        size="s"
                        css={dynamicButtonStyles}
                        onClick={() => {
                            submitQuestion(suggestion.question)
                            setModalMode('askAi')
                        }}
                    >
                        {suggestion.question}
                    </EuiButton>
                </li>
            ))}
        </ul>
    )
}
