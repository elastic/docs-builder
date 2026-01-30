import '../../eui-icons-cache'
import { sharedQueryClient } from '../shared/queryClient'
import { ElasticAiAssistantButton } from './ElasticAiAssistantButton'
import {
    useAskAiModalActions,
    useAskAiModalIsOpen,
    useFlyoutWidth,
} from './askAi.modal.store'
import {
    EuiFlyout,
    EuiFlyoutBody,
    EuiLoadingSpinner,
    EuiProvider,
    euiShadow,
    euiShadowHover,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider, useQuery } from '@tanstack/react-query'
import { useEffect, Suspense, lazy, StrictMode } from 'react'

// Lazy load the modal component
const LazyAskAiModal = lazy(() =>
    import('./AskAiModal').then((module) => ({
        default: module.AskAiModal,
    }))
)

const AskAiButton = () => {
    const isModalOpen = useAskAiModalIsOpen()
    const { openModal, closeModal, setFlyoutWidth } = useAskAiModalActions()
    const flyoutWidth = useFlyoutWidth()
    const euiThemeContext = useEuiTheme()
    const { euiTheme } = euiThemeContext

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

    const fabContainerCss = css`
        position: fixed;
        bottom: ${euiTheme.size.xxxxl};
        right: ${euiTheme.size.xxxxl};
        z-index: ${euiTheme.levels.mask};
        border-radius: 9999px;
        ${euiShadow(euiThemeContext, 'm')};
        transition: transform 0.15s ease;

        &:hover {
            transform: translateY(-2px);
            ${euiShadowHover(euiThemeContext, 'm')};
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
        return () => window.removeEventListener('keydown', handleKeydown)
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
                minWidth={376}
                maxWidth={700}
                paddingSize="none"
                hideCloseButton={true}
                size={flyoutWidth}
                onResize={setFlyoutWidth}
                outsideClickCloses={false}
            >
                <EuiFlyoutBody>
                    <div css={backgroundWrapperCss}>
                        <Suspense
                            fallback={
                                <div css={loadingCss}>
                                    <EuiLoadingSpinner size="xl" />
                                </div>
                            }
                        >
                            <LazyAskAiModal />
                        </Suspense>
                    </div>
                </EuiFlyoutBody>
            </EuiFlyout>
        )
    }

    return (
        <>
            {!isModalOpen && (
                <div css={fabContainerCss}>
                    <ElasticAiAssistantButton
                        onClick={openModal}
                        aria-label="Open Ask AI"
                        fill={true}
                    >
                        Ask AI
                    </ElasticAiAssistantButton>
                </div>
            )}
            {flyout}
        </>
    )
}

const backgroundWrapperCss = css`
    position: relative;
    min-height: 100%;

    &::before {
        content: '';
        position: absolute;
        top: 38px;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: #e5e5f7;
        pointer-events: none;
        z-index: 0;

        opacity: 0.4;
        background-image: radial-gradient(#444cf7 0.5px, #ffffff 0.5px);
        background-size: 10px 10px;

        mask-image: linear-gradient(
            to bottom,
            rgba(0, 0, 0, 1) 0%,
            rgba(0, 0, 0, 1) 10%,
            rgba(0, 0, 0, 0.5) 20%,
            rgba(0, 0, 0, 0) 30%
        );
        -webkit-mask-image: linear-gradient(
            to bottom,
            rgba(0, 0, 0, 1) 0%,
            rgba(0, 0, 0, 1) 10%,
            rgba(0, 0, 0, 0.5) 20%,
            rgba(0, 0, 0, 0) 30%
        );
    }

    & > * {
        position: relative;
        z-index: 1;
    }
`

const AskAi = () => {
    return (
        <StrictMode>
            <EuiProvider
                colorMode="light"
                globalStyles={false}
                utilityClasses={false}
            >
                <QueryClientProvider client={sharedQueryClient}>
                    <AskAiButton />
                </QueryClientProvider>
            </EuiProvider>
        </StrictMode>
    )
}

customElements.define('ask-ai', r2wc(AskAi))
