import { useSearchTerm } from './search.store'
import { SearchResultItem, useSearchQuery } from './useSearchQuery'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import {
    useEuiFontSize,
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
import DOMPurify from 'dompurify'
import { useEffect, useMemo, useState, memo } from 'react'
import { ApiError } from '../errorHandling'

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
        return null
    }

    return (
        <>
            <SearchOrAskAiErrorCallout
                error={error as ApiError | Error | null}
                title="Error loading search results"
            />
            {error && <EuiSpacer size="s" />}
            
            {!error && (
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
                                    <SearchResultListItem
                                        item={result}
                                        key={result.url}
                                    />
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
            )}
        </>
    )
}

interface SearchResultListItemProps {
    item: SearchResultItem
}

function SearchResultListItem({ item: result }: SearchResultListItemProps) {
    const { euiTheme } = useEuiTheme()
    const titleFontSize = useEuiFontSize('m')
    return (
        <li
            tabIndex={0}
            css={css`
                :not(:first-child) {
                    border-top: 1px dotted ${euiTheme.colors.borderBasePrimary};
                }
            `}
        >
            <div
                css={css`
                            display: flex; 
                            align-items: flex-start;
                            gap: ${euiTheme.size.s};
                            padding-inline: ${euiTheme.size.s};
                            padding-block: ${euiTheme.size.m};
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
                    <Breadcrumbs parents={result.parents} />
                    <div
                        css={css`
                            padding-block: ${euiTheme.size.xs};
                            font-size: ${titleFontSize.fontSize};
                        `}
                    >
                        <EuiLink tabIndex={-1} href={result.url}>
                            <span>{result.title}</span>
                        </EuiLink>
                    </div>

                    <EuiText size="s">
                        <div
                            css={css`
                                font-family: ${euiTheme.font.family};
                                position: relative;

                                /* 2 lines with ellipsis */
                                display: -webkit-box;
                                -webkit-line-clamp: 1;
                                -webkit-box-orient: vertical;
                                overflow: hidden;

                                width: 90%;

                                mark {
                                    background-color: transparent;
                                    font-weight: ${euiTheme.font.weight.bold};
                                    color: ${euiTheme.colors.ink};
                                }
                            `}
                        >
                            {result.highlightedBody ? (
                                <SanitizedHtmlContent
                                    htmlContent={result.highlightedBody}
                                />
                            ) : (
                                <span>{result.description}</span>
                            )}
                        </div>
                    </EuiText>
                </div>
            </div>
        </li>
    )
}

function Breadcrumbs({ parents }: { parents: SearchResultItem['parents'] }) {
    const { euiTheme } = useEuiTheme()
    const { fontSize: smallFontsize } = useEuiFontSize('xs')
    return (
        <ul
            css={css`
                margin-top: 2px;
                display: flex;
                gap: 0 ${euiTheme.size.s};
                flex-wrap: wrap;
                list-style: none;
            `}
        >
            {parents
                // .slice(1) // skip /docs
                .map((parent) => (
                    <li
                        key={'breadcrumb-' + parent.url}
                        css={css`
                            &:not(:last-child)::after {
                                content: '/';
                                margin-left: ${euiTheme.size.s};
                                font-size: ${smallFontsize};
                                color: ${euiTheme.colors.textSubdued};
                                margin-top: -1px;
                            }
                            display: inline-flex;
                        `}
                    >
                        <EuiLink
                            href={parent.url}
                            color="subdued"
                            tabIndex={-1}
                        >
                            <EuiText size="xs" color="subdued">
                                {parent.title}
                            </EuiText>
                        </EuiLink>
                    </li>
                ))}
        </ul>
    )
}

const SanitizedHtmlContent = memo(
    ({ htmlContent }: { htmlContent: string }) => {
        const processed = useMemo(() => {
            if (!htmlContent) return ''

            const sanitized = DOMPurify.sanitize(htmlContent, {
                ALLOWED_TAGS: ['mark'],
                ALLOWED_ATTR: [],
                KEEP_CONTENT: true,
            })

            // Check if text starts mid-sentence (lowercase first letter)
            const temp = document.createElement('div')
            temp.innerHTML = sanitized
            const text = temp.textContent || ''
            const firstChar = text.trim()[0]

            // Add leading ellipsis if starts with lowercase
            if (firstChar && /[a-z]/.test(firstChar)) {
                return '… ' + sanitized
            }

            return sanitized
        }, [htmlContent])

        return <div dangerouslySetInnerHTML={{ __html: processed }} />
    }
)
