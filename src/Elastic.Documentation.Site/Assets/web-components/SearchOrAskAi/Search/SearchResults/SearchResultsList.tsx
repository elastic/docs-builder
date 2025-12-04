import { useIsInputFocused } from '../search.store'
import { type SearchResultItem } from '../useSearchQuery'
import { SearchResultListItem } from './SearchResultsListItem'
import {
    useEuiOverflowScroll,
    EuiSpacer,
    useEuiTheme,
    EuiSkeletonRectangle,
    EuiSkeletonLoading,
    EuiSkeletonText,
    EuiSkeletonTitle,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useRef, useCallback, useEffect } from 'react'

interface SearchResultsListProps {
    results: SearchResultItem[]
    pageNumber: number
    pageSize: number
    isLoading: boolean
    searchTerm: string
    onKeyDown?: (
        e: React.KeyboardEvent<HTMLAnchorElement>,
        index: number
    ) => void
    setItemRef?: (element: HTMLAnchorElement | null, index: number) => void
}

export const SearchResultsList = ({
    results,
    pageNumber,
    pageSize,
    isLoading,
    searchTerm,
    onKeyDown,
    setItemRef,
}: SearchResultsListProps) => {
    const { euiTheme } = useEuiTheme()
    const isInputFocused = useIsInputFocused()
    const scrollContainerRef = useRef<HTMLDivElement>(null)

    const scrollbarStyle = css`
        max-height: 400px;
        padding-block: ${euiTheme.size.base};
        margin-right: ${euiTheme.size.s};
        ${useEuiOverflowScroll('y', true)}
    `

    const resetScrollToTop = useCallback(() => {
        if (scrollContainerRef.current) {
            scrollContainerRef.current.scrollTop = 0
        }
    }, [])

    useEffect(() => {
        resetScrollToTop()
    }, [searchTerm, resetScrollToTop])

    return (
        <div data-search-results ref={scrollContainerRef} css={scrollbarStyle}>
            <EuiSkeletonLoading
                isLoading={isLoading}
                loadingContent={<SkeletonResults />}
                loadedContent={
                    <ul
                        css={css`
                            /* Hide pre-selection on first item when hovering another item */
                            &:has(li:not(:first-child):hover) li:first-child a {
                                background-color: transparent;
                                border-color: transparent;
                            }
                            &:has(li:not(:first-child):hover)
                                li:first-child
                                .return-key-icon {
                                visibility: hidden;
                            }
                        `}
                    >
                        {results.map((result, index) => (
                            <SearchResultListItem
                                item={result}
                                key={result.url}
                                index={index}
                                pageNumber={pageNumber}
                                pageSize={pageSize}
                                isPreSelected={index === 0 && isInputFocused}
                                onKeyDown={onKeyDown}
                                setRef={setItemRef}
                            />
                        ))}
                    </ul>
                }
            />
        </div>
    )
}

const SkeletonResults = () => {
    const { euiTheme } = useEuiTheme()

    return (
        <ul>
            {[1, 2, 3].map((i) => (
                <li
                    key={i}
                    css={css`
                        padding: ${euiTheme.size.m} ${euiTheme.size.base};
                        padding-right: calc(2 * ${euiTheme.size.base});
                        margin-inline: ${euiTheme.size.base};
                    `}
                >
                    <div
                        css={css`
                            display: grid;
                            grid-template-columns: auto 1fr;
                            gap: ${euiTheme.size.base};
                        `}
                    >
                        <div
                            css={css`
                                display: flex;
                                justify-content: center;
                                align-items: center;
                            `}
                        >
                            <EuiSkeletonRectangle
                                height={16}
                                width={16}
                                borderRadius="m"
                            />
                        </div>
                        <div>
                            <EuiSkeletonTitle size="xxxs" />
                            <EuiSpacer size="s" />
                            <EuiSkeletonText lines={1} size="xs" />
                            <EuiSpacer size="xs" />
                            <div
                                css={css`
                                    width: 80%;
                                `}
                            >
                                <EuiSkeletonText lines={2} size="xs" />
                            </div>
                        </div>
                    </div>
                </li>
            ))}
        </ul>
    )
}
