import { useSearchTerm } from '../search.store'
import { SearchResultItem, useSearchQuery } from './useSearchQuery'
import {
    useEuiFontSize,
    EuiHighlight,
    EuiLink,
    EuiLoadingSpinner,
    EuiSpacer,
    EuiText,
    useEuiTheme,
    EuiIcon,
    EuiPagination,
    EuiHorizontalRule,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useDebounce } from '@uidotdev/usehooks'
import * as React from 'react'
import { useEffect, useMemo, useState } from 'react'

export const SearchResults = () => {
    const searchTerm = useSearchTerm()
    const [activePage, setActivePage] = useState(0)
    const debouncedSearchTerm = useDebounce(searchTerm, 300)
    useEffect(() => {
        setActivePage(0)
    }, [debouncedSearchTerm])
    const { data, error, isLoading, isFetching } = useSearchQuery({
        searchTerm,
        pageNumber: activePage + 1,
    })
    const { euiTheme } = useEuiTheme()

    if (!searchTerm) {
        return
    }

    if (error) {
        return <div>Error loading search results: {error.message}</div>
    }

    return (
        <div>
            <div
                css={css`
                    display: flex;
                    gap: ${euiTheme.size.s};
                    align-items: center;
                `}
            >
                {isLoading || isFetching ? (
                    <EuiLoadingSpinner size="s" />
                ) : (
                    <EuiIcon type="search" color="subdued" size="s" />
                )}
                <EuiText size="xs">
                    Search results for{' '}
                    <span
                        css={css`
                            font-weight: ${euiTheme.font.weight.bold};
                        `}
                    >
                        {searchTerm}
                    </span>
                </EuiText>
            </div>
            <EuiSpacer size="s" />
            {data && (
                <>
                    <ul>
                        {data.results.map((result) => (
                            <SearchResultListItem item={result} />
                        ))}
                    </ul>
                    <EuiSpacer size="m" />
                    <div
                        css={css`
                            display: flex;
                            justify-content: center;
                        `}
                    >
                        <EuiPagination
                            aria-label="Search results pages"
                            pageCount={Math.min(data.pageCount, 10)}
                            activePage={activePage}
                            onPageClick={(activePage) =>
                                setActivePage(activePage)
                            }
                        />
                    </div>
                </>
            )}
            <EuiHorizontalRule margin="m" />
        </div>
    )
}

interface SearchResultListItemProps {
    item: SearchResultItem
}

function SearchResultListItem({ item: result }: SearchResultListItemProps) {
    const { euiTheme } = useEuiTheme()
    const searchTerm = useSearchTerm()
    const highlightSearchTerms = useMemo(
        () =>
            searchTerm
                .toLowerCase()
                .split(' ')
                .filter((i) => i.length > 1),
        [searchTerm]
    )

    if (highlightSearchTerms.includes('esql')) {
        highlightSearchTerms.push('es|ql')
    }

    if (highlightSearchTerms.includes('dotnet')) {
        highlightSearchTerms.push('.net')
    }
    return (
        <li key={result.url}>
            <div
                tabIndex={0}
                css={css`
                            display: flex; 
                            align-items: flex-start;
                            gap: ${euiTheme.size.s};
                            padding-inline: ${euiTheme.size.s};
                            padding-block: ${euiTheme.size.xs};
                            border-radius: ${euiTheme.border.radius.small};
                            :hover {
                                background-color: ${euiTheme.colors.backgroundTransparentSubdued};
                        `}
            >
                <EuiIcon
                    type="document"
                    color="subdued"
                    css={css`
                        margin-top: ${euiTheme.size.xs};
                    `}
                />
                <div
                    css={css`
                        width: 100%;
                        text-align: left;
                    `}
                >
                    <EuiLink
                        tabIndex={-1}
                        href={result.url}
                        css={css`
                            .euiMark {
                                background-color: ${euiTheme.colors
                                    .backgroundLightWarning};
                                font-weight: inherit;
                            }
                        `}
                    >
                        <EuiHighlight
                            search={highlightSearchTerms}
                            highlightAll={true}
                        >
                            {result.title}
                        </EuiHighlight>
                    </EuiLink>
                    <Breadcrumbs
                        parents={result.parents}
                        highlightSearchTerms={highlightSearchTerms}
                    />
                </div>
            </div>
        </li>
    )
}

function Breadcrumbs({
    parents,
    highlightSearchTerms,
}: {
    parents: SearchResultItem['parents']
    highlightSearchTerms: string[]
}) {
    const { euiTheme } = useEuiTheme()
    const { fontSize: smallFontsize } = useEuiFontSize('xs')
    return (
        <ul
            css={css`
                margin-top: 2px;
                display: flex;
                gap: 0 ${euiTheme.size.xs};
                flex-wrap: wrap;
                list-style: none;
            `}
        >
            {parents
                .slice(1) // skip /docs
                .map((parent) => (
                    <li
                        key={'breadcrumb-' + parent.url}
                        css={css`
                            &:not(:last-child)::after {
                                content: '/';
                                margin-left: ${euiTheme.size.xs};
                                font-size: ${smallFontsize};
                                color: ${euiTheme.colors.text};
                                margin-top: -1px;
                            }
                            display: inline-flex;
                        `}
                    >
                        <EuiLink href={parent.url} color="text" tabIndex={-1}>
                            <EuiText
                                size="xs"
                                color="subdued"
                                css={css`
                                    .euiMark {
                                        background-color: transparent;
                                        text-decoration: underline;
                                        color: inherit;
                                        font-weight: inherit;
                                    }
                                `}
                            >
                                <EuiHighlight
                                    search={highlightSearchTerms}
                                    highlightAll={true}
                                >
                                    {parent.title}
                                </EuiHighlight>
                            </EuiText>
                        </EuiLink>
                    </li>
                ))}
        </ul>
    )
}
