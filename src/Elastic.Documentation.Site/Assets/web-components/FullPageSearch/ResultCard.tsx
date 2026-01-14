import { DocumentIcon, ConsoleIcon } from './FilterSidebar'
import type { SearchResultItem } from './useFullPageSearchQuery'
import {
    EuiBadge,
    EuiButtonIcon,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiPanel,
    EuiText,
    EuiToolTip,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, ReactNode } from 'react'

// Inline SVG icons for result types (breadcrumb size)
const TYPE_ICON_COMPONENTS: Record<string, ReactNode> = {
    doc: <DocumentIcon size={12} />,
    api: <ConsoleIcon size={12} />,
}

const TYPE_ICONS: Record<string, string> = {
    reference: 'documentation',
    api: 'console',
    guide: 'document',
    tutorial: 'training',
    doc: 'document',
}

const CalendarIcon = ({ size = 12 }: { size?: number }) => (
    <svg
        xmlns="http://www.w3.org/2000/svg"
        width={size}
        height={size}
        viewBox="0 0 16 16"
        fill="currentColor"
    >
        <path
            fillRule="evenodd"
            d="M14 4v-.994C14 2.45 13.55 2 12.994 2H11v1h-1V2H6v1H5V2H3.006C2.45 2 2 2.45 2 3.006v9.988C2 13.55 2.45 14 3.006 14h9.988C13.55 14 14 13.55 14 12.994V5H2V4h12zm-3-3h1.994C14.102 1 15 1.897 15 3.006v9.988A2.005 2.005 0 0 1 12.994 15H3.006A2.005 2.005 0 0 1 1 12.994V3.006C1 1.898 1.897 1 3.006 1H5V0h1v1h4V0h1v1zM4 7h2v1H4V7zm3 0h2v1H7V7zm3 0h2v1h-2V7zM4 9h2v1H4V9zm3 0h2v1H7V9zm3 0h2v1h-2V9zm-6 2h2v1H4v-1zm3 0h2v1H7v-1zm3 0h2v1h-2v-1z"
        />
    </svg>
)

interface BreadcrumbsProps {
    parents: { title: string; url: string }[]
    typeIcon?: ReactNode
}

const Breadcrumbs = ({ parents, typeIcon }: BreadcrumbsProps) => {
    const { euiTheme } = useEuiTheme()

    if (parents.length === 0 && !typeIcon) return null

    // For long breadcrumbs (more than 3 items), show first, ellipsis, and last two
    const shouldTruncate = parents.length > 3
    const displayedParents = shouldTruncate
        ? [parents[0], ...parents.slice(-2)]
        : parents

    return (
        <div
            css={css`
                display: flex;
                align-items: center;
                gap: 6px;
                margin-bottom: ${euiTheme.size.xs};
                overflow: hidden;
                white-space: nowrap;
                font-size: 12px;
                color: ${euiTheme.colors.subduedText};
            `}
        >
            {typeIcon && (
                <span
                    css={css`
                        display: flex;
                        align-items: center;
                        flex-shrink: 0;
                        opacity: 0.7;
                    `}
                >
                    {typeIcon}
                </span>
            )}
            {displayedParents.map((parent, idx) => (
                <span
                    key={idx}
                    css={css`
                        display: flex;
                        align-items: center;
                        gap: 6px;
                        min-width: 0;
                        flex-shrink: ${idx === displayedParents.length - 1
                            ? 1
                            : 0};
                    `}
                >
                    {idx > 0 && (
                        <span
                            css={css`
                                flex-shrink: 0;
                                opacity: 0.5;
                            `}
                        >
                            {shouldTruncate && idx === 1 ? '…›' : '›'}
                        </span>
                    )}
                    <span
                        css={css`
                            overflow: hidden;
                            text-overflow: ellipsis;
                        `}
                    >
                        {parent.title}
                    </span>
                </span>
            ))}
        </div>
    )
}

interface ResultCardProps {
    result: SearchResultItem
}

export const ResultCard = ({ result }: ResultCardProps) => {
    const { euiTheme } = useEuiTheme()
    const [copied, setCopied] = useState(false)
    const [showAiSummary, setShowAiSummary] = useState(false)

    const copyUrl = (e: React.MouseEvent) => {
        e.preventDefault()
        e.stopPropagation()
        navigator.clipboard.writeText(window.location.origin + result.url)
        setCopied(true)
        setTimeout(() => setCopied(false), 2000)
    }

    const handleCardClick = (e: React.MouseEvent) => {
        // Don't navigate if clicking on interactive elements inside the card
        const target = e.target as HTMLElement
        const currentTarget = e.currentTarget as HTMLElement

        // Find if there's a button between target and currentTarget (exclusive of currentTarget)
        let element: HTMLElement | null = target
        while (element && element !== currentTarget) {
            if (
                element.tagName === 'BUTTON' ||
                element.getAttribute('role') === 'button'
            ) {
                return
            }
            element = element.parentElement
        }

        window.location.href = result.url
    }

    const typeIcon = TYPE_ICONS[result.type] || 'document'
    const formattedDate = result.lastUpdated
        ? new Date(result.lastUpdated).toLocaleDateString()
        : null

    // Shared mark styles for highlighting
    const markStyles = css`
        mark {
            background: ${euiTheme.colors.lightestShade};
            font-weight: ${euiTheme.font.weight.semiBold};
            color: ${euiTheme.colors.text};
            padding: 0.1em 0.25em;
            border-radius: 0.2em;
        }
    `

    return (
        <EuiPanel
            paddingSize="l"
            hasBorder
            onClick={handleCardClick}
            css={css`
                position: relative;
                transition: all 0.2s ease;
                cursor: pointer;

                &:hover {
                    border-color: ${euiTheme.colors.lightShade};
                    box-shadow: ${euiTheme.levels.menu};

                    .result-title {
                        color: ${euiTheme.colors.primary};
                    }
                }
            `}
        >
            <div
                css={css`
                    position: absolute;
                    top: ${euiTheme.size.m};
                    right: ${euiTheme.size.m};
                    z-index: 1;
                `}
            >
                <EuiToolTip content={copied ? 'Copied!' : 'Copy link'}>
                    <EuiButtonIcon
                        iconType={copied ? 'check' : 'copy'}
                        aria-label="Copy link"
                        onClick={copyUrl}
                        color={copied ? 'success' : 'text'}
                        css={css`
                            opacity: 0;
                            transition: opacity 0.2s ease;

                            *:hover > div > & {
                                opacity: 1;
                            }
                        `}
                    />
                </EuiToolTip>
            </div>

            <Breadcrumbs
                parents={result.parents}
                typeIcon={
                    TYPE_ICON_COMPONENTS[result.type] ?? (
                        <EuiIcon type={typeIcon} size="s" />
                    )
                }
            />

            <div
                css={css`
                    margin-right: ${euiTheme.size.xxl};
                    margin-bottom: ${euiTheme.size.xs};
                `}
            >
                <EuiText>
                    <h3
                        className="result-title"
                        css={css`
                            font-size: 1.1rem;
                            font-weight: ${euiTheme.font.weight.semiBold};
                            color: ${euiTheme.colors.title};
                            transition: color 0.2s ease;

                            mark {
                                background: transparent;
                                color: inherit;
                                font-weight: inherit;
                            }
                        `}
                        dangerouslySetInnerHTML={{ __html: result.title }}
                    />
                </EuiText>
            </div>

            <EuiText
                size="s"
                color="subdued"
                css={css`
                    margin-bottom: ${euiTheme.size.s};
                    ${markStyles}
                `}
                dangerouslySetInnerHTML={{ __html: result.description }}
            />

            {result.aiShortSummary && (
                <>
                    <button
                        onClick={(e) => {
                            e.stopPropagation()
                            setShowAiSummary(!showAiSummary)
                        }}
                        css={css`
                            background: linear-gradient(
                                135deg,
                                rgba(155, 89, 182, 0.08) 0%,
                                rgba(0, 119, 204, 0.08) 100%
                            );
                            border: 1px solid rgba(155, 89, 182, 0.15);
                            border-radius: ${euiTheme.border.radius.medium};
                            color: #7b4b9e;
                            cursor: pointer;
                            font-size: 13px;
                            font-weight: ${euiTheme.font.weight.medium};
                            padding: 6px 12px;
                            display: inline-flex;
                            align-items: center;
                            gap: ${euiTheme.size.xs};
                            margin-bottom: ${euiTheme.size.s};
                            transition: all 0.2s ease;

                            &:hover {
                                background: linear-gradient(
                                    135deg,
                                    #9b59b6 0%,
                                    #0077cc 100%
                                );
                                border-color: transparent;
                                color: white;
                            }

                            &:hover svg {
                                fill: white;
                            }
                        `}
                    >
                        <EuiIcon type="sparkles" size="s" />
                        <span>
                            {showAiSummary ? 'Hide' : 'Show'} AI summary
                        </span>
                        <EuiIcon
                            type={showAiSummary ? 'arrowUp' : 'arrowDown'}
                            size="s"
                        />
                    </button>
                    {showAiSummary && (
                        <div
                            css={css`
                                margin-bottom: ${euiTheme.size.s};
                                padding: ${euiTheme.size.m};
                                background: linear-gradient(
                                    135deg,
                                    rgba(155, 89, 182, 0.03) 0%,
                                    rgba(0, 119, 204, 0.03) 100%
                                );
                                border-left: 3px solid;
                                border-image: linear-gradient(
                                        135deg,
                                        #9b59b6 0%,
                                        #0077cc 100%
                                    )
                                    1;
                                border-radius: 0 ${euiTheme.border.radius.small}
                                    ${euiTheme.border.radius.small} 0;
                            `}
                        >
                            <EuiText size="s" color="default">
                                <p
                                    css={css`
                                        margin: 0;
                                        line-height: 1.6;
                                    `}
                                >
                                    {result.aiRagOptimizedSummary}
                                </p>
                            </EuiText>
                        </div>
                    )}
                </>
            )}

            <EuiFlexGroup
                alignItems="center"
                justifyContent="spaceBetween"
                css={css`
                    padding-top: ${euiTheme.size.s};
                    border-top: 1px solid ${euiTheme.border.color};
                `}
            >
                <EuiFlexItem grow={false}>
                    <EuiFlexGroup gutterSize="xs" wrap>
                        <EuiFlexItem grow={false}>
                            <EuiBadge color="hollow">{result.type}</EuiBadge>
                        </EuiFlexItem>
                        {result.navigationSection && (
                            <EuiFlexItem grow={false}>
                                <EuiBadge color="hollow">
                                    {result.navigationSection}
                                </EuiBadge>
                            </EuiFlexItem>
                        )}
                    </EuiFlexGroup>
                </EuiFlexItem>
                {formattedDate && (
                    <EuiFlexItem grow={false}>
                        <EuiFlexGroup gutterSize="xs" alignItems="center">
                            <EuiFlexItem
                                grow={false}
                                css={css`
                                    display: flex;
                                    align-items: center;
                                    color: ${euiTheme.colors.subduedText};
                                `}
                            >
                                <CalendarIcon size={12} />
                            </EuiFlexItem>
                            <EuiFlexItem grow={false}>
                                <EuiText size="xs" color="subdued">
                                    {formattedDate}
                                </EuiText>
                            </EuiFlexItem>
                        </EuiFlexGroup>
                    </EuiFlexItem>
                )}
            </EuiFlexGroup>
        </EuiPanel>
    )
}
