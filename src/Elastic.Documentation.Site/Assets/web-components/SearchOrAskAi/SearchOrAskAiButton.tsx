/** @jsxImportSource @emotion/react */
import '../../eui-icons-cache'
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
    EuiLoadingSpinner,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useQuery } from '@tanstack/react-query'
import * as React from 'react'
import { useEffect, Suspense, lazy } from 'react'

// Lazy load the modal component
const SearchOrAskAiModal = lazy(() =>
    import('./SearchOrAskAiModal').then((module) => ({
        default: module.SearchOrAskAiModal,
    }))
)

export const SearchOrAskAiButton = () => {
    const searchTerm = useSearchTerm()
    const { clearSearchTerm } = useSearchActions()
    const isModalOpen = useModalIsOpen()
    const { openModal, closeModal, toggleModal } = useModalActions()

    const { data: isApiAvailable } = useQuery({
        queryKey: ['api-health'],
        queryFn: async () => {
            const response = await fetch('/docs/_api/v1/', { method: 'POST' })
            return response.ok
        },
        staleTime: 60 * 60 * 1000, // 60 minutes
        retry: false,
    })

    const positionCss = css`
        position: absolute;
        left: 50%;
        transform: translateX(-50%);
        top: 48px;
        width: 90ch;
        max-width: 100%;
    `

    const loadingCss = css`
        display: flex;
        justify-content: center;
        align-items: center;
        padding: 2rem;
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

    if (!isApiAvailable) {
        return null
    }

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
                                <Suspense
                                    fallback={
                                        <div css={loadingCss}>
                                            <EuiLoadingSpinner size="xl" />
                                        </div>
                                    }
                                >
                                    <SearchOrAskAiModal />
                                </Suspense>
                            </EuiPanel>
                        </EuiFocusTrap>
                    </EuiOverlayMask>
                </EuiPortal>
            )}
        </>
    )
}
