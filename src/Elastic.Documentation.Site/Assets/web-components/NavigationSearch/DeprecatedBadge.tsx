import { useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'

/** Struck through and dimmed, the same treatment as a deprecated sidebar row; applied to the title's text span. */
export const deprecatedTitleText =
    '& > span:first-child { text-decoration: line-through; opacity: 0.7; }'

/** The operation page's Deprecated badge, sized to sit on a result title line. */
export const DeprecatedBadge = () => {
    const { euiTheme } = useEuiTheme()

    return (
        <span
            className="api-title-badges"
            css={css`
                /* An atomic inline, so the row's hover underline stops at the title text. Middle alignment lands 1px low beside the bold title. */
                display: inline-flex;
                align-items: center;
                position: relative;
                top: -1px;
                margin-left: ${euiTheme.size.s};
                vertical-align: middle;
                line-height: 1;
                font-weight: ${euiTheme.font.weight.regular};
                .applies {
                    display: inline-flex;
                    margin-right: 0;
                }
                .applicable-info {
                    display: inline-flex;
                    align-items: center;
                    height: calc(${euiTheme.size.base} + ${euiTheme.size.xxs});
                    padding: 0 ${euiTheme.size.s};
                    line-height: 1;
                }
                .applicable-name {
                    font-size: 10px;
                    line-height: 1;
                }
            `}
        >
            <span className="applies applies-inline">
                <span className="applicable-info api-badge--danger">
                    <span className="applicable-name">Deprecated</span>
                </span>
            </span>
        </span>
    )
}
