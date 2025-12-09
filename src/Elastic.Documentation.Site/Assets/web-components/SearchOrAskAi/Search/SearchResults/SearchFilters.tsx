import { TypeFilter, useTypeFilter, useSearchActions } from '../search.store'
import { useEuiTheme, EuiButton, EuiSpacer } from '@elastic/eui'
import { css } from '@emotion/react'
import { useCallback, useState, MutableRefObject } from 'react'

const FILTERS: TypeFilter[] = ['all', 'doc', 'api']
const FILTER_LABELS: Record<TypeFilter, string> = {
    all: 'All',
    doc: 'Docs',
    api: 'API',
}
const FILTER_ICONS: Record<TypeFilter, string> = {
    all: 'globe',
    doc: 'documentation',
    api: 'code',
}

interface SearchFiltersProps {
    isLoading: boolean
    filterRefs?: MutableRefObject<(HTMLButtonElement | null)[]>
}

export const SearchFilters = ({
    isLoading,
    filterRefs,
}: SearchFiltersProps) => {
    if (isLoading) {
        return null
    }

    const { euiTheme } = useEuiTheme()
    const selectedFilter = useTypeFilter()
    const { setTypeFilter } = useSearchActions()

    // Track which filter is focused for roving tabindex within the toolbar
    const [focusedIndex, setFocusedIndex] = useState(() =>
        FILTERS.indexOf(selectedFilter)
    )

    // Only the focused filter is tabbable (roving tabindex within toolbar)
    const getTabIndex = (index: number): 0 | -1 => {
        return index === focusedIndex ? 0 : -1
    }

    // Arrow keys navigate within the toolbar
    const handleFilterKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLButtonElement>, index: number) => {
            if (e.key === 'ArrowLeft' && index > 0) {
                e.preventDefault()
                const newIndex = index - 1
                setFocusedIndex(newIndex)
                filterRefs?.current[newIndex]?.focus()
            } else if (e.key === 'ArrowRight' && index < FILTERS.length - 1) {
                e.preventDefault()
                const newIndex = index + 1
                setFocusedIndex(newIndex)
                filterRefs?.current[newIndex]?.focus()
            }
            // Tab naturally exits the toolbar
        },
        [filterRefs]
    )

    const handleFilterClick = useCallback(
        (filter: TypeFilter, index: number) => {
            setTypeFilter(filter)
            setFocusedIndex(index)
        },
        [setTypeFilter]
    )

    const handleFilterFocus = useCallback((index: number) => {
        setFocusedIndex(index)
    }, [])

    const getButtonStyle = (isSelected: boolean) => css`
        border-radius: 99999px;
        padding-inline: ${euiTheme.size.s};
        min-inline-size: auto;
        ${isSelected &&
        `
            background-color: ${euiTheme.colors.backgroundBaseHighlighted};
            border-color: ${euiTheme.colors.borderStrongPrimary};
            color: ${euiTheme.colors.textPrimary};
            border-width: 1px;
            border-style: solid;
        `}
        ${isSelected &&
        `
            span svg {
                fill: ${euiTheme.colors.textPrimary};
            }
        `}
        &:hover,
        &:hover:not(:disabled)::before {
            background-color: ${euiTheme.colors.backgroundBaseHighlighted};
        }
        &:focus-visible {
            background-color: ${euiTheme.colors.backgroundBasePlain};
        }
        ${isSelected &&
        `
            &:hover,
            &:focus-visible {
            background-color: ${euiTheme.colors.backgroundBaseHighlighted};
            border-color: ${euiTheme.colors.borderStrongPrimary};
            color: ${euiTheme.colors.textPrimary};
        }
        `}
        span {
            gap: 4px;
            &.eui-textTruncate {
                padding-inline: 4px;
            }
            svg {
                fill: ${isSelected
                    ? euiTheme.colors.textPrimary
                    : euiTheme.colors.borderBaseProminent};
            }
        }
    `

    return (
        <div>
            <div
                css={css`
                    display: flex;
                    gap: ${euiTheme.size.s};
                    padding-inline: ${euiTheme.size.base};
                `}
                role="toolbar"
                aria-label="Search filters"
            >
                {FILTERS.map((filter, index) => {
                    const isSelected = selectedFilter === filter
                    return (
                        <EuiButton
                            key={filter}
                            color="text"
                            iconType={FILTER_ICONS[filter]}
                            iconSize={filter === 'api' ? 's' : 'm'}
                            size="s"
                            onClick={() => handleFilterClick(filter, index)}
                            onFocus={() => handleFilterFocus(index)}
                            onKeyDown={(
                                e: React.KeyboardEvent<HTMLButtonElement>
                            ) => handleFilterKeyDown(e, index)}
                            buttonRef={(el: HTMLButtonElement | null) => {
                                if (filterRefs) {
                                    filterRefs.current[index] = el
                                }
                            }}
                            tabIndex={getTabIndex(index)}
                            css={getButtonStyle(isSelected)}
                            aria-label={
                                filter === 'all'
                                    ? 'Show all results'
                                    : filter === 'doc'
                                      ? 'Filter to documentation results'
                                      : 'Filter to API results'
                            }
                            aria-pressed={isSelected}
                        >
                            {FILTER_LABELS[filter]}
                        </EuiButton>
                    )
                })}
            </div>
            <EuiSpacer size="m" />
        </div>
    )
}
