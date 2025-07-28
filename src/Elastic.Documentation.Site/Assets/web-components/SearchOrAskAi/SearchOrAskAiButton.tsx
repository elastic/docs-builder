import '../../eui-icons-cache'
import { SearchOrAskAiModal } from './SearchOrAskAiModal'
import { useModalActions, useModalIsOpen } from './modal.store'
import { useSearchActions, useSearchTerm } from './search.store'
import {
    EuiButton,
    EuiPortal,
    EuiOverlayMask,
    EuiFocusTrap,
    EuiPanel,
    EuiTextTruncate,
    EuiText,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useEffect } from 'react'

export const SearchOrAskAiButton = () => {
    const searchTerm = useSearchTerm()
    const { clearSearchTerm } = useSearchActions()
    const isModalOpen = useModalIsOpen()
    const { openModal, closeModal, toggleModal } = useModalActions()

    const positionCss = css`
        position: absolute;
        left: 50%;
        transform: translateX(-50%);
        top: 48px;
        width: 90ch;
        max-width: 100%;
    `

    useEffect(() => {
        const handleKeydown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                clearSearchTerm()
                closeModal()
            }
            if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
                event.preventDefault()
                toggleModal()
            }
        }
        window.addEventListener('keydown', handleKeydown)
        return () => {
            window.removeEventListener('keydown', handleKeydown)
        }
    }, [])

    return (
        <>
            <EuiButton
                size="s"
                color="text"
                onClick={openModal}
                iconType="search"
            >
                <EuiText
                    color="subdued"
                    size="xs"
                    style={{ width: 200 }}
                    textAlign="left"
                >
                    <span>
                        {searchTerm ? (
                            <EuiTextTruncate
                                text={searchTerm}
                                truncation="end"
                            />
                        ) : (
                            'Search or ask AI'
                        )}
                    </span>
                </EuiText>
                <EuiText color="subdued" size="xs">
                    <kbd className="font-body bg-grey-20 border-none!">⌘K</kbd>
                </EuiText>
            </EuiButton>
            {isModalOpen && (
                <EuiPortal>
                    <EuiOverlayMask>
                        <EuiFocusTrap onClickOutside={closeModal}>
                            <EuiPanel role="dialog" css={positionCss}>
                                <SearchOrAskAiModal />
                            </EuiPanel>
                        </EuiFocusTrap>
                    </EuiOverlayMask>
                </EuiPortal>
            )}
        </>
    )
}
