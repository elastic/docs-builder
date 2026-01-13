import {
    EuiAccordion,
    EuiCheckbox,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiRadioGroup,
    EuiText,
    useEuiTheme,
    useGeneratedHtmlId,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useState } from 'react'
import type { SearchAggregations } from './useFullPageSearchQuery'
import type { FullPageSearchFilters } from './fullPageSearch.store'

const VERSION_OPTIONS = [
    { id: '9.0+', label: '9.0+ (current)' },
    { id: '8.19', label: '8.19' },
    { id: '7.17', label: '7.17' },
]

interface VersionFilterProps {
    selected: string
    onChange: (version: string) => void
}

const VersionFilter = ({ selected, onChange }: VersionFilterProps) => {
    const { euiTheme } = useEuiTheme()
    const radioGroupId = useGeneratedHtmlId({ prefix: 'versionFilter' })

    return (
        <EuiAccordion
            id="version-filter"
            buttonContent={
                <EuiFlexGroup alignItems="center" gutterSize="s">
                    <EuiFlexItem grow={false}>
                        <EuiIcon type="package" size="s" color="subdued" />
                    </EuiFlexItem>
                    <EuiFlexItem>
                        <EuiText size="s">
                            <strong>Version</strong>
                        </EuiText>
                    </EuiFlexItem>
                </EuiFlexGroup>
            }
            initialIsOpen={true}
            paddingSize="none"
            css={css`
                margin-bottom: ${euiTheme.size.l};
            `}
        >
            <div
                css={css`
                    padding-top: ${euiTheme.size.s};
                `}
            >
                <EuiRadioGroup
                    options={VERSION_OPTIONS}
                    idSelected={selected}
                    onChange={(id) => onChange(id)}
                    name={radioGroupId}
                />
            </div>
        </EuiAccordion>
    )
}

interface FacetFilterProps {
    title: string
    icon: string
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
    const [showAll, setShowAll] = useState(false)
    const accordionId = useGeneratedHtmlId({ prefix: title.toLowerCase() })

    const sortedItems = Object.entries(items).sort(([, a], [, b]) => b - a)
    const visibleItems = showAll ? sortedItems : sortedItems.slice(0, 5)

    if (sortedItems.length === 0) return null

    return (
        <EuiAccordion
            id={accordionId}
            buttonContent={
                <EuiFlexGroup alignItems="center" gutterSize="s">
                    <EuiFlexItem grow={false}>
                        <EuiIcon type={icon} size="s" color="subdued" />
                    </EuiFlexItem>
                    <EuiFlexItem>
                        <EuiText size="s">
                            <strong>{title}</strong>
                        </EuiText>
                    </EuiFlexItem>
                </EuiFlexGroup>
            }
            initialIsOpen={true}
            paddingSize="none"
            css={css`
                margin-bottom: ${euiTheme.size.l};
            `}
        >
            <div
                css={css`
                    padding-top: ${euiTheme.size.s};
                `}
            >
                {visibleItems.map(([key, count]) => (
                    <EuiFlexGroup
                        key={key}
                        alignItems="center"
                        gutterSize="s"
                        css={css`
                            padding: ${euiTheme.size.xs} 0;
                        `}
                    >
                        <EuiFlexItem grow={false}>
                            <EuiCheckbox
                                id={`${accordionId}-${key}`}
                                checked={selected.includes(key)}
                                onChange={() => onChange(key)}
                                label=""
                            />
                        </EuiFlexItem>
                        <EuiFlexItem>
                            <EuiText
                                size="s"
                                css={css`
                                    text-transform: capitalize;
                                `}
                            >
                                {key}
                            </EuiText>
                        </EuiFlexItem>
                        <EuiFlexItem grow={false}>
                            <EuiText size="xs" color="subdued">
                                {count.toLocaleString()}
                            </EuiText>
                        </EuiFlexItem>
                    </EuiFlexGroup>
                ))}
                {sortedItems.length > 5 && (
                    <button
                        onClick={() => setShowAll(!showAll)}
                        css={css`
                            background: none;
                            border: none;
                            color: ${euiTheme.colors.primary};
                            cursor: pointer;
                            font-size: ${euiTheme.size.m};
                            padding: ${euiTheme.size.xs} 0;
                            margin-top: ${euiTheme.size.xs};

                            &:hover {
                                text-decoration: underline;
                            }
                        `}
                    >
                        {showAll
                            ? 'Show less'
                            : `Show ${sortedItems.length - 5} more`}
                    </button>
                )}
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
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                position: sticky;
                top: ${euiTheme.size.l};
            `}
        >
            <VersionFilter selected={version} onChange={onVersionChange} />

            <FacetFilter
                title="Content Type"
                icon="tag"
                items={aggregations?.type ?? {}}
                selected={filters.type}
                onChange={(v) => onFilterChange('type', v)}
            />

            <FacetFilter
                title="Product"
                icon="layers"
                items={aggregations?.navigationSection ?? {}}
                selected={filters.navigationSection}
                onChange={(v) => onFilterChange('navigationSection', v)}
            />

            <FacetFilter
                title="Deployment"
                icon="bolt"
                items={aggregations?.deploymentType ?? {}}
                selected={filters.deploymentType}
                onChange={(v) => onFilterChange('deploymentType', v)}
            />
        </div>
    )
}
