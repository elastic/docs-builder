import { AskAiAnswer } from './AskAi/AskAiAnswer'
import { AskAiSuggestions } from './AskAi/AskAiSuggestions'
import { SearchResults } from './Search/SearchResults'
import { useAskAiTerm, useSearchActions, useSearchTerm } from './search.store'
import {
    EuiFieldSearch,
    EuiSpacer,
    EuiBetaBadge,
    EuiText,
    EuiHorizontalRule,
    useEuiOverflowScroll,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

export const SearchOrAskAiModal = () => {
    const searchTerm = useSearchTerm()
    const askAiTerm = useAskAiTerm()
    const { setSearchTerm, submitAskAiTerm } = useSearchActions()

    return (
        <div
            css={css`
                display: flex;
                flex-direction: column;
            `}
        >
            <div
                css={css`
                    flex-grow: 0;
                `}
            >
                <EuiFieldSearch
                    fullWidth
                    placeholder="Search the docs or ask Elastic Docs AI Assistant"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    onSearch={(e) => {
                        submitAskAiTerm(e)
                    }}
                    isClearable
                    autoFocus={true}
                />
                <EuiSpacer size="m" />
            </div>
            <div
                css={css`
                    flex-grow: 1;
                    overflow-y: scroll;
                    max-height: 80vh;
                    ${useEuiOverflowScroll('y')}
                `}
            >
                <SearchResults />
                {askAiTerm ? (
                    <AskAiAnswer />
                ) : (
                    <AskAiSuggestions
                        suggestions={[
                            { question: 'What is an index template?' },
                            { question: 'What is semantic search?' },
                            { question: 'How do I create an elasticsearch index?' },
                            { question: 'How do I set up an ingest pipeline?' },
                        ]}
                    />
                )}
            </div>
            <EuiHorizontalRule margin="m" />
            <div
                css={css`
                    flex-grow: 0;
                    display: flex;
                    align-items: center;
                    gap: calc(var(--spacing) * 2);
                `}
            >
                <EuiBetaBadge
                    size="s"
                    css={css`
                        block-size: 2em;
                        display: flex;
                    `}
                    label="Beta"
                    color="accent"
                    tooltipContent="This feature is in beta. Got feedback? We'd love to hear it!"
                />

                <EuiText color="subdued" size="xs">
                    This feature is in beta. Got feedback? We'd love to hear it!
                </EuiText>
            </div>
        </div>
    )
}
