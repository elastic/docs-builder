import { AskAiAnswer } from './AskAiAnswer'
import { Suggestions } from './Suggestions'
import { useAskAiTerm, useSearchActions, useSearchTerm } from './search.store'
import {
    EuiFieldSearch,
    EuiSpacer,
    EuiBetaBadge,
    EuiText,
    EuiHorizontalRule,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

/** @jsxImportSource @emotion/react */

export const SearchOrAskAiModal = () => {
    const searchTerm = useSearchTerm()
    const askAiTerm = useAskAiTerm()
    const { setSearchTerm, submitAskAiTerm } = useSearchActions()

    return (
        <>
            <EuiFieldSearch
                fullWidth
                placeholder="Search the docs or ask Elastic Docs AI Assistant"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onSearch={(e) => {
                    submitAskAiTerm(e)
                }}
                isClearable
            />
            <EuiSpacer size="m" />
            {askAiTerm ? <AskAiAnswer /> : <Suggestions />}
            <EuiHorizontalRule margin="m" />
            <div
                css={css`
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
        </>
    )
}
