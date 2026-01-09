import '../../eui-icons-cache'
import { NavigationSearch } from './NavigationSearch'
import { useModalActions, useModalIsOpen } from './modal.store'
import {
    EuiPortal,
    EuiOverlayMask,
    EuiFocusTrap,
    EuiPanel,
    EuiLoadingSpinner,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import { useQuery } from '@tanstack/react-query'
import { useEffect, Suspense, lazy } from 'react'

// Lazy load the modal component
const SearchOrAskAiModal = lazy(() =>
    import('./SearchOrAskAiModal').then((module) => ({
        default: module.SearchOrAskAiModal,
    }))
)

export const SearchOrAskAiButton = () => {
    const { euiTheme } = useEuiTheme()
    const isModalOpen = useModalIsOpen()
    const { openModal, closeModal } = useModalActions()

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
        width: 640px;
        max-width: 100%;
        border-radius: ${euiTheme.size.s};
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
                closeModal()
            }

            // Cmd+; to open Ask AI modal
            if (
                (event.metaKey || event.ctrlKey) &&
                event.code === 'Semicolon'
            ) {
                event.preventDefault()
                openModal()
            }
        }
        window.addEventListener('keydown', handleKeydown)
        return () => {
            window.removeEventListener('keydown', handleKeydown)
        }
    }, [openModal, closeModal])

    useEffect(() => {
        if (!isModalOpen) return

        const html = document.documentElement
        const body = document.body

        const originalHtmlOverflow = html.style.overflow
        const originalBodyOverflow = body.style.overflow
        const originalHtmlPaddingRight = html.style.paddingRight

        const scrollBarWidth =
            window.innerWidth - document.documentElement.clientWidth

        html.style.overflow = 'hidden'
        body.style.overflow = 'hidden'

        if (scrollBarWidth > 0) {
            html.style.paddingRight = `${scrollBarWidth}px`
        }

        return () => {
            html.style.overflow = originalHtmlOverflow
            body.style.overflow = originalBodyOverflow
            html.style.paddingRight = originalHtmlPaddingRight
        }
    }, [isModalOpen])

    if (!isApiAvailable) {
        return null
    }

    return (
        <div>
            <NavigationSearch />

            {isModalOpen && (
                <EuiPortal>
                    <EuiOverlayMask>
                        <EuiFocusTrap onClickOutside={closeModal}>
                            <EuiPanel
                                role="dialog"
                                css={positionCss}
                                paddingSize="none"
                            >
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
        </div>
    )
}
