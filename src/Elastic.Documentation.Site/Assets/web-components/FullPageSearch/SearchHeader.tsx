import {
    EuiButton,
    EuiFieldSearch,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useRef, useState, KeyboardEvent } from 'react'

interface RecentSearchesDropdownProps {
    searches: string[]
    onSelect: (query: string) => void
    onClear: () => void
    visible: boolean
}

const RecentSearchesDropdown = ({
    searches,
    onSelect,
    onClear,
    visible,
}: RecentSearchesDropdownProps) => {
    const { euiTheme } = useEuiTheme()

    if (!visible || searches.length === 0) return null

    return (
        <div
            css={css`
                position: absolute;
                top: 100%;
                left: 0;
                right: 0;
                margin-top: ${euiTheme.size.xs};
                background: ${euiTheme.colors.emptyShade};
                border: 1px solid ${euiTheme.border.color};
                border-radius: ${euiTheme.border.radius.medium};
                box-shadow: ${euiTheme.levels.menu};
                z-index: 1000;
                overflow: hidden;
            `}
        >
            <EuiFlexGroup
                justifyContent="spaceBetween"
                alignItems="center"
                css={css`
                    padding: ${euiTheme.size.s} ${euiTheme.size.m};
                    border-bottom: 1px solid ${euiTheme.border.color};
                `}
            >
                <EuiFlexItem grow={false}>
                    <EuiText size="xs" color="subdued">
                        Recent searches
                    </EuiText>
                </EuiFlexItem>
                <EuiFlexItem grow={false}>
                    <button
                        onClick={onClear}
                        css={css`
                            background: none;
                            border: none;
                            color: ${euiTheme.colors.subduedText};
                            font-size: ${euiTheme.size.m};
                            cursor: pointer;

                            &:hover {
                                color: ${euiTheme.colors.text};
                            }
                        `}
                    >
                        Clear
                    </button>
                </EuiFlexItem>
            </EuiFlexGroup>
            <div
                css={css`
                    padding: ${euiTheme.size.xs};
                `}
            >
                {searches.slice(0, 5).map((search, idx) => (
                    <button
                        key={idx}
                        onClick={() => onSelect(search)}
                        css={css`
                            display: flex;
                            align-items: center;
                            gap: ${euiTheme.size.s};
                            width: 100%;
                            padding: ${euiTheme.size.s} ${euiTheme.size.m};
                            background: none;
                            border: none;
                            border-radius: ${euiTheme.border.radius.small};
                            text-align: left;
                            cursor: pointer;

                            &:hover {
                                background: ${euiTheme.colors.lightestShade};
                            }
                        `}
                    >
                        <EuiIcon type="clock" size="s" color="subdued" />
                        <EuiText size="s">{search}</EuiText>
                    </button>
                ))}
            </div>
        </div>
    )
}

interface SearchHeaderProps {
    query: string
    recentSearches: string[]
    onQueryChange: (query: string) => void
    onSearch: (query: string) => void
    onClearRecent: () => void
}

export const SearchHeader = ({
    query,
    recentSearches,
    onQueryChange,
    onSearch,
    onClearRecent,
}: SearchHeaderProps) => {
    const { euiTheme } = useEuiTheme()
    const inputRef = useRef<HTMLInputElement>(null)
    const [showRecent, setShowRecent] = useState(false)

    const handleKeyDown = (e: KeyboardEvent) => {
        if (e.key === 'Enter') {
            onSearch(query)
            setShowRecent(false)
        } else if (e.key === 'Escape') {
            setShowRecent(false)
        }
    }

    const handleFocus = () => {
        if (!query) {
            setShowRecent(true)
        }
    }

    const handleBlur = () => {
        // Delay to allow click on dropdown items
        setTimeout(() => setShowRecent(false), 200)
    }

    const handleSelectRecent = (search: string) => {
        onQueryChange(search)
        onSearch(search)
        setShowRecent(false)
    }

    return (
        <header
            css={css`
                position: sticky;
                top: 0;
                z-index: 100;
                background: ${euiTheme.colors.emptyShade};
                border-bottom: 1px solid ${euiTheme.border.color};
                padding: ${euiTheme.size.m} ${euiTheme.size.l};
            `}
        >
            <EuiFlexGroup
                alignItems="center"
                gutterSize="m"
                css={css`
                    max-width: var(--max-layout-width, 1400px);
                    margin: 0 auto;
                `}
            >
                <EuiFlexItem grow={false}>
                    <EuiFlexGroup alignItems="center" gutterSize="s">
                        <EuiFlexItem grow={false}>
                            <div
                                css={css`
                                    width: 32px;
                                    height: 32px;
                                    background: linear-gradient(
                                        135deg,
                                        ${euiTheme.colors.primary} 0%,
                                        ${euiTheme.colors.accent} 100%
                                    );
                                    border-radius: ${euiTheme.border.radius
                                        .medium};
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                `}
                            >
                                <EuiIcon
                                    type="documentation"
                                    color="ghost"
                                    size="m"
                                />
                            </div>
                        </EuiFlexItem>
                        <EuiFlexItem grow={false}>
                            <EuiText
                                css={css`
                                    font-weight: ${euiTheme.font.weight
                                        .semiBold};
                                    display: none;
                                    @media (min-width: 600px) {
                                        display: block;
                                    }
                                `}
                            >
                                Elastic Docs
                            </EuiText>
                        </EuiFlexItem>
                    </EuiFlexGroup>
                </EuiFlexItem>
                <EuiFlexItem
                    css={css`
                        max-width: 700px;
                        position: relative;
                    `}
                >
                    <EuiFieldSearch
                        ref={inputRef as never}
                        placeholder="Search documentation or ask a question..."
                        value={query}
                        onChange={(e) => onQueryChange(e.target.value)}
                        onKeyDown={handleKeyDown}
                        onFocus={handleFocus}
                        onBlur={handleBlur}
                        fullWidth
                        isClearable
                    />
                    <RecentSearchesDropdown
                        searches={recentSearches}
                        onSelect={handleSelectRecent}
                        onClear={onClearRecent}
                        visible={showRecent}
                    />
                </EuiFlexItem>
                <EuiFlexItem grow={false}>
                    <EuiButton
                        fill
                        iconType="search"
                        onClick={() => onSearch(query)}
                    >
                        <span
                            css={css`
                                display: none;
                                @media (min-width: 600px) {
                                    display: inline;
                                }
                            `}
                        >
                            Search
                        </span>
                    </EuiButton>
                </EuiFlexItem>
            </EuiFlexGroup>
        </header>
    )
}
