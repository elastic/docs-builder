import {
    EuiBadge,
    EuiButtonIcon,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiLink,
    EuiPanel,
    EuiText,
    EuiToolTip,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, ReactNode } from 'react'
import type { SearchResultItem } from './useFullPageSearchQuery'
import { DocumentIcon, ConsoleIcon } from './FilterSidebar'

// Inline SVG icons for result types (breadcrumb size)
const TYPE_ICON_COMPONENTS: Record<string, ReactNode> = {
    doc: <DocumentIcon size={14} />,
    api: <ConsoleIcon size={14} />,
}

const TYPE_ICONS: Record<string, string> = {
    reference: 'documentation',
    api: 'console',
    guide: 'document',
    tutorial: 'training',
    doc: 'document',
}

interface BreadcrumbsProps {
    parents: { title: string; url: string }[]
    typeIcon?: ReactNode
}

const Breadcrumbs = ({ parents, typeIcon }: BreadcrumbsProps) => {
    const { euiTheme } = useEuiTheme()

    if (parents.length === 0 && !typeIcon) return null

    return (
        <EuiFlexGroup
            gutterSize="xs"
            alignItems="center"
            wrap
            css={css`
                margin-bottom: ${euiTheme.size.xs};
            `}
        >
            {typeIcon && (
                <EuiFlexItem grow={false}>
                    <div
                        css={css`
                            color: ${euiTheme.colors.subduedText};
                            display: flex;
                            align-items: center;
                            margin-right: ${euiTheme.size.xs};
                        `}
                    >
                        {typeIcon}
                    </div>
                </EuiFlexItem>
            )}
            {parents.map((parent, idx) => (
                <EuiFlexItem key={idx} grow={false}>
                    <EuiFlexGroup gutterSize="xs" alignItems="center">
                        {idx > 0 && (
                            <EuiFlexItem grow={false}>
                                <EuiIcon
                                    type="arrowRight"
                                    size="s"
                                    color="subdued"
                                />
                            </EuiFlexItem>
                        )}
                        <EuiFlexItem grow={false}>
                            <EuiText size="xs" color="subdued">
                                {parent.title}
                            </EuiText>
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </EuiFlexItem>
            ))}
        </EuiFlexGroup>
    )
}

interface ResultCardProps {
    result: SearchResultItem
}

export const ResultCard = ({ result }: ResultCardProps) => {
    const { euiTheme } = useEuiTheme()
    const [copied, setCopied] = useState(false)
    const [showAiSummary, setShowAiSummary] = useState(false)

    const copyUrl = () => {
        navigator.clipboard.writeText(window.location.origin + result.url)
        setCopied(true)
        setTimeout(() => setCopied(false), 2000)
    }

    const typeIcon = TYPE_ICONS[result.type] || 'document'
    const formattedDate = result.lastUpdated
        ? new Date(result.lastUpdated).toLocaleDateString()
        : null

    return (
        <EuiPanel
            paddingSize="l"
            hasBorder
            css={css`
                position: relative;
                transition: all 0.2s ease;

                &:hover {
                    border-color: ${euiTheme.colors.lightShade};
                    box-shadow: ${euiTheme.levels.menu};
                }
            `}
        >
            <div
                css={css`
                    position: absolute;
                    top: ${euiTheme.size.m};
                    right: ${euiTheme.size.m};
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
                typeIcon={TYPE_ICON_COMPONENTS[result.type] ?? <EuiIcon type={typeIcon} size="s" />}
            />

            <EuiLink
                href={result.url}
                css={css`
                    display: block;
                    margin-right: ${euiTheme.size.xxl};
                    margin-bottom: ${euiTheme.size.xs};
                `}
            >
                <EuiText>
                    <h3
                        css={css`
                            font-size: 1.1rem;
                            font-weight: ${euiTheme.font.weight.semiBold};

                            &:hover {
                                color: ${euiTheme.colors.primary};
                            }
                        `}
                        dangerouslySetInnerHTML={{ __html: result.title }}
                    />
                </EuiText>
            </EuiLink>

            <EuiText
                size="s"
                color="subdued"
                css={css`
                    margin-bottom: ${euiTheme.size.s};

                    mark {
                        background: linear-gradient(
                            120deg,
                            ${euiTheme.colors.warning} 0%,
                            ${euiTheme.colors.warning} 100%
                        );
                        padding: 0.1em 0.2em;
                        border-radius: 0.2em;
                    }
                `}
                dangerouslySetInnerHTML={{ __html: result.description }}
            />

            {result.aiShortSummary && (
                <>
                    <button
                        onClick={() => setShowAiSummary(!showAiSummary)}
                        css={css`
                            background: none;
                            border: none;
                            color: ${euiTheme.colors.accent};
                            cursor: pointer;
                            font-size: ${euiTheme.size.m};
                            padding: 0;
                            display: flex;
                            align-items: center;
                            gap: ${euiTheme.size.xs};
                            margin-bottom: ${euiTheme.size.s};

                            &:hover {
                                text-decoration: underline;
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
                        <EuiPanel
                            color="accent"
                            paddingSize="s"
                            css={css`
                                margin-bottom: ${euiTheme.size.s};
                            `}
                        >
                            <EuiText size="s">{result.aiRagOptimizedSummary}</EuiText>
                        </EuiPanel>
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
                            <EuiFlexItem grow={false}>
                                <EuiIcon type="calendar" size="s" color="subdued" />
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
