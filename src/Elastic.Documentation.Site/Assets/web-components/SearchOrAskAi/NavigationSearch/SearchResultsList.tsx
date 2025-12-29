import { useSelectedIndex, useSearchActions } from '../Search/search.store'
import { useSearchQuery, SearchResultItem } from '../Search/useSearchQuery'
import { SanitizedHtmlContent } from './SanitizedHtmlContent'
import {
    EuiBadge,
    EuiIcon,
    EuiLoadingSpinner,
    useEuiTheme,
    useEuiOverflowScroll,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { forwardRef, useMemo, MutableRefObject, useState, useRef, useEffect } from 'react'

const RESULTS_MAX_HEIGHT = 465
const BREADCRUMB_SEPARATOR = ' / '

export interface SearchResultsListProps {
    itemRefs: MutableRefObject<(HTMLAnchorElement | null)[]>
    isKeyboardNavigating: MutableRefObject<boolean>
    onMouseMove: () => void
}

export const SearchResultsList = ({
    itemRefs,
    isKeyboardNavigating,
    onMouseMove,
}: SearchResultsListProps) => {
    const { euiTheme } = useEuiTheme()
    const selectedIndex = useSelectedIndex()
    const { setSelectedIndex } = useSearchActions()
    const { isLoading, data } = useSearchQuery()
    const keyboardSelectedIndexRef = useRef<number>(-1)

    const results = data?.results ?? []
    const isInitialLoading = isLoading && !data

    // Track keyboard-selected index to maintain it even when mouse hovers
    useEffect(() => {
        if (isKeyboardNavigating.current && selectedIndex >= 0) {
            keyboardSelectedIndexRef.current = selectedIndex
        }
    }, [selectedIndex, isKeyboardNavigating])

    // Maintain keyboard-selected index when mouse hovers (but allow clicks to override)
    const effectiveSelectedIndex = keyboardSelectedIndexRef.current >= 0 && !isKeyboardNavigating.current
        ? keyboardSelectedIndexRef.current
        : selectedIndex

    const containerStyles = css`
        max-height: ${RESULTS_MAX_HEIGHT}px;
        overflow-y: auto;
        overflow-x: hidden;
        border-top-left-radius: ${euiTheme.size.s};
        border-top-right-radius: ${euiTheme.size.s};
        ${useEuiOverflowScroll('y', false)}
    `

    const emptyStateStyles = css`
        padding: ${euiTheme.size.xl} ${euiTheme.size.xl} ${euiTheme.size.l} ${euiTheme.size.xl};
        text-align: center;
        color: ${euiTheme.colors.textDisabled};
        font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
        line-height: ${euiTheme.base * 1.25}px;
    `

    if (isInitialLoading) {
        return (
            <div css={containerStyles}>
                <div css={emptyStateStyles}>
                    <EuiLoadingSpinner size="xl" />
                </div>
            </div>
        )
    }

    if (results.length === 0) {
        return (
            <div css={containerStyles}>
                <div css={emptyStateStyles}>We couldn't find a page that matches your search</div>
            </div>
        )
    }

    const handleMouseEnter = (index: number) => {
        // Only change selection with mouse if there's no keyboard-selected index to preserve
        if (keyboardSelectedIndexRef.current < 0) {
            setSelectedIndex(index)
        }
        // If there's a keyboard selection, keep it (don't change selectedIndex)
    }

    const handleItemMouseMove = (index: number) => {
        if (isKeyboardNavigating.current) {
            // When keyboard navigating, disable keyboard mode but keep the keyboard-selected item highlighted
            onMouseMove()
            // Don't change selectedIndex - keep the keyboard-selected item
        } else if (keyboardSelectedIndexRef.current < 0) {
            // When not keyboard navigating and no keyboard selection to preserve, allow mouse to select items
            setSelectedIndex(index)
        }
        // If keyboardSelectedIndexRef.current >= 0, we preserve the keyboard selection
    }

    const handleClick = () => {
        // When user clicks, reset the keyboard selection ref to allow mouse selection
        keyboardSelectedIndexRef.current = -1
    }

    return (
        <div css={containerStyles}>
            {results.map((result, index) => {
                const isKeyboardSelected = keyboardSelectedIndexRef.current >= 0 && index === keyboardSelectedIndexRef.current
                return (
                    <SearchResultRow
                        key={result.url}
                        result={result}
                        index={index}
                        isSelected={index === effectiveSelectedIndex}
                        isKeyboardSelected={isKeyboardSelected}
                        isKeyboardNavigating={isKeyboardNavigating.current}
                        onMouseEnter={() => handleMouseEnter(index)}
                        onMouseMove={() => handleItemMouseMove(index)}
                        onClick={handleClick}
                        ref={(el) => {
                            itemRefs.current[index] = el
                        }}
                    />
                )
            })}
        </div>
    )
}

interface SearchResultRowProps {
    result: SearchResultItem
    index: number
    isSelected: boolean
    isKeyboardSelected: boolean
    isKeyboardNavigating: boolean
    onMouseEnter: () => void
    onMouseMove: () => void
    onClick: () => void
}

const SearchResultRow = forwardRef<HTMLAnchorElement, SearchResultRowProps>(
    ({ result, index, isSelected, isKeyboardSelected, isKeyboardNavigating, onMouseEnter, onMouseMove, onClick }, ref) => {
        const { euiTheme } = useEuiTheme()
        const [isHovered, setIsHovered] = useState(false)

        const breadcrumbItems = useMemo(() => {
            const typePrefix = result.type === 'api' ? 'API' : 'Docs'
            return [typePrefix, ...result.parents.slice(1).map((p) => p.title)]
        }, [result.type, result.parents])
        
        // Show "Jump to" if element is selected AND:
        // - Currently keyboard navigating, OR
        // - It's the first element, OR  
        // - It's the keyboard-selected element
        // Always show if it's keyboard-selected or keyboard navigating (even when hovering)
        // For other selected elements, only show when not hovering
        const shouldShowJumpTo = isSelected && (
            isKeyboardNavigating || 
            isKeyboardSelected || 
            (index === 0 && !isHovered)
        )

        return (
            <a
                ref={ref}
                href={result.url}
                onMouseEnter={(e) => {
                    setIsHovered(true)
                    onMouseEnter()
                }}
                onMouseLeave={() => setIsHovered(false)}
                onMouseMove={onMouseMove}
                onClick={onClick}
                data-selected={isSelected ? 'true' : 'false'}
                css={css`
                    display: flex;
                    align-items: center;
                    gap: ${euiTheme.size.s};
                    padding-inline: ${euiTheme.size.l};
                    padding-block: ${euiTheme.size.base};
                    text-decoration: none;
                    cursor: pointer;
                    border-bottom: 1px solid
                        ${euiTheme.colors.borderBaseSubdued};
                    background: ${isSelected
                        ? euiTheme.colors.backgroundBaseSubdued
                        : 'transparent'};

                    &:last-child {
                        border-bottom: none;
                    }

                    &:hover {
                        background: ${isSelected
                            ? euiTheme.colors.backgroundBaseSubdued
                            : euiTheme.colors.backgroundBaseHighlighted};
                        
                        .title-text {
                            color: ${euiTheme.colors.link};
                            text-decoration: underline;

                            mark {
                                color: ${euiTheme.colors.textParagraph};
                                text-decoration: underline;
                            }
                        }
                    }

                    &[data-selected='true'] {
                        .title-text {
                            color: ${euiTheme.colors.link};
                            text-decoration: underline;

                            mark {
                                color: ${euiTheme.colors.textParagraph};
                                text-decoration: underline;
                            }
                        }
                    }
                `}
            >
                <div
                    css={css`
                        flex: 1;
                        min-width: 0;
                        overflow: hidden;
                    `}
                >
                    <Breadcrumb items={breadcrumbItems} />
                    <Title text={result.title} />
                    {result.description && (
                        <Description text={result.description} />
                    )}
                </div>
                <JumpToIndicator isVisible={shouldShowJumpTo} />
            </a>
        )
    }
)

SearchResultRow.displayName = 'SearchResultRow'

/**
 * Breadcrumb with 3-part layout:
 * - First: always visible
 * - Middle: truncates with ellipsis
 * - Last: always visible
 */
const Breadcrumb = ({ items }: { items: string[] }) => {
    const { euiTheme } = useEuiTheme()

    if (items.length === 0) return null

    const baseStyles = css`
        font-size: ${euiTheme.size.m};
        line-height: ${euiTheme.size.base};
        color: ${euiTheme.colors.textDisabled};
        margin-bottom: 0;
    `

    if (items.length === 1) {
        return <div css={baseStyles}>{items[0]}</div>
    }

    const first = items[0]
    const middle = items.slice(1, -1)
    const last = items[items.length - 1]

    return (
        <div
            css={css`
                ${baseStyles}
                display: flex;
                white-space: nowrap;
                overflow: hidden;
            `}
        >
            <span
                css={css`
                    flex-shrink: 0;
                `}
            >
                {first}
            </span>

            {middle.length > 0 && (
                <span
                    css={css`
                        flex-shrink: 1;
                        min-width: 0;
                        overflow: hidden;
                        text-overflow: ellipsis;
                        margin-left: 0.5ch;
                    `}
                >
                    {BREADCRUMB_SEPARATOR}
                    {middle.join(BREADCRUMB_SEPARATOR)}
                </span>
            )}

            <span
                css={css`
                    flex-shrink: 0;
                    margin-left: 0.5ch;
                `}
            >
                {BREADCRUMB_SEPARATOR}
                {last}
            </span>
        </div>
    )
}

const Title = ({ text }: { text: string }) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            className="title-text"
            css={css`
                font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                line-height: ${euiTheme.size.l};
                font-weight: ${euiTheme.font.weight.bold};
                color: ${euiTheme.colors.textParagraph};
                margin-bottom: calc(${euiTheme.size.xs} / 2);

                mark {
                    background-color: ${euiTheme.colors.backgroundLightPrimary};
                    font-weight: ${euiTheme.font.weight.bold};
                    color: ${euiTheme.colors.textParagraph};
                }
            `}
        >
            <SanitizedHtmlContent htmlContent={text} />
        </div>
    )
}

const Description = ({ text }: { text: string }) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                font-size: ${euiTheme.size.m};
                line-height: ${euiTheme.size.base};
                color: ${euiTheme.colors.textSubdued};
                white-space: nowrap;
                overflow: hidden;
                text-overflow: ellipsis;

                mark {
                    background-color: ${euiTheme.colors.backgroundLightPrimary};
                    font-weight: ${euiTheme.font.weight.bold};
                }
            `}
        >
            <SanitizedHtmlContent htmlContent={text} ellipsis />
        </div>
    )
}

const JumpToIndicator = ({ isVisible }: { isVisible: boolean }) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                flex-shrink: 0;
                color: ${euiTheme.colors.textSubdued};
                margin-left: ${euiTheme.size.xxxxl};
                visibility: ${isVisible ? 'visible' : 'hidden'};
            `}
        >
            <EuiBadge color="hollow">
                Jump to <EuiIcon type="returnKey" size="s" />
            </EuiBadge>
        </div>
    )
}
