import { config } from '../../config'
import {
    useAskAiModalActions,
    useAskAiModalIsOpen,
    useFlyoutWidth,
} from './askAi.modal.store'
import { EuiFlyoutBody, EuiLoadingSpinner } from '@elastic/eui'
import { css } from '@emotion/react'
import { useQuery } from '@tanstack/react-query'
import { useEffect, Suspense, lazy } from 'react'

const LazyAskAiModal = lazy(() =>
    import('./AskAiModal').then((module) => ({
        default: module.AskAiModal,
    }))
)

const loadingCss = css`
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 2rem;
`

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

export const AskAiFlyoutBodyContent = () => (
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
)

export const useAskAiFlyout = () => {
    const isModalOpen = useAskAiModalIsOpen()
    const { openModal, closeModal, setFlyoutWidth } = useAskAiModalActions()
    const flyoutWidth = useFlyoutWidth()

    const { data: isApiAvailable } = useQuery({
        queryKey: ['api-health'],
        queryFn: async () => {
            const response = await fetch(`${config.apiBasePath}/v1/`, {
                method: 'POST',
            })
            return response.ok
        },
        staleTime: 60 * 60 * 1000, // 60 minutes
        retry: false,
        enabled: config.buildType !== 'codex',
    })

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

    useEffect(() => {
        const handleOpenEvent = () => openModal()
        document.addEventListener('ask-ai:open', handleOpenEvent)
        return () =>
            document.removeEventListener('ask-ai:open', handleOpenEvent)
    }, [openModal])

    return {
        isApiAvailable:
            config.buildType === 'codex' ? true : (isApiAvailable ?? false),
        isModalOpen,
        openModal,
        closeModal,
        setFlyoutWidth,
        flyoutWidth,
    }
}
