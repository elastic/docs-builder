import { EuiBadge, EuiIcon, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'

export const JumpToIndicator = ({ label }: { label: string }) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            className="jump-to-indicator"
            css={css`
                flex-shrink: 0;
                color: ${euiTheme.colors.textSubdued};
                margin-left: ${euiTheme.size.xxxxl};
                visibility: hidden;
                a[data-selected='true'] &,
                a:hover:not([data-keyboard-navigating='true']) & {
                    visibility: visible;
                }
            `}
        >
            <EuiBadge color="hollow">
                {label} <EuiIcon type="returnKey" size="s" />
            </EuiBadge>
        </div>
    )
}
