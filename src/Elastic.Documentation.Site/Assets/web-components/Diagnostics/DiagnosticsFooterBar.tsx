import { DiagnosticsSummary } from './DiagnosticsSummary'
import { useDiagnosticsStore } from './diagnostics.store'
import { getSeverityColor } from './severityUtils'
import { EuiText, EuiIcon, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useCallback } from 'react'

export const DiagnosticsFooterBar: React.FC = () => {
    const { isHudOpen, toggleHud, errors, warnings, hints } =
        useDiagnosticsStore()
    const { euiTheme } = useEuiTheme()

    const severityColor = getSeverityColor(errors, warnings, hints)
    const borderColor = severityColor
        ? euiTheme.colors[severityColor]
        : euiTheme.border.color

    const onKeyDown = useCallback(
        (e: React.KeyboardEvent) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault()
                toggleHud()
            }
        },
        [toggleHud]
    )

    return (
        <div
            role="button"
            tabIndex={0}
            onClick={toggleHud}
            onKeyDown={onKeyDown}
            aria-expanded={isHudOpen}
            aria-label={
                isHudOpen
                    ? 'Hide diagnostics console'
                    : 'Show diagnostics console'
            }
            style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                width: '100%',
                minHeight: euiTheme.size.xl,
                padding: `${euiTheme.size.m} ${euiTheme.size.m}`,
                borderTop: `1px solid ${borderColor}`,
                backgroundColor: '#0B1628',
                flexShrink: 0,
                cursor: 'pointer',
            }}
        >
            <DiagnosticsSummary />
            <EuiText color="subdued" size="xs">
                <div
                    css={css`
                        display: flex;
                        align-items: center;
                        gap: ${euiTheme.size.xs};
                        padding-inline: ${euiTheme.size.xs};
                    `}
                >
                    <span>{isHudOpen ? 'Hide Console' : 'Show Console'}</span>
                    <EuiIcon
                        type={
                            isHudOpen ? 'chevronSingleDown' : 'chevronSingleUp'
                        }
                        size="s"
                    />
                </div>
            </EuiText>
        </div>
    )
}
