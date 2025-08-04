import { useSearchTerm } from './search.store'
import { useSearchQuery } from './useSearchQuery'
import {
    EuiButton,
    EuiLoadingSpinner,
    EuiSpacer,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'

export const SearchResults = () => {
    const searchTerm = useSearchTerm()
    const { data, error, isLoading } = useSearchQuery()
    const { euiTheme } = useEuiTheme()

    if (!searchTerm) {
        return <></>
    }

    if (error) {
        return <div>Error loading search results: {error.message}</div>
    }

    if (isLoading) {
        return (
            <div>
                <EuiLoadingSpinner size="s" /> Loading search results...
            </div>
        )
    }

    if (!data || data.results.length === 0) {
        return <EuiText size="xs">No results found for "{searchTerm}"</EuiText>
    }

    const buttonCss = css`
        border: none;
        vertical-align: top;
        justify-content: flex-start;
        block-size: 100%;
        padding-block: 4px;
        & > span {
            justify-content: flex-start;
            align-items: flex-start;
        }
        svg {
            color: ${euiTheme.colors.textSubdued};
        }
        .euiIcon {
            margin-top: 4px;
        }
    `

    const trimDescription = (description: string) => {
        const limit = 200
        return description.length > limit
            ? description.slice(0, limit) + '...'
            : description
    }

    return (
        <div
            css={`
                li:not(:first-child) {
                    margin-top: ${euiTheme.size.xs};
                }
            `}
        >
            <EuiText size="xs">Search Results for "{searchTerm}"</EuiText>
            <EuiSpacer size="s" />
            <ul>
                {data.results.map((result) => (
                    <li key={result.url}>
                        <EuiButton
                            css={buttonCss}
                            iconType="document"
                            color="text"
                            size="s"
                            fullWidth
                        >
                            <div
                                css={css`
                                    width: 100%;
                                    text-align: left;
                                `}
                            >
                                {result.title}
                                <EuiSpacer size="xs" />
                                <EuiText
                                    css={css`
                                        text-wrap: pretty;
                                    `}
                                    textAlign="left"
                                    size="xs"
                                    color="subdued"
                                >
                                    {trimDescription(result.description)}
                                </EuiText>
                            </div>
                        </EuiButton>
                        {/*<EuiIcon type="document" color="subdued" />*/}
                        {/*<EuiText>{result.title}</EuiText>*/}
                    </li>
                ))}
            </ul>
        </div>
    )
}
