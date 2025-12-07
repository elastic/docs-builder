import { useTypeFilter, useSearchActions } from '../search.store'
import { useEuiTheme, EuiButton, EuiSpacer } from '@elastic/eui'
import { css } from '@emotion/react'
import { useRef, useCallback, MutableRefObject } from 'react'

interface SearchFiltersProps {
    isLoading: boolean
    inputRef?: React.RefObject<HTMLInputElement>
    itemRefs?: MutableRefObject<(HTMLAnchorElement | null)[]>
    resultsCount?: number
}

export const SearchFilters = ({
    isLoading,
    inputRef,
    itemRefs,
    resultsCount = 0,
}: SearchFiltersProps) => {
    if (isLoading) {
        return null
    }

    const { euiTheme } = useEuiTheme()
    const selectedFilter = useTypeFilter()
    const { setTypeFilter } = useSearchActions()

    const filterRefs = useRef<(HTMLButtonElement | null)[]>([])

    const handleFilterKeyDown = useCallback(
        (e: React.KeyboardEvent<HTMLButtonElement>, filterIndex: number) => {
            const filterCount = 3 // ALL, DOCS, API

            if (e.key === 'ArrowUp') {
                e.preventDefault()
                // Go back to input
                inputRef?.current?.focus()
            } else if (e.key === 'ArrowDown') {
                e.preventDefault()
                // Go to first result if available
                if (resultsCount > 0) {
                    itemRefs?.current[0]?.focus()
                }
            } else if (e.key === 'ArrowLeft') {
                e.preventDefault()
                if (filterIndex > 0) {
                    filterRefs.current[filterIndex - 1]?.focus()
                }
            } else if (e.key === 'ArrowRight') {
                e.preventDefault()
                if (filterIndex < filterCount - 1) {
                    filterRefs.current[filterIndex + 1]?.focus()
                }
            }
        },
        [inputRef, itemRefs, resultsCount]
    )

    const buttonStyle = css`
        border-radius: 99999px;
        padding-inline: ${euiTheme.size.s};
        min-inline-size: auto;
        &[aria-pressed='true'] {
            background-color: ${euiTheme.colors.backgroundBaseHighlighted};
            border-color: ${euiTheme.colors.borderStrongPrimary};
            color: ${euiTheme.colors.textPrimary};
            border-width: 1px;
            border-style: solid;
            span svg {
                fill: ${euiTheme.colors.textPrimary};
            }
        }
        &:hover,
        &:focus-visible,
        &:hover:not(:disabled)::before {
            background-color: ${euiTheme.colors.backgroundBaseHighlighted};
        }
        &[aria-pressed='true']:hover,
        &[aria-pressed='true']:focus-visible {
            background-color: ${euiTheme.colors.backgroundBaseHighlighted};
            border-color: ${euiTheme.colors.borderStrongPrimary};
            color: ${euiTheme.colors.textPrimary};
        }
        span {
            gap: 4px;
            &.eui-textTruncate {
                padding-inline: 4px;
            }
            svg {
                fill: ${euiTheme.colors.borderBaseProminent};
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
                role="group"
                aria-label="Search filters"
            >

                <EuiButton
                    color="text"
                    iconType="globe"
                    iconSize="m"
                    size="s"
                    onClick={() => setTypeFilter('all')}
                    onKeyDown={(e: React.KeyboardEvent<HTMLButtonElement>) =>
                        handleFilterKeyDown(e, 0)
                    }
                    buttonRef={(el: HTMLButtonElement | null) => {
                        filterRefs.current[0] = el
                    }}
                    css={buttonStyle}
                    aria-label={`Show all results`}
                    aria-pressed={selectedFilter === 'all'}
                >
                    {`All`}
                </EuiButton>
                <EuiButton
                    color="text"
                    iconType="documentation"
                    iconSize="m"
                    size="s"
                    onClick={() => setTypeFilter('doc')}
                    onKeyDown={(e: React.KeyboardEvent<HTMLButtonElement>) =>
                        handleFilterKeyDown(e, 1)
                    }
                    buttonRef={(el: HTMLButtonElement | null) => {
                        filterRefs.current[1] = el
                    }}
                    css={buttonStyle}
                    aria-label={`Filter to documentation results`}
                    aria-pressed={selectedFilter === 'doc'}
                >
                    {`Docs`}
                </EuiButton>
                <EuiButton
                    color="text"
                    iconType="code"
                    iconSize="s"
                    size="s"
                    onClick={() => setTypeFilter('api')}
                    onKeyDown={(e: React.KeyboardEvent<HTMLButtonElement>) =>
                        handleFilterKeyDown(e, 2)
                    }
                    buttonRef={(el: HTMLButtonElement | null) => {
                        filterRefs.current[2] = el
                    }}
                    css={buttonStyle}
                    aria-label={`Filter to API results`}
                    aria-pressed={selectedFilter === 'api'}
                >
                    {`API`}
                </EuiButton>
            </div>
            <EuiSpacer size="m" />
        </div>
    )
}
