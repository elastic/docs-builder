import { useEuiTheme } from '@elastic/eui'
import { css, SerializedStyles } from '@emotion/react'

interface LegalDisclaimerProps {
    css?: SerializedStyles
}

export const LegalDisclaimer = ({
    css: customCss,
}: LegalDisclaimerProps = {}) => {
    const { euiTheme } = useEuiTheme()
    return (
        <div
            css={[
                css`
                    text-align: center;
                    color: ${euiTheme.colors.textDisabled};
                    font-size: 10px;
                    a {
                        text-decoration: underline;
                    }
                `,
                customCss,
            ]}
        >
            This chatbot uses AI and can make mistakes, so review the
            information for accuracy. Your queries and interactions may be
            stored to help improve and train the model. Do not enter personal,
            confidential, or proprietary information. Any personal data
            collected will be handled according to our{' '}
            <a
                target="_blank"
                href="https://www.elastic.co/legal/privacy-statement"
            >
                General Privacy Statement
            </a>
            .
        </div>
    )
}
