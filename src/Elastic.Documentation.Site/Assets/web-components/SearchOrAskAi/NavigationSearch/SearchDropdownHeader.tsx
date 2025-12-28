import { EuiBetaBadge, EuiLink, useEuiTheme, useEuiFontSize } from '@elastic/eui'
import { css } from '@emotion/react'

const FEEDBACK_URL =
    'https://github.com/elastic/docs-eng-team/issues/new?template=search-or-ask-ai-feedback.yml'

export const SearchDropdownHeader = () => {
    const { euiTheme } = useEuiTheme()
    const { fontSize: sFontsize, lineHeight: sLineHeight } = useEuiFontSize('s')

    return (
        <div
            css={css`
                display: flex;
                align-items: center;
                justify-content: space-between;
                padding-inline: ${euiTheme.size.base};
                padding-block: ${euiTheme.size.m};
            `}
        >
            <div
                css={css`
                    display: flex;
                    align-items: center;
                    gap: ${euiTheme.size.xs};
                `}
            >
                <span
                    css={css`
                        color: ${euiTheme.colors.textParagraph};
                        font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                        line-height: ${euiTheme.base * 1.25}px;
                    `}
                >
                    Pages
                </span>
                <span
                    css={css`
                        font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                        line-height: ${euiTheme.base * 1.25}px;
                    `}
                >
                    ·
                </span>
                <EuiBetaBadge
                    color="accent"
                    label="ALPHA"
                    size="s"
                    anchorProps={{
                        css: css`
                            display: inline-flex;
                            align-items: center;
                        `,
                    }}
                />
            </div>
            <EuiLink
                href={FEEDBACK_URL}
                target="_blank"
                external
                css={css`
                    font-size: ${sFontsize};
                    line-height: ${sLineHeight};
                `}
            >
                Give feedback
            </EuiLink>
        </div>
    )
}
