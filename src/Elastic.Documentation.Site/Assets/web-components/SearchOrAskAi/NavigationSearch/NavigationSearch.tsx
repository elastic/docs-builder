import { KeyboardShortcutsFooter } from '../KeyboardShortcutsFooter'
import { useSearchTerm, useSearchActions } from '../Search/search.store'
import { useIsSearchCooldownActive } from '../Search/useSearchCooldown'
import { useSearchQuery } from '../Search/useSearchQuery'
import { SearchDropdownHeader } from './SearchDropdownHeader'
import { SearchInput } from './SearchInput'
import { SearchResultsList } from './SearchResultsList'
import { useGlobalKeyboardShortcut } from './useGlobalKeyboardShortcut'
import { useNavigationSearchKeyboardNavigation } from './useNavigationSearchKeyboardNavigation'
import { EuiInputPopover, EuiHorizontalRule } from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, useRef } from 'react'

export const NavigationSearch = () => {
    const [isPopoverOpen, setIsPopoverOpen] = useState(false)
    const popoverContentRef = useRef<HTMLDivElement>(null)
    const searchTerm = useSearchTerm()
    const { setSearchTerm } = useSearchActions()
    const isSearchCooldownActive = useIsSearchCooldownActive()
    const { isLoading, isFetching, data } = useSearchQuery()

    const results = data?.results ?? []
    const hasContent = !!searchTerm.trim()
    const isSearching = isLoading || isFetching

    const {
        inputRef,
        itemRefs,
        isKeyboardNavigating,
        handleInputKeyDown,
        handleMouseMove,
    } = useNavigationSearchKeyboardNavigation({
        resultsCount: results.length,
        isLoading: isSearching,
        onClose: () => setIsPopoverOpen(false),
    })

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const value = e.target.value
        setSearchTerm(value)
        setIsPopoverOpen(!!value.trim())
    }

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Escape') {
            e.preventDefault()
            setSearchTerm('')
            setIsPopoverOpen(false)
            return
        }
        handleInputKeyDown(e)
    }

    const handleBlur = (e: React.FocusEvent) => {
        // Check if focus is moving to something inside the popover
        const relatedTarget = e.relatedTarget as Node | null
        if (
            relatedTarget &&
            popoverContentRef.current?.contains(relatedTarget)
        ) {
            // Focus is moving inside the popover, don't close
            return
        }
        setIsPopoverOpen(false)
    }

    useGlobalKeyboardShortcut('k', () => inputRef.current?.focus())

    return (
        <EuiInputPopover
            isOpen={isPopoverOpen && hasContent}
            closePopover={() => setIsPopoverOpen(false)}
            ownFocus={false}
            disableFocusTrap={true}
            panelMinWidth={640}
            panelPaddingSize="none"
            offset={12}
            panelProps={{
                css: css`
                    max-width: 640px;
                `,
                onMouseDown: (e: React.MouseEvent) => {
                    // Prevent input blur when clicking anywhere inside the popover panel
                    e.preventDefault()
                },
            }}
            input={
                <SearchInput
                    inputRef={inputRef}
                    value={searchTerm}
                    onChange={handleChange}
                    onFocus={() => hasContent && setIsPopoverOpen(true)}
                    onBlur={handleBlur}
                    onKeyDown={handleKeyDown}
                    disabled={isSearchCooldownActive}
                    isLoading={isSearching}
                />
            }
        >
            {hasContent && (
                <div ref={popoverContentRef}>
                    <SearchDropdownContent
                        itemRefs={itemRefs}
                        isKeyboardNavigating={isKeyboardNavigating}
                        onMouseMove={handleMouseMove}
                    />
                </div>
            )}
        </EuiInputPopover>
    )
}

const KEYBOARD_SHORTCUTS = [
    { keys: ['returnKey'], label: 'Jump to' },
    { keys: ['sortUp', 'sortDown'], label: 'Navigate' },
    { keys: ['Esc'], label: 'Close' },
]

interface SearchDropdownContentProps {
    itemRefs: React.MutableRefObject<(HTMLAnchorElement | null)[]>
    isKeyboardNavigating: React.MutableRefObject<boolean>
    onMouseMove: () => void
}

const SearchDropdownContent = ({
    itemRefs,
    isKeyboardNavigating,
    onMouseMove,
}: SearchDropdownContentProps) => (
    <>
        <SearchDropdownHeader />
        <EuiHorizontalRule margin="none" />
        <SearchResultsList
            itemRefs={itemRefs}
            isKeyboardNavigating={isKeyboardNavigating}
            onMouseMove={onMouseMove}
        />
        <KeyboardShortcutsFooter shortcuts={KEYBOARD_SHORTCUTS} />
    </>
)
