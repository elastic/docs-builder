/** @jsxImportSource @emotion/react */
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

interface SearchResultListItemProps {
    item: SearchResultItem
    index: number
    onKeyDown?: (e: React.KeyboardEvent<HTMLLIElement>, index: number) => void
    setRef?: (element: HTMLAnchorElement | null, index: number) => void
}

export function SearchResultListItem({
    item: result,
    index,
    onKeyDown,
    setRef,
}: SearchResultListItemProps) {
    const { euiTheme } = useEuiTheme()
    const titleFontSize = useEuiFontSize('s')
    return (
        <li
        // css={css`
        //     :not(:first-child) {
        //         border-top: 1px dotted ${euiTheme.colors.borderBasePrimary};
        //     }
        // `}
        >
            <a
                ref={(el) => setRef?.(el, index)}
                onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                        window.location.href = result.url
                    } else {
                        // Type mismatch: event is from anchor but handler expects HTMLLIElement
                        onKeyDown?.(
                            e as unknown as React.KeyboardEvent<HTMLLIElement>,
                            index
                        )
                    }
                }}
                css={css`
                    display: flex;
                    align-items: center;
                    gap: ${euiTheme.size.base};
                    border-radius: ${euiTheme.border.radius.small};
                    width: 100%;
                    padding-inline: ${euiTheme.size.base};
                    padding-block: ${euiTheme.size.m};
                    :hover {
                        background-color: ${euiTheme.colors
                            .backgroundBaseSubdued};
                    }
                    :focus {
                        background-color: ${euiTheme.colors
                            .backgroundBaseSubdued};
                    }
                    :focus .return-key-icon {
                        visibility: visible;
                    }
                `}
                tabIndex={0}
                href={result.url}
            >
                <EuiIcon
                    type="data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTQiIGhlaWdodD0iMTMiIHZpZXdCb3g9IjAgMCAxNCAxMyIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTExLjk3NDYgMC4zMTY0MDZMMTEuMDI3MyAzLjE1ODJIMTRWNC4xNTgySDEwLjY5NDNMOS4zNjAzNSA4LjE1ODJIMTJWOS4xNTgySDkuMDI3MzRMNy45NzQ2MSAxMi4zMTY0TDcuMDI1MzkgMTJMNy45NzI2NiA5LjE1ODJINC4wMjczNEwyLjk3NDYxIDEyLjMxNjRMMi4wMjUzOSAxMkwyLjk3MjY2IDkuMTU4MkgwVjguMTU4MkgzLjMwNTY2TDQuNjM5NjUgNC4xNTgySDJWMy4xNTgySDQuOTcyNjZMNi4wMjUzOSAwTDYuOTc0NjEgMC4zMTY0MDZMNi4wMjczNCAzLjE1ODJIOS45NzI2NkwxMS4wMjU0IDBMMTEuOTc0NiAwLjMxNjQwNlpNNC4zNjAzNSA4LjE1ODJIOC4zMDU2Nkw5LjYzOTY1IDQuMTU4Mkg1LjY5NDM0TDQuMzYwMzUgOC4xNTgyWiIgZmlsbD0iIzFEMkEzRSIvPgo8L3N2Zz4="
                    color="subdued"
                    size="m"
                />
                <div>
                    <div
                        css={css`
                            font-size: ${titleFontSize};
                            font-weight: ${euiTheme.font.weight.semiBold};
                        `}
                    >
                        {result.title}
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

                                mark {
                                    background-color: transparent;
                                    font-weight: ${euiTheme.font.weight.bold};
                                    color: ${euiTheme.colors.link};
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
                    {result.parents.length > 0 && (
                        <>
                            <EuiSpacer size="xs" />
                            <Breadcrumbs parents={result.parents} />
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
            {/*<div*/}
            {/*    css={css`*/}
            {/*        display: flex;*/}
            {/*        align-items: flex-start;*/}
            {/*        gap: ${euiTheme.size.s};*/}
            {/*        padding-inline: ${euiTheme.size.s};*/}
            {/*        padding-block: ${euiTheme.size.m};*/}
            {/*        :hover {*/}
            {/*            background-color: ${euiTheme.colors*/}
            {/*                .backgroundBaseSubdued};*/}
            {/*        }*/}
            {/*    `}*/}
            {/*>*/}
            {/*    <EuiIcon*/}
            {/*        type="document"*/}
            {/*        color="subdued"*/}
            {/*        css={css`*/}
            {/*            margin-top: ${euiTheme.size.xs};*/}
            {/*        `}*/}
            {/*    />*/}
            {/*    <div*/}
            {/*        css={css`*/}
            {/*            width: 100%;*/}
            {/*            text-align: left;*/}
            {/*        `}*/}
            {/*    >*/}
            {/*        <Breadcrumbs parents={result.parents} />*/}
            {/*        <div*/}
            {/*            css={css`*/}
            {/*                padding-block: ${euiTheme.size.xs};*/}
            {/*                font-size: ${titleFontSize.fontSize};*/}
            {/*            `}*/}
            {/*        >*/}
            {/*            <EuiLink tabIndex={-1} href={result.url}>*/}
            {/*                <span>{result.title}</span>*/}
            {/*            </EuiLink>*/}
            {/*        </div>*/}
            {/*    </div>*/}
            {/*</div>*/}
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
                gap: 0 ${euiTheme.size.xs};
                flex-wrap: wrap;
                list-style: none;
            `}
        >
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
                    {/*<EuiLink*/}
                    {/*    href={parent.url}*/}
                    {/*    color="subdued"*/}
                    {/*    tabIndex={-1}*/}
                    {/*>*/}
                    <EuiText size="xs" color="subdued">
                        {parent.title}
                    </EuiText>
                    {/*</EuiLink>*/}
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
