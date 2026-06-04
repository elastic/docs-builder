import '../../eui-icons-cache'
import { ElasticAiAssistantButton } from './ElasticAiAssistantButton'
import { AskAiFlyoutBodyContent, useAskAiFlyout } from './useAskAiFlyout'
import { EuiFlyout, EuiPortal, EuiThemeProvider } from '@elastic/eui'
import { css } from '@emotion/react'

const headerButtonWrapperCss = css`
    display: flex;
    align-items: center;
    margin-left: 8px;
`

export const AskAiHeaderButton = () => {
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

    return (
        <>
            <span css={headerButtonWrapperCss}>
                <ElasticAiAssistantButton
                    aria-label="Ask AI"
                    onClick={isModalOpen ? closeModal : openModal}
                    fill={true}
                    size="s"
                >
                    Ask AI
                </ElasticAiAssistantButton>
            </span>

            {isModalOpen && (
                <EuiPortal>
                    <EuiThemeProvider colorMode="light">
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
                    </EuiThemeProvider>
                </EuiPortal>
            )}
        </>
    )
}
