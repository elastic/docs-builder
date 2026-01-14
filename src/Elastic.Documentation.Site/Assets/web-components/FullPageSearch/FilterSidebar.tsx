import {
    EuiAccordion,
    EuiBadge,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiText,
    useEuiTheme,
    useGeneratedHtmlId,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { ReactNode } from 'react'
import type { SearchAggregations } from './useFullPageSearchQuery'
import type { FullPageSearchFilters } from './fullPageSearch.store'

// Inline SVG icons
const PackageIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
        <path d="M14.4472,3.72363l-6-3a.99965.99965,0,0,0-.8944,0l-6,3A.99992.99992,0,0,0,1,4.618v6.764a1.00012,1.00012,0,0,0,.5528.89441l6,3a1,1,0,0,0,.8944,0l6-3A1.00012,1.00012,0,0,0,15,11.382V4.618A.99992.99992,0,0,0,14.4472,3.72363ZM5.87085,5.897l5.34357-2.67181L13.372,4.304,8,6.94293ZM8,1.618,10.09637,2.6662,4.74292,5.343,2.628,4.30408ZM2,5.10968,4.25,6.215V9a.5.5,0,0,0,1,0V6.70618L7.5,7.81146V14.132L2,11.382ZM8.5,14.132V7.81146L14,5.10968V11.382Z"/>
    </svg>
)

const DocumentIcon = ({ size = 16 }: { size?: number }) => (
    <svg xmlns="http://www.w3.org/2000/svg" width={size} height={size} viewBox="0 0 16 16" fill="currentColor">
        <path fillRule="evenodd" d="M3 2a1 1 0 0 1 1-1h4.707L13 5.293V14a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V2Zm5 0H4v12h8V6H9a1 1 0 0 1-1-1V2Zm1 .707L11.293 5H9V2.707Z" clipRule="evenodd"/>
    </svg>
)

const ConsoleIcon = ({ size = 16 }: { size?: number }) => (
    <svg xmlns="http://www.w3.org/2000/svg" width={size} height={size} viewBox="0 0 16 16" fill="currentColor">
        <path d="M1.157 12.224 5.768 8.32a.404.404 0 0 0 0-.64l-4.61-3.904a.407.407 0 0 1 0-.643.608.608 0 0 1 .759 0l4.61 3.904c.631.534.63 1.393 0 1.926l-4.61 3.904a.608.608 0 0 1-.76 0 .407.407 0 0 1 0-.643ZM9 12h6v1H9z"/>
    </svg>
)

const LayersIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
        <path fillRule="evenodd" d="M1.553 4.106a1 1 0 0 0 0 1.788l6 3a1 1 0 0 0 .894 0l6-3a1 1 0 0 0 0-1.788l-6-3a1 1 0 0 0-.894 0l-6 3ZM14 5 8 8 2 5l6-3 6 3Z" clipRule="evenodd"/>
        <path d="m8 11 6.894-3.447S15 7.843 15 8a1 1 0 0 1-.553.895l-6 3a1 1 0 0 1-.894 0l-6-3A1 1 0 0 1 1 8c0-.158.106-.447.106-.447L8 11Z"/>
        <path d="m8 14 6.894-3.447s.106.29.106.447a1 1 0 0 1-.553.895l-6 3a1 1 0 0 1-.894 0l-6-3A1 1 0 0 1 1 11c0-.158.106-.447.106-.447L8 14Z"/>
    </svg>
)

const CloudIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
        <path d="M10.7458178,5.00539515 C13.6693111,5.13397889 16,7.54480866 16,10.5 C16,13.5375661 13.5375661,16 10.5,16 L4,16 C1.790861,16 0,14.209139 0,12 C0,10.3632968 0.983004846,8.95618668 2.39082809,8.33685609 C2.20209747,7.91526722 2.07902056,7.46524082 2.02752078,7 L0.5,7 C0.223857625,7 0,6.77614237 0,6.5 C0,6.22385763 0.223857625,6 0.5,6 L2.02746439,6 C2.1234088,5.13207349 2.46618907,4.33854089 2.98405031,3.69115709 L1.64644661,2.35355339 C1.45118446,2.15829124 1.45118446,1.84170876 1.64644661,1.64644661 C1.84170876,1.45118446 2.15829124,1.45118446 2.35355339,1.64644661 L3.69115709,2.98405031 C4.33854089,2.46618907 5.13207349,2.1234088 6,2.02746439 L6,0.5 C6,0.223857625 6.22385763,8.8817842e-16 6.5,8.8817842e-16 C6.77614237,8.8817842e-16 7,0.223857625 7,0.5 L7,2.02763291 C7.86199316,2.12340881 8.65776738,2.46398198 9.30889273,2.98400049 L10.6464466,1.64644661 C10.8417088,1.45118446 11.1582912,1.45118446 11.3535534,1.64644661 C11.5488155,1.84170876 11.5488155,2.15829124 11.3535534,2.35355339 L10.0163882,3.69071859 C10.3273281,4.07929253 10.5759739,4.52210317 10.7458178,5.00539515 Z M4,15 L10.5,15 C12.9852814,15 15,12.9852814 15,10.5 C15,8.01471863 12.9852814,6 10.5,6 C8.66116238,6 7.03779702,7.11297909 6.34791295,8.76123609 C7.34903712,9.48824913 8,10.6681043 8,12 C8,12.2761424 7.77614237,12.5 7.5,12.5 C7.22385763,12.5 7,12.2761424 7,12 C7,10.3431458 5.65685425,9 4,9 C2.34314575,9 1,10.3431458 1,12 C1,13.6568542 2.34314575,15 4,15 Z M9.69118222,5.05939327 C9.13621169,3.82983527 7.90059172,3 6.5,3 C4.56700338,3 3,4.56700338 3,6.5 C3,7.04681795 3.12529266,7.57427865 3.36125461,8.05072309 C3.56924679,8.01734375 3.78259797,8 4,8 C4.51800602,8 5.01301412,8.0984658 5.46732834,8.27770144 C6.22579284,6.55947236 7.82085903,5.33623457 9.69118222,5.05939327 Z"/>
    </svg>
)

const VERSION_OPTIONS = [
    { id: '9.0+', label: '9.0+', current: true },
    { id: '8.19', label: '8.19', current: false },
    { id: '7.17', label: '7.17', current: false },
]

// Type icons matching ResultCard
const TYPE_ICONS: Record<string, ReactNode> = {
    doc: <DocumentIcon />,
    api: <ConsoleIcon />,
}

// Label mappings for facet values
const TYPE_LABELS: Record<string, string> = {
    doc: 'Documentation',
    api: 'OpenAPI Reference',
}

interface VersionFilterProps {
    selected: string
    onChange: (version: string) => void
}

const VersionFilter = ({ selected, onChange }: VersionFilterProps) => {
    const { euiTheme } = useEuiTheme()
    const accordionId = useGeneratedHtmlId({ prefix: 'version' })

    return (
        <EuiAccordion
            id={accordionId}
            buttonContent={
                <EuiFlexGroup alignItems="center" gutterSize="s">
                    <EuiFlexItem grow={false} css={css`color: ${euiTheme.colors.subduedText};`}>
                        <PackageIcon />
                    </EuiFlexItem>
                    <EuiFlexItem>
                        <EuiText size="s">
                            <strong>Version</strong>
                        </EuiText>
                    </EuiFlexItem>
                </EuiFlexGroup>
            }
            forceState="open"
            arrowDisplay="none"
            paddingSize="none"
            css={css`
                margin-bottom: ${euiTheme.size.l};

                /* Override EuiAccordion's inline block-size: 0px which breaks async content */
                .euiAccordion__childWrapper {
                    block-size: auto !important;
                    height: auto !important;
                }
            `}
        >
            <div
                css={css`
                    display: flex;
                    flex-direction: column;
                    border: 1px solid ${euiTheme.border.color};
                    border-radius: ${euiTheme.border.radius.medium};
                    overflow: hidden;
                    margin-top: ${euiTheme.size.s};
                `}
            >
                {VERSION_OPTIONS.map((option, idx) => {
                    const isSelected = selected === option.id
                    return (
                        <button
                            key={option.id}
                            onClick={() => onChange(option.id)}
                            css={css`
                                display: flex;
                                align-items: center;
                                justify-content: space-between;
                                padding: ${euiTheme.size.s} ${euiTheme.size.m};
                                background: ${isSelected ? `${euiTheme.colors.primary}1A` : euiTheme.colors.emptyShade};
                                color: ${isSelected ? euiTheme.colors.primary : euiTheme.colors.text};
                                border: none;
                                border-bottom: ${idx < VERSION_OPTIONS.length - 1 ? `1px solid ${euiTheme.border.color}` : 'none'};
                                cursor: pointer;
                                font-size: 14px;
                                font-weight: ${isSelected ? euiTheme.font.weight.semiBold : euiTheme.font.weight.regular};
                                transition: background 0.15s ease;
                                text-align: left;

                                &:hover {
                                    background: ${isSelected ? `${euiTheme.colors.primary}26` : euiTheme.colors.lightestShade};
                                }
                            `}
                        >
                            <span>{option.label}</span>
                            {option.current && (
                                <EuiBadge color="warning">current</EuiBadge>
                            )}
                        </button>
                    )
                })}
            </div>
        </EuiAccordion>
    )
}

interface TypeFilterProps {
    items: Record<string, number>
    selected: string[]
    onChange: (value: string) => void
}

const TypeFilter = ({ items, selected, onChange }: TypeFilterProps) => {
    const { euiTheme } = useEuiTheme()
    const accordionId = useGeneratedHtmlId({ prefix: 'type' })

    const sortedItems = Object.entries(items).sort(([, a], [, b]) => b - a)

    if (sortedItems.length === 0) return null

    return (
        <EuiAccordion
            id={accordionId}
            buttonContent={
                <EuiFlexGroup alignItems="center" gutterSize="s">
                    <EuiFlexItem grow={false} css={css`color: ${euiTheme.colors.subduedText};`}>
                        <DocumentIcon />
                    </EuiFlexItem>
                    <EuiFlexItem>
                        <EuiText size="s">
                            <strong>Type</strong>
                        </EuiText>
                    </EuiFlexItem>
                </EuiFlexGroup>
            }
            forceState="open"
            arrowDisplay="none"
            paddingSize="none"
            css={css`
                margin-bottom: ${euiTheme.size.l};

                /* Override EuiAccordion's inline block-size: 0px which breaks async content */
                .euiAccordion__childWrapper {
                    block-size: auto !important;
                    height: auto !important;
                }
            `}
        >
            <div
                css={css`
                    padding-top: ${euiTheme.size.s};
                `}
            >
                {sortedItems.map(([key, count]) => {
                    const isSelected = selected.includes(key)
                    return (
                        <button
                            key={key}
                            onClick={() => onChange(key)}
                            css={css`
                                display: flex;
                                align-items: center;
                                gap: ${euiTheme.size.s};
                                width: 100%;
                                padding: ${euiTheme.size.s} ${euiTheme.size.s};
                                background: ${isSelected ? `${euiTheme.colors.primary}1A` : 'transparent'};
                                border: none;
                                border-radius: ${euiTheme.border.radius.small};
                                cursor: pointer;
                                text-align: left;
                                transition: background 0.15s ease;

                                &:hover {
                                    background: ${isSelected ? `${euiTheme.colors.primary}26` : euiTheme.colors.lightestShade};
                                }
                            `}
                        >
                            <span css={css`color: ${isSelected ? euiTheme.colors.primary : euiTheme.colors.subduedText}; display: flex;`}>
                                {TYPE_ICONS[key] ?? <DocumentIcon />}
                            </span>
                            <EuiText
                                size="s"
                                css={css`
                                    flex: 1;
                                    font-weight: ${isSelected ? euiTheme.font.weight.semiBold : euiTheme.font.weight.regular};
                                    color: ${isSelected ? euiTheme.colors.primary : 'inherit'};
                                `}
                            >
                                {TYPE_LABELS[key] ?? key}
                            </EuiText>
                            {isSelected ? (
                                <EuiIcon type="cross" size="s" color="primary" />
                            ) : (
                                <EuiText size="xs" color="subdued">
                                    {count.toLocaleString()}
                                </EuiText>
                            )}
                        </button>
                    )
                })}
            </div>
        </EuiAccordion>
    )
}

interface FacetFilterProps {
    title: string
    icon: ReactNode
    items: Record<string, number>
    selected: string[]
    onChange: (value: string) => void
}

const FacetFilter = ({
    title,
    icon,
    items,
    selected,
    onChange,
}: FacetFilterProps) => {
    const { euiTheme } = useEuiTheme()
    const accordionId = useGeneratedHtmlId({ prefix: title.toLowerCase() })

    const sortedItems = Object.entries(items).sort(([, a], [, b]) => b - a)

    if (sortedItems.length === 0) return null

    return (
        <EuiAccordion
            id={accordionId}
            buttonContent={
                <EuiFlexGroup alignItems="center" gutterSize="s">
                    <EuiFlexItem grow={false} css={css`color: ${euiTheme.colors.subduedText};`}>
                        {icon}
                    </EuiFlexItem>
                    <EuiFlexItem>
                        <EuiText size="s">
                            <strong>{title}</strong>
                        </EuiText>
                    </EuiFlexItem>
                </EuiFlexGroup>
            }
            forceState="open"
            paddingSize="none"
            css={css`
                margin-bottom: ${euiTheme.size.l};

                /* Override EuiAccordion's inline block-size: 0px which breaks async content */
                .euiAccordion__childWrapper {
                    block-size: auto !important;
                    height: auto !important;
                }
            `}
        >
            <div
                css={css`
                    padding-top: ${euiTheme.size.s};
                `}
            >
                {sortedItems.map(([key, count]) => {
                    const isSelected = selected.includes(key)
                    return (
                        <button
                            key={key}
                            onClick={() => onChange(key)}
                            css={css`
                                display: flex;
                                align-items: center;
                                gap: ${euiTheme.size.s};
                                width: 100%;
                                padding: ${euiTheme.size.s} ${euiTheme.size.s};
                                background: ${isSelected ? `${euiTheme.colors.primary}1A` : 'transparent'};
                                border: none;
                                border-radius: ${euiTheme.border.radius.small};
                                cursor: pointer;
                                text-align: left;
                                transition: background 0.15s ease;

                                &:hover {
                                    background: ${isSelected ? `${euiTheme.colors.primary}26` : euiTheme.colors.lightestShade};
                                }
                            `}
                        >
                            <EuiText
                                size="s"
                                css={css`
                                    flex: 1;
                                    text-transform: capitalize;
                                    font-weight: ${isSelected ? euiTheme.font.weight.semiBold : euiTheme.font.weight.regular};
                                    color: ${isSelected ? euiTheme.colors.primary : 'inherit'};
                                `}
                            >
                                {key}
                            </EuiText>
                            {isSelected ? (
                                <EuiIcon type="cross" size="s" color="primary" />
                            ) : (
                                <EuiText size="xs" color="subdued">
                                    {count.toLocaleString()}
                                </EuiText>
                            )}
                        </button>
                    )
                })}
            </div>
        </EuiAccordion>
    )
}

interface FilterSidebarProps {
    aggregations?: SearchAggregations
    version: string
    filters: FullPageSearchFilters
    onVersionChange: (version: string) => void
    onFilterChange: (key: keyof FullPageSearchFilters, value: string) => void
}

export const FilterSidebar = ({
    aggregations,
    version,
    filters,
    onVersionChange,
    onFilterChange,
}: FilterSidebarProps) => {
    return (
        <>
            <VersionFilter selected={version} onChange={onVersionChange} />

            <TypeFilter
                items={aggregations?.type ?? {}}
                selected={filters.type}
                onChange={(v) => onFilterChange('type', v)}
            />

            <FacetFilter
                title="Section"
                icon={<LayersIcon />}
                items={aggregations?.navigationSection ?? {}}
                selected={filters.navigationSection}
                onChange={(v) => onFilterChange('navigationSection', v)}
            />

            <FacetFilter
                title="Deployment"
                icon={<CloudIcon />}
                items={aggregations?.deploymentType ?? {}}
                selected={filters.deploymentType}
                onChange={(v) => onFilterChange('deploymentType', v)}
            />
        </>
    )
}

// Export icons for use in ResultCard
export { DocumentIcon, ConsoleIcon }
