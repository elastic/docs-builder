import '../../eui-icons-cache'
import { ElasticAiAssistantButton } from './ElasticAiAssitant'
import { useSearchTerm } from './Search/search.store'
import AiIcon from './ai-icon.svg'
import {
    ModalMode,
    useModalActions,
    useModalIsOpen,
    useModalMode,
} from './modal.store'
import {
    EuiButton,
    EuiPortal,
    EuiOverlayMask,
    EuiFocusTrap,
    EuiPanel,
    EuiTextTruncate,
    EuiText,
    EuiLoadingSpinner,
    useEuiTheme,
    EuiToolTip,
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
    const searchTerm = useSearchTerm()
    const isModalOpen = useModalIsOpen()
    const modalMode = useModalMode()
    const { openModal, closeModal, setModalMode } = useModalActions()

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

    const openAndSetModalMode = (mode: ModalMode) => {
        setModalMode(mode)
        if (!isModalOpen) {
            openModal()
        }
    }

    const openAskAiModal = () => openAndSetModalMode('askAi')
    const openSearchModal = () => openAndSetModalMode('search')

    // Prevent layout jump when hiding the scrollbar by compensating its width

    useEffect(() => {
        const handleKeydown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                closeModal()
            }
            if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
                event.preventDefault()
                openSearchModal()
                // Input focuses itself via its own Cmd+K listener
            }

            if (
                (event.metaKey || event.ctrlKey) &&
                event.code === 'Semicolon'
            ) {
                event.preventDefault()
                openAskAiModal()
                // Input focuses itself via its own Cmd+; listener
            }
        }
        window.addEventListener('keydown', handleKeydown)
        return () => {
            window.removeEventListener('keydown', handleKeydown)
        }
    }, [isModalOpen, modalMode])

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
        <div
            css={css`
                display: flex;
                gap: ${euiTheme.size.base};
            `}
        >
            <EuiToolTip content="Keyboard shortcut: ⌘;">
                <ElasticAiAssistantButton
                    size="s"
                    iconType={AiIcon}
                    onClick={openAskAiModal}
                >
                    Ask AI Assistant
                </ElasticAiAssistantButton>
            </EuiToolTip>

            <EuiButton
                size="s"
                color="text"
                onClick={openSearchModal}
                iconType="search"
            >
                <EuiText
                    color="subdued"
                    size="s"
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
                            'Search in Docs'
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
