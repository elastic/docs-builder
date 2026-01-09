import { useChatActions } from './chat.store'
import { useIsAskAiCooldownActive } from './useAskAiCooldown'
import { EuiButton, EuiText, useEuiTheme, EuiSpacer } from '@elastic/eui'
import { css } from '@emotion/react'
import { useMemo } from 'react'

export interface AskAiSuggestion {
    question: string
}

// Comprehensive list of AI suggestion questions
const ALL_SUGGESTIONS: AskAiSuggestion[] = [
    { question: 'How do I set up a data stream in Elasticsearch?' },
    { question: 'What are the best practices for indexing performance?' },
    { question: 'How can I create a dashboard in Kibana?' },
    { question: 'What is the difference between a keyword and text field?' },
    { question: 'How do I configure machine learning jobs?' },
    { question: 'What are aggregations and how do I use them?' },
    { question: 'How do I set up Elasticsearch security and authentication?' },
    { question: 'What are the different types of Elasticsearch queries?' },
    { question: 'How do I monitor cluster health and performance?' },
    {
        question:
            'What is the Elastic Stack and how do the components work together?',
    },
    { question: 'How do I create and manage Elasticsearch indices?' },
    { question: 'What are the best practices for Elasticsearch mapping?' },
    { question: 'How do I set up log shipping with Beats?' },
    { question: 'What is APM and how do I use it for application monitoring?' },
    { question: 'How do I create custom visualizations in Kibana?' },
    { question: 'What are Elasticsearch snapshots and how do I use them?' },
    { question: 'How do I configure cross-cluster search?' },
    {
        question:
            'What are the different Elasticsearch node types and their roles?',
    },
]

export const AskAiSuggestions = () => {
    const { submitQuestion } = useChatActions()
    const disabled = useIsAskAiCooldownActive()
    const { euiTheme } = useEuiTheme()

    // Randomly select 3 questions from the comprehensive list
    const selectedSuggestions = useMemo(() => {
        // Shuffle the array and take first 3
        const shuffled = [...ALL_SUGGESTIONS].sort(() => Math.random() - 0.5)
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
                        >
                            {suggestion.question}
                        </EuiButton>
                    </li>
                ))}
            </ul>
        </div>
    )
}
