/** @jsxImportSource @emotion/react */
import { logInfo } from '../../../../telemetry/logging'
import {
    ATTR_SEARCH_QUERY,
    ATTR_SEARCH_RESULT_URL,
    ATTR_SEARCH_RESULT_TITLE,
    ATTR_SEARCH_RESULT_POSITION,
    ATTR_SEARCH_RESULT_POSITION_ON_PAGE,
    ATTR_SEARCH_RESULT_SCORE,
    ATTR_SEARCH_PAGE,
    ATTR_EVENT_NAME,
    ATTR_EVENT_CATEGORY,
} from '../../../../telemetry/semconv'
import { useSearchTerm } from '../search.store'
import { type SearchResultItem } from '../useSearchQuery'
import {
    EuiText,
    useEuiTheme,
    useEuiFontSize,
    EuiIcon,
    EuiSpacer,
} from '@elastic/eui'
import { css } from '@emotion/react'
import DOMPurify from 'dompurify'
import { memo, useMemo } from 'react'

function trackSearchResultClick(params: {
    query: string
    resultUrl: string
    resultTitle: string
    absolutePosition: number
    positionOnPage: number
    pageNumber: number
    score: number
}): void {
    logInfo('search_result_clicked', {
        [ATTR_SEARCH_QUERY]: params.query,
        [ATTR_SEARCH_RESULT_URL]: params.resultUrl,
        [ATTR_SEARCH_RESULT_TITLE]: params.resultTitle,
        [ATTR_SEARCH_RESULT_POSITION]: params.absolutePosition,
        [ATTR_SEARCH_RESULT_POSITION_ON_PAGE]: params.positionOnPage,
        [ATTR_SEARCH_PAGE]: params.pageNumber,
        [ATTR_SEARCH_RESULT_SCORE]: params.score,
        [ATTR_EVENT_NAME]: 'search_result_clicked',
        [ATTR_EVENT_CATEGORY]: 'ui',
    })
}

interface SearchResultListItemProps {
    item: SearchResultItem
    index: number
    pageNumber: number
    pageSize: number
    isSelected?: boolean
    tabIndex?: 0 | -1
    onSelect?: (index: number) => void
    onKeyDown?: (
        e: React.KeyboardEvent<HTMLAnchorElement>,
        index: number
    ) => void
    setRef?: (el: HTMLAnchorElement | null) => void
}

export function SearchResultListItem({
    item: result,
    index,
    pageNumber,
    pageSize,
    isSelected,
    tabIndex = -1,
    onSelect,
    onKeyDown,
    setRef,
}: SearchResultListItemProps) {
    const { euiTheme } = useEuiTheme()
    const titleFontSize = useEuiFontSize('m')
    const searchQuery = useSearchTerm()

    // Calculate absolute position across all pages
    // pageNumber is 0-based, so multiply by pageSize and add the index
    const absolutePosition = pageNumber * pageSize + index

    const handleClick = () => {
        trackSearchResultClick({
            query: searchQuery,
            resultUrl: result.url,
            resultTitle: result.title,
            absolutePosition,
            positionOnPage: index,
            pageNumber,
            score: result.score,
        })
    }

    return (
        <li
            css={css`
                :not(:last-child) {
                    margin-bottom: ${euiTheme.size.xs};
                }
            `}
        >
            <a
                ref={setRef}
                data-selected={isSelected || undefined}
                tabIndex={tabIndex}
                role="option"
                aria-selected={isSelected}
                onClick={handleClick}
                onMouseEnter={(e) => {
                    // If another result item has focus, move focus to this item
                    if (document.activeElement instanceof HTMLElement) {
                        const isResultItem = document.activeElement.closest(
                            '[data-search-results]'
                        )
                        if (
                            isResultItem &&
                            document.activeElement !== e.currentTarget
                        ) {
                            e.currentTarget.focus()
                        }
                    }
                    onSelect?.(index)
                }}
                onFocus={() => onSelect?.(index)}
                onKeyDown={(e) => onKeyDown?.(e, index)}
                css={css`
                    display: grid;
                    grid-template-columns: auto 1fr auto;
                    align-items: center;
                    gap: ${euiTheme.size.m};
                    border-radius: ${euiTheme.size.s};
                    border-width: 1px;
                    border-style: solid;
                    border-color: transparent;
                    padding-inline-start: ${euiTheme.size.m};
                    padding-inline-end: ${euiTheme.size.base};
                    padding-block: ${euiTheme.size.m};
                    margin-inline-start: ${euiTheme.size.base};
                    margin-inline-end: ${euiTheme.size.s};
                    outline: none;
                    outline-color: transparent;

                    /* Selected: background + border (hover updates selection via onMouseEnter) */
                    &[data-selected] {
                        background-color: ${euiTheme.colors
                            .backgroundBaseHighlighted};
                        border-color: ${euiTheme.colors.borderBasePlain};

                        .return-key-icon {
                            visibility: visible;
                        }
                    }

                    /* Focus ring for selected and focus states */
                    &:focus-visible {
                        outline: 2px solid
                            ${euiTheme.colors.borderStrongPrimary};
                        outline-offset: -2px;
                        border-color: ${euiTheme.colors.borderStrongPrimary};
                    }
                `}
                href={result.url}
            >
                <EuiIcon
                    className="result-type-icon"
                    type={result.type === 'api' ? 'code' : 'documentation'}
                    size="m"
                    css={css`
                        color: ${euiTheme.colors.borderBaseProminent};
                    `}
                />
                <div
                    css={css`
                        mark {
                            background-color: transparent;
                            color: ${euiTheme.colors.link};
                            font-weight: ${euiTheme.font.weight.bold};
                        }
                    `}
                >
                    {result.parents.length > 0 && (
                        <>
                            <Breadcrumbs
                                type={result.type}
                                parents={result.parents}
                            />
                        </>
                    )}
                    <div
                        css={css`
                            font-size: ${titleFontSize.fontSize};
                            font-weight: ${euiTheme.font.weight.semiBold};
                        `}
                    >
                        <SanitizedHtmlContent
                            htmlContent={result.title}
                            ellipsis={false}
                        />
                    </div>
                    <EuiSpacer
                        css={css`
                            block-size: 2px;
                        `}
                    />
                    <EuiText size="xs">
                        <div
                            css={css`
                                font-family: ${euiTheme.font.family};
                                color: ${euiTheme.colors.textSubdued};
                                position: relative;
                                display: -webkit-box;
                                -webkit-line-clamp: 1;
                                -webkit-box-orient: vertical;
                                overflow: hidden;
                            `}
                        >
                            <SanitizedHtmlContent
                                htmlContent={result.description}
                                ellipsis={true}
                            />
                        </div>
                    </EuiText>
                </div>
                <EuiIcon
                    className="return-key-icon"
                    css={css`
                        visibility: hidden;
                        color: ${euiTheme.colors.borderBaseProminent};
                    `}
                    type="returnKey"
                    color="subdued"
                    size="m"
                />
            </a>
        </li>
    )
}

function Breadcrumbs({
    type,
    parents,
}: {
    type: SearchResultItem['type']
    parents: SearchResultItem['parents']
}) {
    const { euiTheme } = useEuiTheme()
    const { fontSize: smallFontsize } = useEuiFontSize('xs')
    return (
        <ul
            css={css`
                display: flex;
                gap: 0 ${euiTheme.size.xs};
                flex-wrap: wrap;
                list-style: none;
            `}
        >
            <li
                key={'breadcrumb-' + type}
                css={css`
                    &:not(:last-child)::after {
                        content: '/';
                        margin-left: ${euiTheme.size.xs};
                        font-size: ${smallFontsize};
                        color: ${euiTheme.colors.textSubdued};
                        margin-top: -1px;
                    }
                    display: inline-flex;
                `}
            >
                <EuiText
                    size="xs"
                    css={css`
                        color: ${euiTheme.colors.textSubdued};
                    `}
                >
                    {type === 'api' ? 'API' : 'Docs'}
                </EuiText>
            </li>
            {parents.slice(1).map((parent) => (
                <li
                    key={'breadcrumb-' + parent.url}
                    css={css`
                        &:not(:last-child)::after {
                            content: '/';
                            margin-left: ${euiTheme.size.xs};
                            font-size: ${smallFontsize};
                            color: ${euiTheme.colors.textSubdued};
                            margin-top: -1px;
                        }
                        display: inline-flex;
                    `}
                >
                    <EuiText
                        size="xs"
                        css={css`
                            color: ${euiTheme.colors.textSubdued};
                        `}
                    >
                        {parent.title}
                    </EuiText>
                </li>
            ))}
        </ul>
    )
}

const SanitizedHtmlContent = memo(
    ({ htmlContent, ellipsis }: { htmlContent: string; ellipsis: boolean }) => {
        const processed = useMemo(() => {
            if (!htmlContent) return ''

            const sanitized = DOMPurify.sanitize(htmlContent, {
                ALLOWED_TAGS: ['mark'],
                ALLOWED_ATTR: [],
                KEEP_CONTENT: true,
            })

            if (!ellipsis) {
                return sanitized
            }

            const temp = document.createElement('div')
            temp.innerHTML = sanitized

            const text = temp.textContent || ''
            const firstChar = text.trim()[0]

            // Add ellipsis when text starts mid-sentence to indicate continuation
            if (firstChar && /[a-z]/.test(firstChar)) {
                return '… ' + sanitized
            }

            return sanitized
        }, [htmlContent])

        return <div dangerouslySetInnerHTML={{ __html: processed }} />
    }
)

SanitizedHtmlContent.displayName = 'SanitizedHtmlContent'
