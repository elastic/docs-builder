import { useSearchActions, useSearchTerm } from '../search.store'
import {
    EuiButton,
    EuiIcon,
    EuiSpacer,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

export interface AskAiSuggestion {
    question: string
}

interface Props {
    suggestions: AskAiSuggestion[]
}

export const AskAiSuggestions = (props: Props) => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm, submitAskAiTerm } = useSearchActions()
    const { euiTheme } = useEuiTheme()
    const buttonCss = css`
        border: none;
        & > span {
            justify-content: flex-start;
        }
        svg {
            color: ${euiTheme.colors.textSubdued};
        }
    `
    return (
        <>
            <div
                css={css`
                    display: flex;
                    gap: ${euiTheme.size.s};
                    align-items: center;
                `}
            >
                <EuiIcon type="sparkles" color="subdued" size="s" />
                <EuiText size="xs">Ask Elastic Docs AI Assistant</EuiText>
            </div>
            <EuiSpacer size="s" />
            {searchTerm && (
                <EuiButton
                    iconType="newChat"
                    color="text"
                    fullWidth
                    size="s"
                    css={buttonCss}
                    onClick={() => {
                        submitAskAiTerm(searchTerm)
                    }}
                >
                    {searchTerm}
                </EuiButton>
            )}
            {props.suggestions.map((suggestion, index) => (
                <EuiButton
                    key={index}
                    iconType="newChat"
                    color="text"
                    fullWidth
                    size="s"
                    css={buttonCss}
                    onClick={() => {
                        setSearchTerm(suggestion.question)
                        submitAskAiTerm(suggestion.question)
                    }}
                >
                    {suggestion.question}
                </EuiButton>
            ))}
        </>
    )
}
