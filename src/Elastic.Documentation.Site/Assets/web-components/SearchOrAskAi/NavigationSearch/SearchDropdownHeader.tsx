import {
    EuiBetaBadge,
    EuiLink,
    useEuiTheme,
    useEuiFontSize,
} from '@elastic/eui'
import { css } from '@emotion/react'

const FEEDBACK_URL =
    'https://github.com/elastic/docs-eng-team/issues/new?template=search-or-ask-ai-feedback.yml'

export const SearchDropdownHeader = () => {
    const { euiTheme } = useEuiTheme()
    const { fontSize: xsFontsize } = useEuiFontSize('xs')

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
                    gap: ${euiTheme.size.s};
                `}
            >
                <span
                    css={css`
                        color: ${euiTheme.colors.textParagraph};
                    `}
                >
                    Pages
                </span>
                <span>·</span>
                <EuiBetaBadge
                    color="accent"
                    label="ALPHA"
                    css={css`
                        display: inline-flex;
                        align-items: center;
                    `}
                />
            </div>
            <EuiLink
                href={FEEDBACK_URL}
                target="_blank"
                external
                css={css`
                    font-size: ${xsFontsize};
                `}
            >
                Give feedback
            </EuiLink>
        </div>
    )
}
