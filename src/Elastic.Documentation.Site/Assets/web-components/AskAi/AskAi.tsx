import '../../eui-icons-cache'
import AiIcon from './ai-icon.svg'
import { useAskAiModalActions, useAskAiModalIsOpen } from './askAi.modal.store'
import {
    EuiFlyout,
    EuiFlyoutBody,
    EuiIcon,
    EuiLoadingSpinner,
    EuiProvider,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import {
    QueryClient,
    QueryClientProvider,
    useQuery,
} from '@tanstack/react-query'
import { useEffect, Suspense, lazy, StrictMode } from 'react'

const queryClient = new QueryClient()

// Lazy load the modal component
const LazyAskAiModal = lazy(() =>
    import('./AskAiModal').then((module) => ({
        default: module.AskAiModal,
    }))
)

const AskAiButton = () => {
    const isModalOpen = useAskAiModalIsOpen()
    const { openModal, closeModal } = useAskAiModalActions()
    const { euiTheme } = useEuiTheme()

    const { data: isApiAvailable } = useQuery({
        queryKey: ['api-health'],
        queryFn: async () => {
            const response = await fetch('/docs/_api/v1/', { method: 'POST' })
            return response.ok
        },
        staleTime: 60 * 60 * 1000, // 60 minutes
        retry: false,
    })

    const loadingCss = css`
        display: flex;
        justify-content: center;
        align-items: center;
        padding: 2rem;
    `

    const fabCss = css`
        position: fixed;
        bottom: ${euiTheme.size.xl};
        right: ${euiTheme.size.xl};
        height: 44px;
        padding-inline: ${euiTheme.size.m};
        border-radius: ${euiTheme.border.radius.medium};
        background-color: ${euiTheme.colors.primary};
        color: ${euiTheme.colors.ghost};
        border: none;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: ${euiTheme.size.s};
        font-size: ${euiTheme.size.m};
        font-weight: ${euiTheme.font.weight.medium};
        box-shadow: 0px 4px 12px rgba(0, 0, 0, 0.15);
        transition:
            transform 0.15s ease,
            box-shadow 0.15s ease,
            background-color 0.15s ease;
        z-index: ${euiTheme.levels.mask};

        &:hover {
            transform: translateY(-2px);
            box-shadow: 0px 6px 16px rgba(0, 0, 0, 0.2);
            background-color: ${euiTheme.colors.primaryText};
        }

        &:active {
            transform: translateY(0);
        }
    `

    useEffect(() => {
        const handleKeydown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') {
                event.preventDefault()
                closeModal()
            }
            // Cmd+; to open Ask AI flyout
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

    if (!isApiAvailable) {
        return null
    }

    let flyout
    if (isModalOpen) {
        flyout = (
            <EuiFlyout
                ownFocus={false}
                onClose={closeModal}
                aria-label="Ask AI"
                resizable={true}
                minWidth={400}
                maxWidth={800}
                paddingSize="none"
                hideCloseButton={true}
                size={400}
                outsideClickCloses={false}
            >
                <EuiFlyoutBody>
                    <Suspense
                        fallback={
                            <div css={loadingCss}>
                                <EuiLoadingSpinner size="xl" />
                            </div>
                        }
                    >
                        <LazyAskAiModal />
                    </Suspense>
                </EuiFlyoutBody>
            </EuiFlyout>
        )
    }

    return (
        <>
            {!isModalOpen && (
                <button
                    css={fabCss}
                    onClick={openModal}
                    aria-label="Open Ask AI"
                >
                    <EuiIcon type={AiIcon} size="m" color="ghost" />
                    <span>Ask AI</span>
                </button>
            )}
            {flyout}
        </>
    )
}

const AskAi = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={queryClient}>
                    <AskAiButton />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define('ask-ai', r2wc(AskAi))
