import {
    EuiBetaBadge,
    EuiLink,
    EuiSpacer,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'

export const InfoBanner = () => {
    const { euiTheme } = useEuiTheme()
    return (
        <div>
            <EuiSpacer size="m" />
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: ${euiTheme.size.s};
                    padding: 0 ${euiTheme.size.base} ${euiTheme.size.base};
                `}
            >
                <EuiBetaBadge
                    tabIndex={-1}
                    css={css`
                        display: inherit;
                    `}
                    label="Alpha"
                    size="s"
                    color="accent"
                    tooltipContent="This feature is in private preview and is only enabled if you are in Elastic's Global VPN."
                />

                <EuiText color="subdued" size="s">
                    This feature is in private preview.{' '}
                    <EuiLink
                        target="_blank"
                        rel="noopener noreferrer"
                        href="https://github.com/elastic/docs-eng-team/issues/new?template=search-or-ask-ai-feedback.yml"
                    >
                        Got feedback? We'd love to hear it!
                    </EuiLink>
                </EuiText>
            </div>
        </div>
    )
}
