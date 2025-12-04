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
    onFocus?: (index: number) => void
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
    onFocus,
    onKeyDown,
    setRef,
}: SearchResultListItemProps) {
    const { euiTheme } = useEuiTheme()
    const titleFontSize = useEuiFontSize('s')
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
        <li>
            <a
                ref={setRef}
                data-selected={isSelected || undefined}
                tabIndex={0}
                onClick={handleClick}
                onFocus={() => onFocus?.(index)}
                onKeyDown={(e) => onKeyDown?.(e, index)}
                css={css`
                    display: grid;
                    grid-template-columns: auto 1fr auto;
                    align-items: center;
                    gap: ${euiTheme.size.base};
                    border-radius: ${euiTheme.border.radius.medium};
                    padding-inline: ${euiTheme.size.base};
                    padding-block: ${euiTheme.size.m};
                    margin-inline: ${euiTheme.size.base};
                    border: 1px solid transparent;
                    outline: none;

                    /* Shared highlight styles for selected, hover, and focus */
                    &[data-selected],
                    &:hover,
                    &:focus {
                        background-color: ${euiTheme.colors
                            .backgroundBaseSubdued};
                        border-color: ${euiTheme.colors.borderBasePlain};
                        .return-key-icon {
                            visibility: visible;
                        }
                    }

                    /* Focus ring for selected and focus states */
                    &[data-selected],
                    &:focus {
                        outline: 2px solid ${euiTheme.colors.primary};
                        outline-offset: -2px;
                    }
                `}
                href={result.url}
            >
                <EuiIcon
                    type={result.type === 'api' ? 'code' : 'document'}
                    color="subdued"
                    size="m"
                />
                <div
                    css={css`
                        mark {
                            background-color: transparent;
                            color: ${euiTheme.colors.link};
                        }
                    `}
                >
                    <div
                        css={css`
                            font-size: ${titleFontSize.fontSize};
                            font-weight: ${euiTheme.font.weight.semiBold};
                        `}
                    >
                        <SanitizedHtmlContent
                            htmlContent={
                                result.highlightedTitle ?? result.title
                            }
                            ellipsis={false}
                        />
                    </div>
                    <EuiSpacer size="xs" />
                    <EuiText size="xs">
                        <div
                            css={css`
                                font-family: ${euiTheme.font.family};
                                position: relative;

                                display: -webkit-box;
                                -webkit-line-clamp: 1;
                                -webkit-box-orient: vertical;
                                overflow: hidden;

                                //width: 90%;
                            `}
                        >
                            {result.highlightedBody ? (
                                <SanitizedHtmlContent
                                    htmlContent={result.highlightedBody}
                                    ellipsis={true}
                                />
                            ) : (
                                <span>{result.description}</span>
                            )}
                        </div>
                    </EuiText>
                    {result.parents.length > 0 && (
                        <>
                            <EuiSpacer size="xs" />
                            <Breadcrumbs
                                type={result.type}
                                parents={result.parents}
                            />
                        </>
                    )}
                </div>
                <EuiIcon
                    className="return-key-icon"
                    css={css`
                        visibility: hidden;
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
                margin-top: 2px;
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
                <EuiText size="xs" color="subdued">
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
                    <EuiText size="xs" color="subdued">
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
