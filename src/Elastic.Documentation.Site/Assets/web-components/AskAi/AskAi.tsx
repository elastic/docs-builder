import '../../eui-icons-cache'
import { sharedQueryClient } from '../shared/queryClient'
import { ElasticAiAssistantButton } from './ElasticAiAssistantButton'
import { AskAiFlyoutBodyContent, useAskAiFlyout } from './useAskAiFlyout'
import {
    EuiFlyout,
    EuiProvider,
    euiShadow,
    euiShadowHover,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import r2wc from '@r2wc/react-to-web-component'
import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'

const fabContainerCss = (euiThemeContext: ReturnType<typeof useEuiTheme>) => {
    const { euiTheme } = euiThemeContext
    return css`
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
}

const AskAiButton = () => {
    const euiThemeContext = useEuiTheme()
    const {
        isApiAvailable,
        isModalOpen,
        openModal,
        closeModal,
        setFlyoutWidth,
        flyoutWidth,
    } = useAskAiFlyout()

    if (!isApiAvailable) {
        return null
    }

    const flyout = isModalOpen ? (
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
            <AskAiFlyoutBodyContent />
        </EuiFlyout>
    ) : null

    return (
        <>
            {!isModalOpen && (
                <div css={fabContainerCss(euiThemeContext)}>
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
